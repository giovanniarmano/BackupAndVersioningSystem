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
using System.Threading;

namespace SynchBox_Client
{
    public class SyncSocketClient
    {   
        TcpClient client = null;

        IPAddress ipAddress = null;
        int port = -1;
        NetworkStream netStream = null;
   
        bool connected = false;
        CancellationToken ct ;

        public SyncSocketClient(string ip, int port, CancellationToken ct) 
        {
            ipAddress = IPAddress.Parse(ip);
            this.port = port;
            this.ct = ct;
        }

        public NetworkStream getStream() {
            if (connected == false)
                throw new Exception("Socket not connected!");
            return netStream;
        }

        public async Task<bool> StartClientAsync()
        {
            try
            {
                Logging.WriteToLog("Starting client async ...");
                client = new TcpClient();
                await client.ConnectAsync(ipAddress, port);
                netStream = client.GetStream();
                connected = true;
                Logging.WriteToLog("Starting client async DONE");
            }
            catch (Exception e)
            {
                Logging.WriteToLog("Error in Connecting to the server.");
                Logging.WriteToLog(e.ToString());
                //throw;
                return false;
            }

            return true;
        }

        public void Close() {
            //magari mando un msg close prima
            if (client != null)
            {
                Logging.WriteToLog("Closing Stream & TcpClient ...");
                client.GetStream().Close();
                client.Close();
                Logging.WriteToLog("Closing Stream & TcpClient DONE");
            }
            else 
            {
                Logging.WriteToLog("Closing Stream & TcpClient ... ALREADY CLOSED OR NULL");
            }
            client = null;
            ipAddress = null;
            port = -1;
            netStream = null;
            connected = false;
        }

    }
}
