using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncBox_Server
{
    public static class Logging
    {
        private static readonly object locker = new object();

        public static void WriteToLog(string message)
        {
            lock (locker)
            {
                try { 
                StreamWriter SW;
                DateTime t = DateTime.Now;
                SW = File.AppendText("C:\\backup\\Log_server.txt");
                SW.WriteLine(t.ToString() + " - (" + System.Threading.Thread.CurrentThread.ManagedThreadId + ")" + message);
                SW.Close();
                }
                catch { }
            }
        }
    }
}
