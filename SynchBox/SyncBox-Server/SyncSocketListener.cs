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
        Logging log;

        //private Socket handler;
        private Socket listener;

        public SyncSocketListener(){
            port = 1500;
        }

        public SyncSocketListener(int port,Logging log){
            this.port=port;
            this.log = log;
        }

        public void Start()
        {
            //NO MULTITHREAD FUNCTION

            log.WriteToLog("SynSockListener.Start() - started");

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
            log.WriteToLog("binding to local endpoint");
                listener.Bind(localEndPoint);
                listener.Listen(10); //max number of pending connections
                // Start listening for connections.
                log.WriteToLog("binding successfull");
          }

        public Socket AcceptConnection() {
            log.WriteToLog("tryiing to accept connextion");
            Socket handler = listener.Accept();
            log.WriteToLog("connection accepted!");
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