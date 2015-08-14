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
    //Parameter class -> object to pass to each thread serving the clients
    public class Param
    {
        public SyncSocketListener listener;
        public string dbConnection;
        public Logging log;

        public Param(SyncSocketListener listener,string dbConnection,Logging log){
            this.listener = listener;
            this.dbConnection = dbConnection;
            this.log = log;
        }
    }
    
    /// <summary>
    /// Logica di interazione per MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public const int NTHREAD = 2;   //# of threads
        
        db db_handle;
        string dbConnection;
        public SyncSocketListener listener;
        Logging log = new Logging();
        
        Thread[] thread_array = new Thread[NTHREAD];    //Array of threads //TODO improve?? HOW?

        public MainWindow()
        {
            InitializeComponent();
            log.WriteToLog("-----SERVER START-----");
            
        }

        //NON SONO PER NIENTE SICURO CHE FUNZIONI!!
        //per il disturittore
        //per il metodo abort! per robustezza e perchè potrebbe esserci un try catch nel thread
         ~MainWindow()
        {
            log.WriteToLog("Shutting down MainWindow...");
            int i = 0;
            for (i = 0; i < NTHREAD; i++)
            {
                if (thread_array[i] != null) { 
                    thread_array[i].Abort();
                    log.WriteToLog("THREAD ABORTED - " + i);
                }
            }
            log.WriteToLog("Shutting down MainWindow DONE");
            
        }

        private void b_start_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                log.WriteToLog("starting the server ..."); 
                
                db_handle = new db(db_path_textbox.Text);
                dbConnection = db_path_textbox.Text;
                db_handle.start();
                
                //nuovo oggetto listener
                listener = new SyncSocketListener(1500,log);
                listener.Start();

                //in release, here is MULTITHREAD!!!!!!!!!! DONE
                //si può fare MUCH MUCH BETTER!
                
                Param p = new Param(listener,dbConnection,log);
                
                int i = 0;
                for (i = 0; i < NTHREAD; i++)
                {              
                    thread_array[i] = new Thread(new ParameterizedThreadStart(manage_Client));
                    thread_array[i].Start(p);

                    log.WriteToLog("THREAD STARTED - " + i);
                }

                log.WriteToLog("starting the server DONE");
            }
            catch (Exception exc)
            {
                MessageBox.Show("Exception trying starting server! " + exc.ToString());
                log.WriteToLog("Exception trying starting server! " + exc.ToString());
            }
        }

        //Cuncurrency Thread Function! manages multiple clients requests
        public void manage_Client(object obj)
        {
            Param p = (Param)obj;
            NetworkStream connected_stream;
            proto_server protoServer;
            try
            {
                while (true) 
                {
                    Socket s = p.listener.AcceptConnection();
                    connected_stream = p.listener.getStream(s);
                    protoServer = new proto_server(connected_stream, p.dbConnection,p.log);
                    
                    try
                    {
                        while (true)
                        {
                            protoServer.manage();
                        }
                    }
                    catch (System.IO.IOException se) {
                        p.log.WriteToLog("qui io penso che la connessione sia stata chiusa dal client!" + se.ToString());
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("qui io penso che ci sia stato un altro tipo di eccezione" + ex.ToString());
                        p.log.WriteToLog("qui io penso che ci sia stato un altro tipo di eccezione" + ex.ToString());
                    }
                }
            }catch (Exception exc) 
            {
                MessageBox.Show(exc.ToString());
                p.log.WriteToLog(exc.ToString());
            }
        }
    }
}
