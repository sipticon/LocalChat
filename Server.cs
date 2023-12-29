using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace LocalChat
{
    public class Server
    {
        private Socket serverSocket;
        private IPEndPoint serverIpEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8080);
        private List<ClientInfo> connectedClients = new List<ClientInfo>();
        private bool isServeRunnig = true;
        public Server()
        {
            byte[] data = new byte[1024];

            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            serverSocket.Bind(serverIpEndPoint);
            serverSocket.Listen(10);
            Console.WriteLine("Server started");
            Console.WriteLine("Waiting for a client...");
            try
            {
                while (isServeRunnig)
                {
                    Socket client = serverSocket.Accept();

                    IPEndPoint clientRemoteEndPoint = (IPEndPoint)client.RemoteEndPoint;

                    Console.WriteLine("Connected with {0} at port {1}", clientRemoteEndPoint.Address,
                        clientRemoteEndPoint.Port);

                    string welcome =
                        "Welcome to the server! \nCan I ask your name? P.S. You have to pass it by 'NAME' command, otherwise name will be set to default.";

                    data = Encoding.UTF8.GetBytes(welcome);

                    client.Send(data, data.Length, SocketFlags.None);

                    Thread newConnection = newConnection = new Thread(HandleClientResponse);
                    newConnection.IsBackground = true;
                    newConnection.Start(client);
                }

                Console.WriteLine("Server stopped!");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Server stopped!");
            }
        }

        private void HandleClientResponse(object socket)
        {
            ClientInfo currentClientInfo = new ClientInfo();
            currentClientInfo.IsHaveToEcho = true;
            currentClientInfo.CountOfSentMessages = 0;
            currentClientInfo.ClientSocket = (Socket)socket;
            currentClientInfo.ClientStopwatch = Stopwatch.StartNew();

            connectedClients.Add(currentClientInfo);
            bool isFirstConnectionWithClient = true;

            try
            {
                while (currentClientInfo.ClientSocket.Connected)
                {
                    string messageFromClient = ReceiveMessageFromClient(currentClientInfo);
                    string command = "";
                    string name = "";
                    string text = "";
                    ParseReceivedMessage(messageFromClient, ref command, ref name, ref text);
                    currentClientInfo.CountOfSentMessages++;
                    if (isFirstConnectionWithClient)
                    {
                        currentClientInfo.ClientName = (command != "" && command.Trim() == "NAME")
                            ? name.Trim()
                            : $"Client N{connectedClients.Count}";
                        isFirstConnectionWithClient = false;
                        SendMessageToClient(currentClientInfo.ClientSocket,
                            $"Your name successfully changed to {currentClientInfo.ClientName}");
                    }
                    else if (command != "")
                        CheckInputCommand(currentClientInfo, command.Trim(), name.Trim(), text.TrimStart(),
                            messageFromClient);
                    else
                        SendMessageToClient(currentClientInfo.ClientSocket,
                            PauseResumeEchoToClient(currentClientInfo.IsHaveToEcho, messageFromClient));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Client disconnected!");
            }
        }

        private void CheckInputCommand(ClientInfo clientInfo, string command, string name, string text, string messageFromClient)
        {
            Socket socket = clientInfo.ClientSocket;
            switch (command)
            {
                //NAME name - set name to client/ if not sent name = Client Nn - ok
                //STAT - returns clients list - ok
                //MESG name (text) - send (text) to the name - ok
                //BCST (text) - send (text) to all clients - ok
                //QUIT - disconnect client - ok
                //KILL name - disconnect name - ok
                //PARE name - PAuse/REsume sending back message to name - ok
                //CLOS - server close - ok
                //TIME name - return connection time with name - ok
                //KILK name - returns numbers of messages sent from name - ok
                //IPAD name - return IP of name - ok
                case "STAT":
                    SendMessageToClient(socket, ReturnClientListAsString());
                    break;
                case "MESG":
                    if (text != "" && IsCorrectEnterOfCommand(socket, name))
                    {
                        string resultOfSent = SendMessageToClientByName(name, $"Message from {clientInfo.ClientName}: {text}.");
                        SendMessageToClient(socket, resultOfSent);
                    }
                    break;
                case "BCST":
                    if (IsCorrectEnterOfCommand(socket, name))
                        SendMessageToAllClients($"Message from {clientInfo.ClientName}: {name} {text}.");
                    break;
                case "QUIT":
                    SendMessageToClient(socket, $"You successfully disconnected!");
                    socket.Shutdown(SocketShutdown.Both);
                    socket.Close();
                    connectedClients.Remove(clientInfo);
                    Console.WriteLine($"Client {clientInfo.ClientName} disconnected");
                    break;
                case "KILL":
                    if (IsCorrectEnterOfCommand(socket, name))
                    {
                        string resultOfKill = DisconnectSocket(GetClientByName(name));
                        SendMessageToClient(socket, resultOfKill);
                    }
                    break;
                case "PARE":
                    if (IsCorrectEnterOfCommand(socket, name))
                    {
                        if (GetClientByName(name) != null)
                        {
                            GetClientByName(name).IsHaveToEcho = !GetClientByName(name).IsHaveToEcho;
                            SendMessageToClient(socket, $"Is server have to eco to client {name} set to {GetClientByName(name).IsHaveToEcho}");
                        }
                        else
                            SendMessageToClient(socket, $"Client with name {name} does not connected!");
                    }
                    break;
                case "CLOS":
                    SendMessageToClient(socket, "Server closed!");
                    CloseServer();
                    break;
                case "TIME":
                    if (IsCorrectEnterOfCommand(socket, name))
                    {
                        if (GetClientByName(name) != null)
                        {
                            string messageAboutTime =
                                $"Client {name} connecting to server during {GetClientByName(name).ClientStopwatch.ElapsedMilliseconds / 1000} sec.";
                            SendMessageToClient(socket, messageAboutTime);
                        }
                        else
                            SendMessageToClient(socket, $"Client with name {name} does not connected!");
                    }
                    break;
                case "KILK":
                    if (IsCorrectEnterOfCommand(socket, name))
                    {
                        if (GetClientByName(name) != null)
                        {
                            string messageAboutCountOfSentMessage =
                                $"Client {name} sent to server {GetClientByName(name).CountOfSentMessages} messages.";
                            SendMessageToClient(socket, messageAboutCountOfSentMessage);
                        }
                        else
                            SendMessageToClient(socket, $"Client with name {name} does not connected!");
                    }
                    break;
                case "IPAD":
                    if (IsCorrectEnterOfCommand(socket, name))
                    {
                        if (GetClientByName(name) != null)
                        {
                            string messageAboutIP =
                                $"Client {name} has next IP {GetClientByName(name).ClientSocket.RemoteEndPoint}.";
                            SendMessageToClient(socket, messageAboutIP);
                        }
                        else
                            SendMessageToClient(socket, $"Client with name {name} does not connected!");
                    }
                    break;
                default:
                    SendMessageToClient(socket, PauseResumeEchoToClient(GetClientByName(name).IsHaveToEcho, messageFromClient));
                    break;
            }
        }

        private bool IsCorrectEnterOfCommand(Socket socket, string textToCheck)
        {
            if(textToCheck == "")
            {
                SendMessageToClient(socket, "Incorrect enter of command!");
                return false;
            }

            return true;
        }

        private void ParseReceivedMessage(string receive, ref string command, ref string name, ref string text)
        {
            Regex regex = new Regex("(?'command'\\p{Lu}{4})(?'name'\\s\\w+)?(?'text'\\s.+)?");
            if (regex.IsMatch(receive))
            {
                command = regex.Match(receive).Groups["command"].Value;
                name = regex.Match(receive).Groups["name"].Value;
                text = regex.Match(receive).Groups["text"].Value;
            }
        }

        private ClientInfo GetClientByName(string name)
        {
            ClientInfo clientToReturn = null;
            foreach (var client in connectedClients)
            {
                if (client.ClientName == name)
                    clientToReturn = client;
            }
            return clientToReturn;
        }

        private void SendMessageToClient(Socket socket, string messageToSend)
        {
            socket.Send(Encoding.UTF8.GetBytes(messageToSend));
        }

        private string SendMessageToClientByName(string name, string message)
        {
            string returnText = $"Client with name {name} does not connected!";
            foreach (var client in connectedClients)
            {
                if (client.ClientName == name)
                {
                    client.ClientSocket.Send(Encoding.UTF8.GetBytes(message));
                    returnText = $"Message successfully sent to {name}.";
                }
            }

            return returnText;
        }

        private void SendMessageToAllClients(string message)
        {
            foreach (var client in connectedClients)
            {
                client.ClientSocket.Send(Encoding.UTF8.GetBytes(message));
            }
        }

        private string ReceiveMessageFromClient(ClientInfo client)
        {
            byte[] data = new byte[1024];

            int recv = client.ClientSocket.Receive(data);
            string stringData = Encoding.UTF8.GetString(data, 0, recv);
            Console.WriteLine($"Received from {client.ClientName}: " + stringData);
            return stringData;
        }

        private string PauseResumeEchoToClient(bool isHaveToResponseToThisClient, string receive)
        {
            string stringToReturn = "(/ > <)/";
            if (isHaveToResponseToThisClient)
                stringToReturn = receive;
            return stringToReturn;
        }

        private string ReturnClientListAsString()
        {
            string connectedClientsList = "Connected clients: ";
            foreach (var client in connectedClients)
            {
                connectedClientsList += client.ClientName + " ";
            }
            return connectedClientsList;
        }

        private string DisconnectSocket(ClientInfo client)
        {
            if (client != null)
            {
                SendMessageToClient(client.ClientSocket, $"You successfully disconnected!");
                client.ClientSocket.Shutdown(SocketShutdown.Both);
                client.ClientSocket.Close();
                connectedClients.Remove(client);
                Console.WriteLine($"Client {client.ClientName} disconnected from server.");
                return $"Client {client.ClientName} successfully disconnected!";
            }
            else
            {
                return "Client with this name does not connected!";
            }
            
        }

        private void CloseServer()
        {
            try
            {
                foreach (var client in connectedClients)
                {
                    DisconnectSocket(client);
                }

                connectedClients = new List<ClientInfo>();
            }
            catch {}
            isServeRunnig = false;
            Console.WriteLine("Server closing...");
            serverSocket.Close();
        }
    }
}