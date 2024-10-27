using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace FantasyVoxels.Networking
{
    public class Server
    {
        public static int MaxPlayers { get; private set; }
        public static int Port { get; private set; }

        public static Dictionary<int,Client> clients = new Dictionary<int,Client>();

        private static TcpListener tcpListner;

        public static void Start(int maxPlayers, int port)
        {
            MaxPlayers = maxPlayers;
            Port = port;

            Console.WriteLine($"Starting server on {port}");

            InitializeServerData();

            tcpListner = new TcpListener(IPAddress.Any,Port);
            tcpListner.Start();
            tcpListner.BeginAcceptTcpClient(new AsyncCallback(TCPConnectCallback),null);

            Console.WriteLine($"Server started on {port}");
        }

        private static void TCPConnectCallback(IAsyncResult ar)
        {
            TcpClient client = tcpListner.EndAcceptTcpClient(ar);
            tcpListner.BeginAcceptTcpClient(new AsyncCallback(TCPConnectCallback), null);

            Console.WriteLine($"{client.Client.RemoteEndPoint} is attempting connection...");

            for (int i = 1; i <= MaxPlayers; i++)
            {
                //Find an empty slot
                if (clients[i].tcp.socket == null)
                {
                    clients[i].tcp.Connect(client);
                    return;
                }
            }

            Console.WriteLine($"{client.Client.RemoteEndPoint} connection terminated. Server full!");
        }

        private static void InitializeServerData()
        {
            for (int i = 1; i <= MaxPlayers; i++)
            {
                clients.Add(i,new Client(i));
            }
        }
    }
}
