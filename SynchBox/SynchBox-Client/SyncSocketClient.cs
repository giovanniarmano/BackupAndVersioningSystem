#define debug
//#undef debug

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Windows;

namespace SynchBox_Client
{
    public class SyncSocketClient
    {   
        //TODO Remove @ release
        private string remoteAddressString = "127.0.0.1";
        private int port = 1500;
        private Socket sender;
        private bool connected = false;
        NetworkStream netStream;
        Logging log;
        //static SyncSocketClient this_ = null;

        //default ctor
        public SyncSocketClient() { 
        }
        
        //ctor
        public SyncSocketClient(string ip, int port,Logging log) {
            //if (this_ == null) { }
            remoteAddressString = ip;
            this.port = port;
            this.log = log;
          //  this_ = this;
        }

        public void setPort(int port) { this.port = port; }
        public void setAddress(string address) { this.remoteAddressString = address; }


        public NetworkStream getStream() {
            if (connected == false)
                throw new Exception("Socket not connected!");

            netStream = new NetworkStream(sender, true);

            return netStream;
        }

        public void Connect() {
        try {
            //log.WriteToLog("connecting ...");
            // Establish the remote endpoint for the socket.
            IPHostEntry ipHostInfo = Dns.Resolve(Dns.GetHostName());
            IPAddress remoteAddress = IPAddress.Parse(remoteAddressString);
            IPEndPoint remoteEP = new IPEndPoint(remoteAddress,port);

            // Create a TCP/IP  socket.
            sender = new Socket(AddressFamily.InterNetwork, 
                SocketType.Stream, ProtocolType.Tcp );

            // Connect the socket to the remote endpoint. Catch any errors.
            
            sender.Connect(remoteEP);

            //log.WriteToLog("connecting DONE");
            //log.WriteToLog("connected to " + sender.RemoteEndPoint.ToString());
                
            connected = true;

        } catch (Exception e) {
            //MessageBox.Show( e.ToString());
            log.WriteToLog("connecting FAILED");
            throw;
        }
    }

        public void Close() 
        {
            try
            {
                // Release the socket.
                sender.Shutdown(SocketShutdown.Both);
                sender.Close();
            } catch (Exception e) 
            {
                MessageBox.Show( e.ToString());
            }
        }
    }
}
