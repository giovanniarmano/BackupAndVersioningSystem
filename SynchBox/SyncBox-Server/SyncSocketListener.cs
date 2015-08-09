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
        private int port = 0;

        public SyncSocketListener(){
            port = 1001;
        }

        public SyncSocketListener(int port){
            this.port=port;
        }

        public bool Start()
        {
            //NO MULTITHREAD FUNCTION

            string data = null;

            // Data buffer for incoming data.
            byte[] bytes = new Byte[1024];

            MessageBox.Show("listener start!");

            // Establish the local endpoint for the socket.
            // Dns.GetHostName returns the name of the 
            // host running the application.
            IPHostEntry ipHostInfo = Dns.Resolve(Dns.GetHostName());
            IPAddress ipAddress = ipHostInfo.AddressList[0];
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, port);

            // Create a TCP/IP socket.
            Socket listener = new Socket(AddressFamily.InterNetwork,
                SocketType.Stream, ProtocolType.Tcp );

            // Bind the socket to the local endpoint and 
            // listen for incoming connections.
            try {
                listener.Bind(localEndPoint);
                listener.Listen(10); //max number of pending connections

                // Start listening for connections.
                while (true) {
                    MessageBox.Show("Waiting for a connection...");
                    // Program is suspended while waiting for an incoming connection.
                    Socket handler = listener.Accept();
                    data = null;

                    // An incoming connection needs to be processed.
                    while (true) {
                        bytes = new byte[1024];
                        int bytesRec = handler.Receive(bytes);
                        data += Encoding.ASCII.GetString(bytes,0,bytesRec);
                        if (data.IndexOf("<EOF>") > -1) {
                            break;
                        }
                    }

                    // Show the data on the console.
                    MessageBox.Show( "Text received : {0}", data);

                    // Echo the data back to the client.
                    byte[] msg = Encoding.ASCII.GetBytes(data);

                    handler.Send(msg);
                    handler.Shutdown(SocketShutdown.Both);
                    handler.Close();
                }
            
            } catch (Exception e) {
                MessageBox.Show(e.ToString());
                return false;
            }

            MessageBox.Show("\nPress ENTER to continue...");
            //Console.Read();

            
    }
}
}