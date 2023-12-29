using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace LocalChat;

public class ClientInfo
{
    public string ClientName { get; set; } = "new client";
    public int CountOfSentMessages { get; set; }
    public Stopwatch ClientStopwatch { get; set; }
    public Socket ClientSocket { get; set; }
    public bool IsHaveToEcho { get; set; }
}