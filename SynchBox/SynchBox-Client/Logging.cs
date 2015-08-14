using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SynchBox_Client
{
    public class Logging
    {
        public Logging()
        {

        }

        private static readonly object locker = new object();

        public void WriteToLog(string message)
        {
            lock (locker)
            {
                StreamWriter SW;
                DateTime t = DateTime.Now;
                SW = File.AppendText("E:\\backup\\Log_client.txt");
                SW.WriteLine(t.ToString() + " - (" + System.Threading.Thread.CurrentThread.ManagedThreadId + ")" + message);
                SW.Close();
            }
        }
    }
}
