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
    public class SyncSocketListener
    {   
        CancellationToken ct;
        TcpListener listener;
        int clientCounter = 0;
        int port = -1;
      
        public SyncSocketListener(int port, CancellationToken ct){
            this.port=port;
            this.ct = ct;
        }

        public void Stop()
        {
            listener.Stop();
        }

        public void Start()
        {   
            listener = new TcpListener(IPAddress.Any, port);

            listener.Start();
            Logging.WriteToLog("Binding DONE");

            //just fire and forget. We break from the "forgotten" async loops
            //in AcceptClientsAsync using a CancellationToken from `cts`
            Logging.WriteToLog("Accept Clients Async ...");
            AcceptClientsAsync(listener, ct);
            Logging.WriteToLog("Accept Clients Async STARTED (in other Task)");
        }

        async Task AcceptClientsAsync(TcpListener listener, CancellationToken ct)
        {
            //TODO struttura + complex per gestire clientlist!
            clientCounter = 0;
            while (!ct.IsCancellationRequested)
            {
                TcpClient client = await listener.AcceptTcpClientAsync()
                                                    .ConfigureAwait(false);
                clientCounter++;
                //once again, just fire and forget, and use the CancellationToken
                //to signal to the "forgotten" async invocation.
                Logging.WriteToLog("Managing client "+ clientCounter + " ...");
                manageClient(client, clientCounter, ct);
            }
        }

        async Task manageClient(TcpClient client,int count,CancellationToken ct)
        {
            Logging.WriteToLog("Client "+count+" Connected ...");
            using (client)
            {
                //Logging.WriteToLog("Managing Client (in loop) ...");
                NetworkStream ns = client.GetStream();
               // bool connectionClosed = false;
                bool exceptionCatch = false;
                while (!exceptionCatch && !ct.IsCancellationRequested)
                {
                    //manage task ?? //Ha senso dare il lavoro ad un task oppure basta il task che fa manageclient
                    try
                    {
                        Logging.WriteToLog("managing in new task ...");
                        Task t = Task.Factory.StartNew(() =>
                            proto_server.manage(ns, ct, ref exceptionCatch)
                            );
                        await t;

                        
                        //Logging.WriteToLog("managing in current task ...");
                        //proto_server.manage(ns, ct);
                    }
                    catch (Exception ex)
                    {   
                        //inutile! Eccezione catturata nel thread
                        Logging.WriteToLog("//TODO// Guarda il Log con queste eccezioni e gestiscile meglio!");
                        Logging.WriteToLog(" public static void manageSocket(Socket s) - SyncSocketListener");
                        Logging.WriteToLog(ex.ToString());
                       // connectionClosed = true;
                    }
                }
            }
        }
    }
}