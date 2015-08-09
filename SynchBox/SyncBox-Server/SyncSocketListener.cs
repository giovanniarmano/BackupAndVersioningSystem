using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Windows;

namespace SyncBox_Server
{
    class SyncSocketListener
    {
        private int port = -1;

        //private Socket handler;
        private Socket listener;

        public SyncSocketListener(){
            port = 1500;
        }

        public SyncSocketListener(int port){
            this.port=port;
        }

        public void Start()
        {
            //NO MULTITHREAD FUNCTION

            MessageBox.Show("listener start!");

            // Establish the local endpoint for the socket.
            // Dns.GetHostName returns the name of the 
            // host running the application.
            IPHostEntry ipHostInfo = Dns.Resolve(Dns.GetHostName());
            IPAddress ipAddress = ipHostInfo.AddressList[0];
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, port);

            // Create a TCP/IP socket.
            listener = new Socket(AddressFamily.InterNetwork,
                SocketType.Stream, ProtocolType.Tcp );

            // Bind the socket to the local endpoint and 
            // listen for incoming connections.
                listener.Bind(localEndPoint);
                listener.Listen(10); //max number of pending connections
                // Start listening for connections.
          }

        public Socket AcceptConnection() {
            Socket handler = listener.Accept();
            return handler;
            
            //handler.Send(msg);
            //handler.Shutdown(SocketShutdown.Both);
            //handler.Close();
        }

        public NetworkStream getStream(Socket s) { 
            NetworkStream netStream = new NetworkStream(s, true);
            return netStream;
        }
}
}