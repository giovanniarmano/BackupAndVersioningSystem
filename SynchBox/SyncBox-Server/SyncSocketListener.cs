using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Windows;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace SyncBox_Server
{
    // State object for reading client data asynchronously
    public class StateObject
    {
        // Client  socket.
        public Socket workSocket = null;
        // Size of receive buffer.
        //public const int BufferSize = 1024;
        // Receive buffer.
        //public byte[] buffer = new byte[BufferSize];
        // Received data string.
        //public StringBuilder sb = new StringBuilder();
    }

    public class SyncSocketListener
    {
        // Thread signal.
        public static ManualResetEvent allDone = new ManualResetEvent(false);

        private int port = -1;
       // Logging log;

        //private Socket handler;
        private Socket listener;

        public SyncSocketListener(int port){
            this.port=port;
           // this.log = log;
        }

        public void Start()
        {
            //NO MULTITHREAD FUNCTION
            Logging.WriteToLog("Binding ...");
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
           // IPHostEntry ipHostInfo = Dns.Resolve(Dns.GetHostName());
            IPHostEntry ipHostInfo = Dns.GetHostEntry("localhost");
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
            Logging.WriteToLog("Binding DONE");
        }

        public void acceptConnections(){
            
            while (true)
            {
                // Set the event to nonsignaled state.
                allDone.Reset();

                // Start an asynchronous socket to listen for connections.
                Logging.WriteToLog("Accepting connection ...");
                listener.BeginAccept(
                    new AsyncCallback(AcceptCallback),
                    listener);

                // Wait until a connection is made before continuing.
                allDone.WaitOne();
            }
          
        }
    

        public static void AcceptCallback(IAsyncResult ar)
        {
            // Signal the main thread to continue.
            allDone.Set();

            Logging.WriteToLog("Accepting connection DONE");

            // Get the socket that handles the client request.
            Socket listener = (Socket)ar.AsyncState;
            Socket handler = listener.EndAccept(ar);

            // Create the state object.
            StateObject state = new StateObject();
            state.workSocket = handler;

            // START A TASK THAT MANAGES ONE SOCKET
            //cancelling token
            //ADD TO TASKlIST
            manageSocket(handler);
            
            //handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
            //    new AsyncCallback(ReadCallback), state);
        }

        //A TASK PER SOCKET MANAGER
        //MAGAGE OVER THE SOCKET
        //if !cancelle manage call in a task

        public static void manageSocket(Socket s) {
            NetworkStream ns = new NetworkStream(s, true);
            bool connectionClosed = false;
            while (!connectionClosed /*&& !cancelling*/)
            {   
                //manage task ??
                try
                {
                    
                    proto_server.manage(ns);
                }
                catch (Exception ex) {
                    Logging.WriteToLog("//TODO// Guarda il Log con queste eccezioni e gestiscile meglio!");
                    Logging.WriteToLog(" public static void manageSocket(Socket s) - SyncSocketListener");
                    Logging.WriteToLog(ex.ToString());
                    connectionClosed = true;
                }
            }
        }





        //public static void ReadCallback(IAsyncResult ar)
        //{
        //    String content = String.Empty;

        //    // Retrieve the state object and the handler socket
        //    // from the asynchronous state object.
        //    StateObject state = (StateObject)ar.AsyncState;

        //    Socket handler = state.workSocket;

        //    // Read data from the client socket. 
        //    int bytesRead = handler.EndReceive(ar);

        //    if (bytesRead > 0)
        //    {
        //        // There  might be more data, so store the data received so far.
        //        state.sb.Append(Encoding.ASCII.GetString(
        //            state.buffer, 0, bytesRead));

        //        // Check for end-of-file tag. If it is not there, read 
        //        // more data.
        //        content = state.sb.ToString();
        //        if (content.IndexOf("<EOF>") > -1)
        //        {
        //            // All the data has been read from the 
        //            // client. Display it on the console.
        //            Console.WriteLine("Read {0} bytes from socket. \n Data : {1}",
        //                content.Length, content);
        //            // Echo the data back to the client.
        //            Send(handler, content);
        //        }
        //        else
        //        {
        //            // Not all data received. Get more.
        //            handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
        //            new AsyncCallback(ReadCallback), state);
        //        }
        //    }
        //}

        //private static void Send(Socket handler, String data) {
        //// Convert the string data to byte data using ASCII encoding.
        //byte[] byteData = Encoding.ASCII.GetBytes(data);

        //// Begin sending the data to the remote device.
        //handler.BeginSend(byteData, 0, byteData.Length, 0,
        //    new AsyncCallback(SendCallback), handler);
        //}

        //private static void SendCallback(IAsyncResult ar) {
        //    try {
        //        // Retrieve the socket from the state object.
        //        Socket handler = (Socket) ar.AsyncState;

        //        // Complete sending the data to the remote device.
        //        int bytesSent = handler.EndSend(ar);
        //        Console.WriteLine("Sent {0} bytes to client.", bytesSent);

        //        handler.Shutdown(SocketShutdown.Both);
        //        handler.Close();

        //    } catch (Exception e) {
        //        Console.WriteLine(e.ToString());
        //    }
        //}

        //public Socket AcceptConnection() {
        //    Logging.WriteToLog("Accepting connection ...");
        //    Socket handler = listener.Accept();
        //    Logging.WriteToLog("Accepting connection DONE");
        //    return handler;
        //}

        //public NetworkStream getStream(Socket s) { 
        //    NetworkStream netStream = new NetworkStream(s, true);
        //    return netStream;
        //}
}
}