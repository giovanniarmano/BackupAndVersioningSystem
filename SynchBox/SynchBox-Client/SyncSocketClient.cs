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
        // Data buffer for incoming data.
        //byte[] bytes = new byte[1024];

        // Connect to a remote device.
        try {
            // Establish the remote endpoint for the socket.
            // This example uses port 11000 on the local computer.
            IPHostEntry ipHostInfo = Dns.Resolve(Dns.GetHostName());
    //        IPAddress ipAddress = ipHostInfo.AddressList[0];
            IPAddress remoteAddress = IPAddress.Parse(remoteAddressString);
            IPEndPoint remoteEP = new IPEndPoint(remoteAddress,port);

            // Create a TCP/IP  socket.
            sender = new Socket(AddressFamily.InterNetwork, 
                SocketType.Stream, ProtocolType.Tcp );

            // Connect the socket to the remote endpoint. Catch any errors.
            try
            {
                sender.Connect(remoteEP);

                log.WriteToLog("Socket connected to" +
                    sender.RemoteEndPoint.ToString());

                // Encode the data string into a byte array.
                //byte[] msg = Encoding.ASCII.GetBytes("This is a test<EOF>");

                // Send the data through the socket.
                //int bytesSent = sender.Send(msg);

                // Receive the response from the remote device.
                //int bytesRec = sender.Receive(bytes);
                //MessageBox.Show("Echoed test = {0}",
                //    Encoding.ASCII.GetString(bytes,0,bytesRec));

                //return sender;
                connected = true;

            }
/*    
            catch (ArgumentNullException ane)
            {
                MessageBox.Show("ArgumentNullException :" + ane.ToString());
                throw;
            }
            catch (SocketException se)
            {
                MessageBox.Show("SocketException :" + se.ToString());
                throw;
            }
 */
           catch (Exception e)
            {
                //MessageBox.Show("Unexpected exception : " + e.ToString());
                throw;
            }
            

        } catch (Exception e) {
            //MessageBox.Show( e.ToString());
            throw;
        }
    }


        public void Close() {
            try
            {
                // Release the socket.
                sender.Shutdown(SocketShutdown.Both);
                sender.Close();
            } catch (Exception e) {
            MessageBox.Show( e.ToString());
        }
        }



    }
}
