using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace FantasyVoxels.Networking
{
    public class Client
    {
        public static int dataBufferSize = 4096;
        public int id;
        public TCP tcp;

        public Client(int clientID)
        {
            this.id = clientID;
            tcp = new TCP(clientID);
        }

        public class TCP
        {
            public TcpClient socket;

            private readonly int id;
            private NetworkStream stream;
            private byte[] receiveBuffer;

            public TCP(int id)
            {
                this.id = id;
            }
            public void Connect(TcpClient socket)
            {
                this.socket = socket;
                socket.ReceiveBufferSize = dataBufferSize;
                socket.SendBufferSize = dataBufferSize;

                stream = socket.GetStream();

                receiveBuffer = new byte[dataBufferSize];

                stream.BeginRead(receiveBuffer,0,dataBufferSize,ReceiveCallback,null);
            }

            private void ReceiveCallback(IAsyncResult ar)
            {
                try
                {
                    int byteLength = stream.EndRead(ar);
                    if (byteLength <= 0)
                    {
                        //Disconnect

                        return;
                    }

                    byte[] data = new byte[byteLength];
                    Array.Copy(receiveBuffer,data,byteLength);

                    //TODO: handle

                    stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error receiving TCP data, client disconnect: {ex.Message}");
                    //Disconnect
                }
            }
        }
    }
}
