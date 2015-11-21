using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;
using System.IO;
using System.Text.RegularExpressions;
//using System.Windows;

namespace SynchBox_Client
{
    

    /// <summary>
    /// Logica di interazione per MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //L'unica istanza di MainWindow conosce e istanzia un riferimento alla classe SessionVars
        //L'istanza si chiama sessionVars
        //L'idea è quella di usare questa classe per tenere traccia dei parametri di sessione (unici e non duplicati in quanto siamo sul client!)
        
        public class SessionVars
        {
            public CancellationTokenSource cts;
            public SyncSocketClient socketClient = null;

            public string ip_str = "";
            public int port_int = -1;
            public string port_str = "";

            public bool connected = false;

            public string username = "";
            public string uid_str = "";

            public string path = "";
            public int lastSyncId = -1;
        }

        public SessionVars sessionVars;
        public SynchClient synchClient = new SynchClient();
        
        private void initializeSessionParam()
        {
            sessionVars.username = "";
            sessionVars.uid_str = "";
            sessionVars.ip_str = "";
            sessionVars.port_str = "";
            sessionVars.port_int = -1;
            sessionVars.connected = false;
            sessionVars.path = "";
            sessionVars.lastSyncId = -1;
        }

        private void initializeSyncParam()
        {
            synchClient.remoteFiles.Clear();
        }

        public MainWindow()
        {
            InitializeComponent();
            sessionVars = new SessionVars();
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
            //begin_login_ui();
            loginRegisterAsync("login"); 
            
            Logging.WriteToLog("calling login async DONE");
        }

        private void b_register_Click(object sender, RoutedEventArgs e)
        {
            Logging.WriteToLog("calling register async ...");
            //begin_register_ui();
            loginRegisterAsync("register");
            Logging.WriteToLog("calling register async DONE");
        }

        
        /// <summary>
        /// Effettua Login/Register a seconda della stringa {login | register} passata come parametro
        /// </summary>
        /// <param name="op"></param>
        /// <returns></returns>
        private async Task loginRegisterAsync(string op) {
            try
            {   
                Logging.WriteToLog("Logging-in/registering async ...");
                           
                validateTextBoxes();

                sessionVars.cts = new CancellationTokenSource();
                begin_login_ui();

                Logging.WriteToLog("connecting ...");
               // sender_SyncSocketClient =
                    await myStartAsync(ip_tb.Text, int.Parse(port_tb.Text), sessionVars.cts.Token);

                if (!sessionVars.connected)
                    throw new Exception("Connection FAILED");

                Logging.WriteToLog("connecting DONE  " + sessionVars.ip_str + ":" + sessionVars.port_int);

               //sender_stream = sender_SyncSocketClient.getStream();
                //protoClient = new proto_client(sender_stream);

                proto_client.login_c login_result;
                string usr = username_tb.Text; string pwd = password_tb.Password;
                switch (op)
                {
                    case "login":
                        //HERE MULTITASK

                        Task<proto_client.login_c> t = Task.Factory.StartNew<proto_client.login_c>(()=>
                        proto_client.do_login(sessionVars.socketClient.getStream(), usr, pwd, sessionVars.cts.Token)
                        );
                        
                        login_result = await t;

                        if (!login_result.is_logged)
                        {
                            Logging.WriteToLog("logging in FAILED");
                            //System.Windows.MessageBox.Show("Login Failed");
                            throw new Exception("Login Failed!");    
                        }
                        Logging.WriteToLog("logging in SUCCESSFULL");

                        sessionVars.username = login_result.username;
                        sessionVars.uid_str = login_result.uid.ToString();

                        Logging.WriteToLog("user:" + sessionVars.username + " - uid:" + sessionVars.uid_str);
                
                        login_ui();
                    break;

                    case "register":
                        //HERE MULTITASK
                        Task<proto_client.login_c> t1 = Task.Factory.StartNew<proto_client.login_c>(()=>
                        proto_client.do_register(sessionVars.socketClient.getStream(), usr, pwd, sessionVars.cts.Token)
                        );
                        
                        login_result = await t1;

                        if (!login_result.is_logged)
                        {
                            Logging.WriteToLog("logging in FAILED");
                           // System.Windows.MessageBox.Show("Registration Failed");
                            throw new Exception("Login Failed!");
                        }
                        Logging.WriteToLog("logging in SUCCESSFULL");

                        //set them to the calass params for login
                        sessionVars.username = login_result.username;
                        sessionVars.uid_str = login_result.uid.ToString();

                        Logging.WriteToLog("user:" + sessionVars.username + " - uid:" + sessionVars.uid_str);
                
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
                sessionVars.connected = false;
                //TODO ??
                b_login_login_Click(this, null);
                end_login_register_ui();
            }
            catch (Exception exc)
            {
                System.Windows.MessageBox.Show(exc.Message);                
                Logging.WriteToLog("not possible to login or connect to server! Error : " + exc.ToString());
                end_login_register_ui();
            }
        }


        private async Task<SyncSocketClient> myStartAsync(string _ip, int _port,CancellationToken ct)
        {
           // Thread.Sleep(3000);
            //throw new NotImplementedException();
            if (sessionVars.connected)
            {
                if (_ip.CompareTo(sessionVars.ip_str) == 0 && _port == sessionVars.port_int)
                {
                    return sessionVars.socketClient;
                }
                else
                {
                    if (sessionVars.socketClient != null) {
                        //close client //open new one
                        sessionVars.socketClient.Close();
                        sessionVars.socketClient = null;
                        sessionVars.ip_str = "";
                        sessionVars.port_int = -1;
                        sessionVars.connected = false;
                    }
                }
            }
            sessionVars.socketClient = new SyncSocketClient(_ip, _port,ct);
            
            bool successful_connect = await sessionVars.socketClient.StartClientAsync();
            if (!successful_connect)
            {
                Logging.WriteToLog("Connecting FAILED");      
            }
            else {
                sessionVars.connected = true;
                sessionVars.ip_str = _ip;
                sessionVars.port_int = _port;
            }
            return sessionVars.socketClient;
        }

       

        private void setNameLogin() {
            welcome_l.Content = "welcome, " + sessionVars.username + " @ " + sessionVars.ip_str + ":" + sessionVars.port_str;
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
            sessionVars.username = "";
            sessionVars.uid_str = "";
            
            proto_client.do_logout(sessionVars.socketClient.getStream());
            sessionVars.socketClient.Close();
            //.Close();
            initializeSessionParam();
            //ui logout
            logout_ui();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            Logging.WriteToLog("Begin syncronization ...");
            if (!Directory.Exists(local_path.Text))
            {
                System.Windows.Forms.MessageBox.Show("Invalid directory");
                return;
            }
            sessionVars.path = local_path.Text;
            Logging.WriteToLog("Loading information ...");
            int result = synchClient.getInitInformation(sessionVars);

            if (Directory.EnumerateFileSystemEntries(local_path.Text).Any() && result==1)
            {
                System.Windows.Forms.MessageBox.Show("Impossible to sincronize a not empty directory");
                return;
            }

            initializeSyncParam();
            synchClient.StartSyncAsync(sessionVars.socketClient.getStream(), sessionVars);

            //ShowRemoteFileSystem(sessionVars.socketClient.getStream());
            
        }

        private void Button_click_sfoglia(object sender, System.EventArgs e)
        {
            FolderBrowserDialog FolderBrowserDialog1 = new FolderBrowserDialog();
            FolderBrowserDialog1.ShowNewFolderButton = true;
            FolderBrowserDialog1.Description = "Select a Folder";

            if (FolderBrowserDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
               local_path.Text = FolderBrowserDialog1.SelectedPath;
            }
        }



        private void button_Click_2(object sender, RoutedEventArgs e)
        {
            //Test List protobuf
            proto_client.do_test(sessionVars.socketClient.getStream(), 5, sessionVars.cts.Token);
        }

        private TreeViewItem CreateFileNode(DirectoryInfo di)
        {
            var fileNode = new TreeViewItem();
            fileNode.MouseLeftButtonUp += treeItem_Selected;


            foreach (var file in di.GetFiles())
            {
                fileNode.Items.Add(file.Name);
            }

            return fileNode;
        }

        private void ShowRemoteFileSystem(NetworkStream networkStream)
        {
            treeView_1.Items.Clear();

            proto_client.ListResponse remoteFileList;
            remoteFileList = proto_client.ListRequestLastWrapper(networkStream);

            List<proto_client.FileListItem> SortedList = remoteFileList.fileList.OrderBy(o => o.folder).ToList(); //controllare che non si possa fare nella query lato server
            Lookup<string, proto_client.FileListItem> lookup = (Lookup<string, proto_client.FileListItem>)SortedList.ToLookup(p => p.folder, p => p);

            var keys = lookup.Select(g => g.Key).ToList();

            
            List<string[]> listTmp = new List<string[]>();
            List<string> listString = new List<string>();

            for (int i = 0; i < keys.Count; i++)
            {
                string cartella = keys[i];
                string[] source = cartella.Split(new Char[] { '\\' });
                listString = source.ToList();
                listString.Add(cartella);
                listTmp.Add(listString.ToArray());
            }
            Lookup<int, string[]> directoryDept = (Lookup<int, string[]>)listTmp.ToLookup(p => p.Length-3 , p => p);

            treeView_1.Items.Add(CreateDirectoryNode(directoryDept, 0));

        }

        private object CreateDirectoryNode(Lookup<int, string[]> directoryDept, int p)
        {
            var directoryNode = new TreeViewItem();
            foreach (var directory in directoryDept[p])
            {
                directoryNode = new TreeViewItem() { Header = directory[p], Tag = directory[directory.Length-1]};
                directoryNode.MouseLeftButtonUp += directoryTreeItem_Selected;
                foreach (var subdirectory in directory)
                {
                    directoryNode.Items.Add(CreateDirectoryNode(directoryDept, p + 1));
                }
            }

            return directoryNode;
        }

        private TreeViewItem CreateDirectoryNode(DirectoryInfo di)
        {
            var directoryNode = new TreeViewItem() { Header = di.Name, Tag = di.FullName };
            directoryNode.MouseLeftButtonUp += directoryTreeItem_Selected;

            foreach (var directory in di.GetDirectories())
            {
                directoryNode.Items.Add(CreateDirectoryNode(directory));

            }

            return directoryNode;
        }

        private void ShowFileSystem(System.Windows.Controls.TreeView treeView, string path)
        {
            treeView.Items.Clear();
            var rootDirectoryInfo = new DirectoryInfo(path);

            treeView.Items.Add(CreateDirectoryNode(rootDirectoryInfo));
        }

        private void directoryTreeItem_Selected(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            TreeViewItem item = sender as TreeViewItem;
            string path = item.Tag.ToString();
            var rootDirectoryInfo = new DirectoryInfo(path);

            treeView_2.Items.Clear();

            foreach (var file in rootDirectoryInfo.GetFiles())
            {
                var fileNode = new TreeViewItem() { Header = file.Name};
                fileNode.MouseLeftButtonUp += treeItem_Selected;
                treeView_2.Items.Add(fileNode);
            }
        }

        //generare il terzo blocco con tutte le revisione di quel file
        private void treeItem_Selected(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            TreeViewItem item = sender as TreeViewItem;
            string path = item.Tag.ToString();
            var fileInfo = new FileInfo(path);

            treeView_3.Items.Clear();

            /*

            foreach (var file in rootDirectoryInfo.GetFiles())
            {
                var fileNode = new TreeViewItem() { Header = file.Name };
                fileNode.MouseLeftButtonUp += treeItem_Selected;
                treeView_3.Items.Add(fileNode);
            }*/
        }

    }
}