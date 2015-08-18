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
    public class SyncSocketListener
    {
        private int port = -1;
        Logging log;

        //private Socket handler;
        private Socket listener;

        public SyncSocketListener(int port,Logging log){
            this.port=port;
            this.log = log;
        }

        public void Start()
        {
            //NO MULTITHREAD FUNCTION
            log.WriteToLog("Binding ...");
            // Establish the local endpoint for the socket.
            // Dns.GetHostName returns the name of the 
            // host running the application.
            
            //OLD DEPRECATED
            /*
            IPHostEntry ipHostInfo = Dns.Resolve(Dns.GetHostName());
            //IPHostEntry ipHostInfo = Dns.Resolve(Dns.GetHostEntry);
            IPAddress ipAddress = ipHostInfo.AddressList[0];
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, port);
            */
            IPHostEntry ipHostInfo = Dns.Resolve(Dns.GetHostName());
           // IPHostEntry ipHostInfo = Dns.GetHostEntry("localhost");
            IPAddress ipAddress = ipHostInfo.AddressList.ToList().Find(p=>p.AddressFamily==AddressFamily.InterNetwork);
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, port);
         
            

            // Create a TCP/IP socket.
            listener = new Socket(AddressFamily.InterNetwork,
            SocketType.Stream, ProtocolType.Tcp );

            // Bind the socket to the local endpoint and 
            // listen for incoming connections.
            
            listener.Bind(localEndPoint);
            listener.Listen(10); //max number of pending connections
            // Start listening for connections.
            log.WriteToLog("Binding DONE");
          }

        public Socket AcceptConnection() {
            log.WriteToLog("Accepting connection ...");
            Socket handler = listener.Accept();
            log.WriteToLog("Accepting connection DONE");
            return handler;
        }

        public NetworkStream getStream(Socket s) { 
            NetworkStream netStream = new NetworkStream(s, true);
            return netStream;
        }
}
}