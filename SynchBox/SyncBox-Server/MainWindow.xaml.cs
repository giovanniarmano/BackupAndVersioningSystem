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
using System.Windows;
using System.Net;
using System.Net.Sockets;


namespace SyncBox_Server
{
   // public struct Param
   // {
   //     public SyncSocketListener listener;
   //     public string dbConnection;
   // }

    /// <summary>
    /// Logica di interazione per MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        db db_handle;
        string dbConnection;
        SyncSocketListener listener;
        Logging log = new Logging();

        public MainWindow()
        {
            InitializeComponent();
            log.WriteToLog("Initialize component");
            //MessageBox.Show("Begin");
        }

        private void b_start_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                log.WriteToLog("trying to start the server ..."); 
                db_handle = new db(db_path_textbox.Text);
                dbConnection = db_path_textbox.Text;
                //alloc db structs 
                log.WriteToLog("trying to get db handle");
                db_handle.start();
                
                log.WriteToLog("trying to get new listener");
                //nuovo oggetto listener
                listener = new SyncSocketListener(1500,log);
                //mi metto in ascolto! Bind
                listener.Start();

                log.WriteToLog("listener started");
                

                //in release, here is MULTITHREAD!!!!!!!!!!

     //           Param p;// = new Param(listener,dbConnection);
     //           p.listener = listener;
     //           p.dbConnection = dbConnection;

                log.WriteToLog("trying to manage client");
                manage_Client();

            }
            catch (Exception exc)
            {
                MessageBox.Show("Exception trying starting server! " + exc.ToString());
                log.WriteToLog("Exception trying starting server! " + exc.ToString());
            }
        }

        public void manage_Client()
        {
            NetworkStream connected_stream;
            proto_server protoServer;
            try
            {
                while (true) 
                {
                    log.WriteToLog("manage client - accepting connection");
                    //SyncSocketListener listener = p.getListener();
                    Socket s = listener.AcceptConnection();
                    connected_stream = listener.getStream(s);

                    log.WriteToLog("manage client - connection accepted!");

                    protoServer = new proto_server(connected_stream, dbConnection,log);
                    //protoServer.setDb(db_handle);
                    //protoServer.manage_login();
                    //try
                    //{
                        while (true)
                        {
                            log.WriteToLog("try to manage protoserver");
                            protoServer.manage();
                            log.WriteToLog("protoserver managed");
                        }
                   // }
                   // catch (Exception ex) {
                   //     MessageBox.Show(ex.ToString());
                   // }
                }
            }catch (Exception exc) 
            {
                MessageBox.Show(exc.ToString());
                log.WriteToLog(exc.ToString());
            }
        }
    }
}
