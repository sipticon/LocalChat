using System.Net.Sockets;
using System.Net;
using System.Text;

namespace LocalChat
{
    public class Program
    {
        static int port = 8080;

        private static Socket udpSocket;
        public static void Main(String[] args)
        {
           SendUdp();
        }

        private static void SendUdp()
        {
            try
            {
                udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                IPEndPoint remotePoint = new IPEndPoint(IPAddress.Broadcast, port);
                udpSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);

                udpSocket.SendTimeout = 5000;

                byte[] data = Encoding.Unicode.GetBytes("Is anyone here?");
                udpSocket.SendTo(data, remotePoint);

                Client client = new Client(new IPEndPoint(IPAddress.Parse("127.0.0.1"), port));
            }
            catch (Exception ex)
            {
                Server server = new Server();
            }
            finally
            {
                udpSocket.Close();
            }
        }
    }
}