using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SynchBox_Client
{
    public static class Logging
    {
        

        private static readonly object locker = new object();

        public static void WriteToLog(string message)
        {
            lock (locker)
            {
                StreamWriter SW;
                DateTime t = DateTime.Now;
                SW = File.AppendText("D:\\backup\\Log_client.txt");
                SW.WriteLine(t.ToString() + " - (" + System.Threading.Thread.CurrentThread.ManagedThreadId + ")" + message);
                SW.Close();
            }
        }
    }
}
