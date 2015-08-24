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
    /// <summary>
    /// Logica di interazione per MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public Task tAcceptConnections;
        CancellationTokenSource cts;
        string dbConnection;
        public SyncSocketListener listener;

        public MainWindow()
        {
            InitializeComponent();
            Logging.WriteToLog("-----SERVER START-----");
        }

        private void b_start_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Logging.WriteToLog("starting the server ...");
                starting_ui();
                cts = new CancellationTokenSource();

                db.setDbConn(db_path_textbox.Text);

                dbConnection = db_path_textbox.Text;
                db.start();
                
                //nuovo oggetto listener
                listener = new SyncSocketListener(1500,cts.Token);
                listener.Start();

                started_ui();
                Logging.WriteToLog("starting the server DONE");
            }
            catch (Exception exc)
            {
                MessageBox.Show("Exception trying starting server! " + exc.ToString());
                Logging.WriteToLog("Exception trying starting server! " + exc.ToString());
                closed_ui();
            }
        }

        private void b_stop_Click(object sender, RoutedEventArgs e)
        {
            Logging.WriteToLog("stopping the server ...");
            closing_ui();

           // StopServerThreads();
            //TODO REDO!
            
            if (cts != null) {
                Logging.WriteToLog("Cancelling Tasks ... ");
                cts.Cancel();
            }
            //TODO Check if not throw exceotons
            listener.Stop();

            Logging.WriteToLog("stopping the server DONE");
            closed_ui();
        }

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
