using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace SyncBox_Server
{
    ////Parameter class -> object to pass to each thread serving the clients
    //public class Param
    //{
    //    public SyncSocketListener listener;
    //    public string dbConnection;

        
    //    // public Logging log;

    //    //public Param(SyncSocketListener listener,string dbConnection,Logging log){
    //    //    this.listener = listener;
    //    //    this.dbConnection = dbConnection;
    //    //   // this.log = log;
    //    //}
    //}
    
    /// <summary>
    /// Logica di interazione per MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public Task tAcceptConnections;
        private CancellationTokenSource cts = new CancellationTokenSource();

        // public const int NTHREAD = 2;   //# of threads
        
       // db db_handle;
        string dbConnection;
        public SyncSocketListener listener;
      
        //  Logging log = new Logging();
        
        //Thread[] thread_array = new Thread[NTHREAD];    //Array of threads //TODO improve?? HOW?

        public MainWindow()
        {
            InitializeComponent();
            Logging.WriteToLog("-----SERVER START-----");
            
        }

        //NON SONO PER NIENTE SICURO CHE FUNZIONI!!
        //per il disturittore
        //per il metodo abort! per robustezza e perchè potrebbe esserci un try catch nel thread
        // ~MainWindow()
        //{
        //    Logging.WriteToLog("Shutting down MainWindow...");
        //    int i = 0;
        //    for (i = 0; i < NTHREAD; i++)
        //    {
        //        if (thread_array[i] != null) { 
        //            thread_array[i].Abort();
        //            Logging.WriteToLog("THREAD ABORTED - " + i);
        //        }
        //    }
        //    Logging.WriteToLog("Shutting down MainWindow DONE");
            
        //}

        private void b_start_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Logging.WriteToLog("starting the server ...");
                starting_ui();

                db.setDbConn(db_path_textbox.Text);

                dbConnection = db_path_textbox.Text;
                db.start();
                
                //nuovo oggetto listener
                listener = new SyncSocketListener(1500);
                listener.Start();

                tAcceptConnections = new Task(listener.acceptConnections, TaskCreationOptions.LongRunning);
                tAcceptConnections.Start();

                //tAcceptConnections = Task.Factory.StartNew(listener.acceptConnections, cts, TaskCreationOptions.LongRunning, TaskScheduler.Default);

               // tAcceptConnections.Start(cts);
                //in release, here is MULTITHREAD!!!!!!!!!! DONE
                //si può fare MUCH MUCH BETTER!
                /*
                Param p = new Param(listener,dbConnection,log);
                
                int i = 0;
                for (i = 0; i < NTHREAD; i++)
                {              
                    thread_array[i] = new Thread(new ParameterizedThreadStart(manage_Client));
                    thread_array[i].IsBackground = true;
                    thread_array[i].Start(p);

                    Logging.WriteToLog("THREAD STARTED - " + i);
                }
                */
                started_ui();
                Logging.WriteToLog("starting the server DONE");
            }
            catch (Exception exc)
            {
                MessageBox.Show("Exception trying starting server! " + exc.ToString());
                Logging.WriteToLog("Exception trying starting server! " + exc.ToString());
            }
        }

        private void b_stop_Click(object sender, RoutedEventArgs e)
        {
            Logging.WriteToLog("stopping the server ...");
            closing_ui();

           // StopServerThreads();
            //TODO REDO!
            if (cts != null)
                cts.Cancel();

            Logging.WriteToLog("stopping the server DONE");
            closed_ui();

        }

        //public void StopServerThreads() { 
        //    int i = 0;
        //    for (i = 0; i < NTHREAD; i++) {
        //        if (thread_array[i] != null)
        //        {
        //            if (thread_array[i].IsAlive) {
        //                thread_array[i].Abort();
                        
        //            }
        //        }
        //    }
        //}

        //Cuncurrency Thread Function! manages multiple clients requests


        //public void manage_Client(object obj)
        //{
        //    Param p = (Param)obj;
        //    NetworkStream connected_stream;
        //    proto_server protoServer;
        //    try
        //    {
        //        while (true)
        //        {
        //            Socket s = p.listener.AcceptConnection();
        //            connected_stream = p.listener.getStream(s);
        //            protoServer = new proto_server(connected_stream, p.dbConnection, p.log);

        //            try
        //            {
        //                while (true)
        //                {
        //                    protoServer.manage();
        //                }
        //            }
        //            catch (ThreadAbortException te)
        //            {
        //                Logging.WriteToLog("ThreaAbortExeption catching ...");
        //                connected_stream.Close();
        //                s.Shutdown(SocketShutdown.Both);
        //                s.Close();
        //                Logging.WriteToLog("ThreaAbortExeption catching DONE");
        //                throw;
        //            }
        //            catch (System.IO.IOException se)
        //            {
        //                p.Logging.WriteToLog("qui io penso che la connessione sia stata chiusa dal client!" + se.ToString());
        //            }
        //            catch (Exception ex)
        //            {
        //                MessageBox.Show("qui io penso che ci sia stato un altro tipo di eccezione" + ex.ToString());
        //                p.Logging.WriteToLog("qui io penso che ci sia stato un altro tipo di eccezione" + ex.ToString());
        //            }
        //        }
        //    }
        //    catch (Exception exc)
        //    {
        //        MessageBox.Show(exc.ToString());
        //        p.Logging.WriteToLog(exc.ToString());
        //    }
        //}

       

        private void starting_ui()
        {
            b_start.Content = "Starting ...";
            b_start.IsEnabled = false;
            disable_tb();
        }

        private void started_ui()
        {
            b_start.Content = "Start";
            b_start.IsEnabled = true;
            b_start.Visibility = Visibility.Hidden;
            b_stop.Visibility = Visibility.Visible;
            disable_tb();
        }

        private void closing_ui()
        {
            b_stop.Content = "Stopping ...";
            b_stop.IsEnabled = false;
            disable_tb();
        }

        private void closed_ui()
        {
            b_stop.Content = "Stop";
            b_stop.IsEnabled = true;
            b_stop.Visibility = Visibility.Hidden;
            b_start.Visibility = Visibility.Visible;
            enable_tb();
        }

        private void enable_tb()
        {
            db_path_textbox.IsEnabled = true;
            port_tb.IsEnabled = true;
        }
        private void disable_tb()
        {
            db_path_textbox.IsEnabled = false;
            port_tb.IsEnabled = false;
        }

    }
}
