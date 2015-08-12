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
    /// <summary>
    /// Logica di interazione per MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        db db_handle;
        string dbConnection;
        SyncSocketListener listener;

        public MainWindow()
        {
            InitializeComponent();
            //MessageBox.Show("Begin");
        }

        private void b_start_Click(object sender, RoutedEventArgs e)
        {
            try
            {   
                
                db_handle = new db(db_path_textbox.Text);
                dbConnection = db_path_textbox.Text;
                //alloc db structs 
                db_handle.start();
                
                //nuovo oggetto listener
                listener = new SyncSocketListener(1500);
                //mi metto in ascolto! Bind
                listener.Start();

                //in release, here is MULTITHREAD!!!!!!!!!!

                NetworkStream connected_stream;
                proto_server protoServer;

                Socket s = listener.AcceptConnection();
                connected_stream = listener.getStream(s);

                protoServer = new proto_server(connected_stream, dbConnection);
                //protoServer.setDb(db_handle);
                //protoServer.manage_login();
                protoServer.manage();

            }
            catch (Exception exc)
            {
                MessageBox.Show("Exception trying starting server! " + exc.ToString());
            }
        }
    }
}
