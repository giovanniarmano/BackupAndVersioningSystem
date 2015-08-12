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
using System.Windows;

namespace SynchBox_Client
{
    /// <summary>
    /// Logica di interazione per MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        SyncSocketClient sender_SyncSocketClient;
        NetworkStream sender_stream;
        proto_client protoClient;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }

        private void b_login_login_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //check non null textbox
                //throw new Exception("Complete Login/Registration information");

                //set them to the class params

                sender_SyncSocketClient = new SyncSocketClient("127.0.0.1", 1500);
                sender_SyncSocketClient.Connect();

                sender_stream = sender_SyncSocketClient.getStream();
                protoClient = new proto_client(sender_stream);

               // proto_client.login_c log = new proto_client.login_c();

                protoClient.do_login("usr", "pwd");

                //set them to the calass params for login

                //istantiate myprotocol

                //myprotocol login
                //throws exception

                //set session, user etc parameters

                //UI login-> logout & not possible to login again

                //spostati su home

            }
            catch (Exception exc)
            {
                MessageBox.Show("not possible to login or connect to server! Error : " + exc.ToString());
            }
        }
    }
}