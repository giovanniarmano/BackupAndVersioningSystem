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
//using System.Windows;

namespace SynchBox_Client
{   
    /// <summary>
    /// Logica di interazione per MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        SyncSocketClient sender_SyncSocketClient;
        NetworkStream sender_stream;
        //proto_client protoClient;
        
        CancellationTokenSource cts;
        
        SyncSocketClient cur_client = null;
        
        string ip = "";
        int int_port = -1;
        string port = "";

        bool connected = false;

        string username = "";
        string uid = "";
       
        private void initializeSessionParam()
        {
            username = "";
            uid = "";
            ip = "";
            port = "";
            int_port = -1;
            connected = false;
        }

        public MainWindow()
        {
            InitializeComponent();
            Logging.WriteToLog("-----CLIENT STARTED------");
            initializeSessionParam();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }

        private void b_login_login_Click(object sender, RoutedEventArgs e)
        {
            Logging.WriteToLog("calling login async ...");
            //multitask fatto dentro ! qui sotto
            begin_login_ui();
            loginRegisterAsync("login"); 
            
            Logging.WriteToLog("calling login async DONE");
        }

        //login
        //register
        private async Task loginRegisterAsync(string op) {
            try
            {   
                Logging.WriteToLog("Logging-in/registering async ...");
                
                cts = new CancellationTokenSource();
                validateTextBoxes();

                Logging.WriteToLog("connecting ...");
                sender_SyncSocketClient = await myStartAsync(ip_tb.Text, int.Parse(port_tb.Text),cts.Token);

                if (!connected)
                    throw new Exception("Connection FAILED");

                Logging.WriteToLog("connecting DONE  " + ip + ":" + int_port);

                sender_stream = sender_SyncSocketClient.getStream();
                //protoClient = new proto_client(sender_stream);

                proto_client.login_c login_result;
                string usr = username_tb.Text; string pwd = password_tb.Password;
                switch (op)
                {
                    case "login":
                        //HERE MULTITASK

                        Task<proto_client.login_c> t = Task.Factory.StartNew<proto_client.login_c>(()=>
                        proto_client.do_login(sender_stream, usr, pwd, cts.Token)
                        );
                        
                        login_result = await t;

                        if (!login_result.is_logged)
                        {
                            Logging.WriteToLog("logging in FAILED");
                            throw new Exception("Login Failed!");    
                        }
                        Logging.WriteToLog("logging in SUCCESSFULL");

                        username = login_result.username;
                        uid = login_result.uid.ToString();

                        Logging.WriteToLog("user:" + username + " - uid:" + uid);
                
                        login_ui();
                    break;

                    case "register":
                        //HERE MULTITASK
                        Task<proto_client.login_c> t1 = Task.Factory.StartNew<proto_client.login_c>(()=>
                        proto_client.do_register(sender_stream, usr, pwd, cts.Token)
                        );
                        
                        login_result = await t1;

                        if (!login_result.is_logged)
                        {
                            Logging.WriteToLog("logging in FAILED");
                            throw new Exception("Login Failed!");
                        }
                        Logging.WriteToLog("logging in SUCCESSFULL");

                        //set them to the calass params for login
                        username = login_result.username;
                        uid = login_result.uid.ToString();

                        Logging.WriteToLog("user:" + username + " - uid:" + uid);
                
                        login_ui();

                    break;

                    default:

                    break;
                }

                end_login_register_ui();
                //Logging.WriteToLog("set login name, clear textbox, disable text box");
                //istantiate myprotocol

                //myprotocol login
                //throws exception

                //set session, user etc parameters

                //UI login-> logout & not possible to login again

                //set them (textbox values) to the class params

                //spostati su home

            }
            catch (System.IO.IOException se)
            {
                Logging.WriteToLog("Socket exception!" + se.ToString());
                connected = false;
                //TODO ??
                b_login_login_Click(this, null);
                end_login_register_ui();
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);                
                Logging.WriteToLog("not possible to login or connect to server! Error : " + exc.ToString());
                end_login_register_ui();
            }
        }


        private async Task<SyncSocketClient> myStartAsync(string _ip, int _port,CancellationToken ct)
        {
           // Thread.Sleep(3000);
            //throw new NotImplementedException();
            if (connected)
            {
                if (_ip.CompareTo(ip) == 0 && _port == int_port)
                {
                    return cur_client;
                }
                else
                {
                    if (cur_client != null) { 
                        //close client //open new one
                        cur_client.Close();
                        cur_client = null;
                        ip = "";
                        int_port = -1;
                        connected = false;
                    }
                }
            }
            cur_client = new SyncSocketClient(_ip, _port,ct);
            
            bool successful_connect = await cur_client.StartClientAsync();
            if (!successful_connect)
            {
                Logging.WriteToLog("Connecting FAILED");      
            }
            else { 
                connected = true;
                ip = _ip;
                int_port = _port;
            }
            return cur_client;
        }

        private void b_register_Click(object sender, RoutedEventArgs e)
        {
            Logging.WriteToLog("calling register async ...");
            begin_register_ui();
            loginRegisterAsync("register");
            Logging.WriteToLog("calling register async DONE");
        }

        private void setNameLogin() {
            welcome_l.Content = "welcome, " + username + " @ " + ip + ":" + port;
        }

        private void unsetNameLogin() {
            welcome_l.Content = "Welcome, no user logged in !";
        }


        private void validateTextBoxes() { 
            if(username_tb.Text.Equals(""))
                throw new Exception ("No username!");
            if (password_tb.Password.Equals("")) 
                throw new Exception("No password!");
            if (ip_tb.Text.Equals(""))
                throw new Exception("No IP!");
            if (port_tb.Text.Equals(""))
                throw new Exception("No Port!");
            try{
             int t;
             t = int.Parse(port_tb.Text);
             if (t <= 0 || t > 65535)
                 throw new Exception("port exc");
            }
            catch (Exception exc)
            {
                throw new Exception("Port Format not correct! Must Be a number (0..65535)");
            }
        }

        private void begin_login_ui()
        {
            b_login_login.Content = "Logging ...";
            b_login_login.IsEnabled = false;
            b_register.IsEnabled = false;
        }

        private void begin_register_ui()
        {
            b_register.Content = "Registering ...";
            b_login_login.IsEnabled = false;
            b_register.IsEnabled = false;
        }

        private void end_login_register_ui() {
            b_login_login.Content = "Login";
            b_register.Content = "or Register";
            b_login_login.IsEnabled = true;
            b_register.IsEnabled = true;
            
        }

        private void login_ui() {
            setNameLogin();
            //clearTextBox();
            disableTextBox();

        }

        private void logout_ui()
        {
            clearTextBox();
            enableTextBox();
            unsetNameLogin();
        }

        private void clearTextBox() {
            username_tb.Clear();
            password_tb.Clear();
            //ip_tb.Clear();
            //port_tb.Clear();
        }

        private void enableTextBox()
        {   username_tb.IsEnabled = true;
            password_tb.IsEnabled = true;
            ip_tb.IsEnabled = true;
            port_tb.IsEnabled = true;
            b_login_login.Visibility = Visibility.Visible;
            b_register.Visibility = Visibility.Visible;
            b_logout_login.Visibility = Visibility.Hidden;
        }
        private void disableTextBox()
        {   username_tb.IsEnabled = false;
            password_tb.IsEnabled = false;
            ip_tb.IsEnabled = false;
            port_tb.IsEnabled = false;
            b_login_login.Visibility = Visibility.Hidden;
            b_register.Visibility = Visibility.Hidden;
            b_logout_login.Visibility = Visibility.Visible;
        }

        private void b_logout_login_Click(object sender, RoutedEventArgs e)
        {
            //do logout
            username = "";
            uid = "";

            //ui logout
            logout_ui();
        }
    }
}