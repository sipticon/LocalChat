using System.Net;
using System.Net.Sockets;
using System.Text;

namespace LocalChat
{
    public class Client
    {
        private Thread receiveThread = null;
        private Thread sendThread = null;
        private Socket clientSocket = null;
        public Client(IPEndPoint serverIpEndPoint)
        {
            clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            clientSocket.Connect(serverIpEndPoint);
            Console.WriteLine("Connected to server.");
            
            try
            {
                receiveThread = new Thread(ReceiveMessageFromServer);
                receiveThread.Start(clientSocket);
                sendThread = new Thread(SendMessageToServer);
                sendThread.Start(clientSocket);
            }
            catch (Exception e)
            {
                Console.WriteLine("Disconnected from server!");
            }
        }

        private void SendMessageToServer(object socket)
        {
            while (clientSocket.Connected)
            {
                try
                {
                    Socket sendSocket = (Socket)socket;
                    string clientInput = Console.ReadLine();
                    sendSocket.Send(Encoding.UTF8.GetBytes(clientInput));
                }
                catch (Exception ex)
                {
                    Console.WriteLine("You can't send messages!");
                }
            }
        }

        private void ReceiveMessageFromServer(object socket)
        {
            while (clientSocket.Connected)
            {
                Socket receiveSocket = (Socket)socket;
                byte[] receivedData = new byte[1024];
                try
                {
                    int receivedDataSize = receiveSocket.Receive(receivedData);
                    string receivedString = Encoding.UTF8.GetString(receivedData, 0, receivedDataSize);
                    if (receivedString == "You successfully disconnected!")
                        CloseConnection();
                    else
                        Console.WriteLine("Server: " + receivedString);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Unable to connect with server!");
                    receiveThread.Interrupt();
                }
            }
        }

        private void CloseConnection()
        {
            Console.WriteLine("You disconnected from server!");
            clientSocket.Shutdown(SocketShutdown.Both);
            clientSocket.Close();
            receiveThread.Interrupt();
            sendThread.Interrupt();
        }
    }
}