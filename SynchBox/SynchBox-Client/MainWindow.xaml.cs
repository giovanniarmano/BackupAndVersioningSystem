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
   
        public class SessionVars
        {
            public CancellationTokenSource cts;
            public SyncSocketClient socketClient = null;
            public volatile bool isSynchronizationActive = false;

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
        public List<proto_client.FileListItem> remoteFileListAll;
        public List<proto_client.FileListItem> remoteFileListLast;
        internal static MainWindow tw;

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

        public MainWindow()
        {
            InitializeComponent();
            tw = this;
            disable_tabitems();
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
            welcome_l.Content = "Welcome, " + sessionVars.username + " @ " + sessionVars.ip_str + ":" + sessionVars.port_int.ToString();
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


        private void login_ui() {
            setNameLogin();
            disableTextBox();
        }

        private void logout_ui()
        {
            clearTextBox();
            enableTextBox();
            unsetNameLogin();
            disable_tabitems();
        }

        private void clearTextBox() {
            username_tb.Clear();
            password_tb.Clear();
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



        private void end_login_register_ui()
        {
            b_login_login.Content = "Login";
            b_register.Content = "or Register";
            b_login_login.IsEnabled = true;
            b_register.IsEnabled = true;

            
            enable_tabitems();
        }

        private void disable_tabitems()
        {
            var tab = tabControl.Items[1] as TabItem;
            tab.IsEnabled = false;
         
            tab = tabControl.Items[2] as TabItem;
            tab.IsEnabled = false;
            tab.IsEnabled = false;
        }

        private void enable_tabitems()
        {
            var tab = tabControl.Items[1] as TabItem;
            tab.IsEnabled = true;

            tab = tabControl.Items[2] as TabItem;
            tab.IsEnabled = false;
        }

        private void enable_tabitems_sync()
        {
            var tab = tabControl.Items[1] as TabItem;
            tab.IsEnabled = true;

            tab = tabControl.Items[2] as TabItem;
            tab.IsEnabled = true;
        }


        public void b_logout_login_Click(object sender, RoutedEventArgs e)
        {
            //do logout
            sessionVars.username = "";
            sessionVars.uid_str = "";

            //added! stop synch before do logout!
            //copied from stop synch!
            sessionVars.isSynchronizationActive = false;
            start_synch_stopped_ui();

            proto_client.do_logout(sessionVars.socketClient.getStream());
            sessionVars.socketClient.Close();
      
            initializeSessionParam();
            //ui logout
            logout_ui();
        }

        //fa il logout quando il server si chiude! + sposta la visuale sulla finestra di login!
        public void my_do_logout()
        {
            //do logout
            sessionVars.username = "";
            sessionVars.uid_str = "";

            //added! stop synch before do logout!
            //copied from stop synch!
            sessionVars.isSynchronizationActive = false;
            start_synch_stopped_ui();

            proto_client.do_logout(sessionVars.socketClient.getStream());
            sessionVars.socketClient.Close();
            //.Close();
            initializeSessionParam();
            //ui logout
            tabControl.SelectedIndex = 0;
            logout_ui();
        }
        

        //button begin Syncronization
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            sessionVars.isSynchronizationActive = true;
            Logging.WriteToLog("Begin syncronization ...");
            if (!Directory.Exists(local_path.Text))
            {
                System.Windows.Forms.MessageBox.Show("Invalid directory");
                return;
            }
            start_synch_begin_ui();
            sessionVars.path = local_path.Text;
            Logging.WriteToLog("Loading information ...");
            int result = synchClient.getInitInformation(sessionVars);

            if (Directory.EnumerateFileSystemEntries(local_path.Text).Any() && result==1)
            {
                System.Windows.Forms.MessageBox.Show("Impossible to sincronize a not empty directory");
                start_synch_stopped_ui();
                return;
            }
            
            synchClient.setEnvironment(sessionVars.socketClient.getStream(), sessionVars);
            ThreadStart MyDelegate = new ThreadStart(synchClient.StartSyncAsync);
            Thread MyThread = new Thread(MyDelegate);
            MyThread.Start();

            //TODO: aggiungere la terminazione di un thread, al posto del ct!!

            start_synch_end_ui();

            //sposto sull'evento click updateRestoreButton
            ShowRemoteFileSystem(sessionVars.socketClient.getStream());

            treeView_2.Items.Clear();
            treeView_3.Items.Clear();
            enable_tabitems_sync();

        }

        //fare stop synch anche quando si fa logout!
        private void b_stop_sync_Click(object sender, RoutedEventArgs e)
        {
            sessionVars.isSynchronizationActive = false;
            start_synch_stopped_ui();
        }

        private void start_synch_end_ui()
        {
            b_start_sync.Visibility = Visibility.Hidden;
            b_stop_sync.Visibility = Visibility.Visible;

            //b_start_sync.Content = "Stop Syncronization";
            //b_start_sync.IsEnabled = true;
            local_path.IsEnabled = false;
            button_sfoglia.IsEnabled = false;
        }

        private void start_synch_stopped_ui()
        {
            b_start_sync.Visibility = Visibility.Visible;
            b_stop_sync.Visibility = Visibility.Hidden;

            b_start_sync.Content = "Start Syncronization";
            b_start_sync.IsEnabled = true;

            local_path.IsEnabled = true;
            button_sfoglia.IsEnabled = true;
        }
        private void start_synch_begin_ui()
        {
            b_start_sync.Content = "Starting Syncronization...";
            b_start_sync.IsEnabled = false;
            b_start_sync.Visibility = Visibility.Visible;
            b_stop_sync.Visibility = Visibility.Hidden;

            local_path.IsEnabled = false;
            button_sfoglia.IsEnabled = false;

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

            ThreadStart MyDelegate = new ThreadStart(drawRestore);
            Thread MyThread = new Thread(MyDelegate);
            MyThread.Start();

        }

        private void drawRestore()
        {

            getRemoteInformation(sessionVars.socketClient.getStream());

            proto_client.FileListItem root = new proto_client.FileListItem();
            root.filename = "Root\\";
            root.fid = 1;

            if (!treeView_1.Dispatcher.CheckAccess())
            {
                treeView_1.Dispatcher.Invoke(
                    System.Windows.Threading.DispatcherPriority.Background,
                    new Action(
                        delegate()
                        {
                            treeView_1.Items.Add(CreateDirectoryNode(remoteFileListLast, 1, root));
                        }
                        ));
            }
            else
            {
                treeView_1.Items.Add(new TreeViewItem() { Header = "Loading information" });
            }
        }

        private void getRemoteInformation(NetworkStream networkStream)
        {
            proto_client.ListResponse remoteFileInfo;

            remoteFileInfo = proto_client.ListRequestLastWrapper(networkStream);
            remoteFileListLast = remoteFileInfo.fileList.ToList();

            remoteFileInfo = proto_client.ListRequestAllWrapper(networkStream);
            remoteFileListAll = remoteFileInfo.fileList.ToList();
        }

        private object CreateDirectoryNode(List<proto_client.FileListItem> remoteFileList, int p, proto_client.FileListItem parentDirectory)
        {
            var directoryNode = new TreeViewItem() { Header = parentDirectory.filename.Substring(0, parentDirectory.filename.Length - 1), Tag = parentDirectory.fid };
            if (parentDirectory.deleted)
            {
                directoryNode.Foreground = Brushes.Red;
            }
            directoryNode.MouseLeftButtonUp += directoryTreeItem_Selected;

            foreach (var directory in remoteFileList)
            {
                if (directory.dir && directory.folder_id == p && directory.folder_id != directory.fid)
                {
                    directoryNode.Items.Add(CreateDirectoryNode(remoteFileList, directory.fid, directory));
                }
            }

            return directoryNode;
        }

        private void directoryTreeItem_Selected(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            TreeViewItem item = sender as TreeViewItem;
            string folderId = item.Tag.ToString();
            int i = 0;

            treeView_2.Items.Clear();
            treeView_3.Items.Clear();
            dirLabel.Content = item.Header;
            fileLabel.Content = "";

            foreach (var file in remoteFileListLast)
            {
                if(!file.dir && file.folder_id.ToString().CompareTo(folderId) == 0)
                {
                    i++;
                    var fileNode = new TreeViewItem() { Header = file.filename, Tag = file.fid };
                    fileNode.MouseLeftButtonUp += treeItem_Selected;
                    if (file.deleted)
                    {
                        fileNode.Foreground = Brushes.Red;
                    }
                    treeView_2.Items.Add(fileNode);
                }
            }
            if (i == 0)
            {
                var fileNode = new TreeViewItem() { Header = "Directory without files" };
                treeView_2.Items.Add(fileNode);
            }
        }

        //generare il terzo blocco con tutte le revisione di quel file
        private void treeItem_Selected(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            TreeViewItem item = sender as TreeViewItem;
            string fileId = item.Tag.ToString();

            treeView_3.Items.Clear();
            fileLabel.Content = item.Header;

            foreach (var file in remoteFileListAll)
            {
                if (file.fid.ToString().CompareTo(fileId) == 0 && !file.deleted)
                {
                    var fileNode = new TreeViewItem() { Header = file.timestamp, Tag = file.fid + "_" + file.rev };
                    fileNode.MouseLeftButtonUp += treeItem_Revision;
                    treeView_3.Items.Add(fileNode);
                }
            } 
        }

        private void treeItem_Revision(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;

            TreeViewItem item = sender as TreeViewItem;
            string tag = item.Tag.ToString();
            string[] fileInfo = tag.Split('_');


            System.Windows.Forms.DialogResult dialogResult = System.Windows.Forms.MessageBox.Show("Do you want to restore this file and override the current version?", "Restore dialog", MessageBoxButtons.YesNoCancel);
            if (dialogResult == System.Windows.Forms.DialogResult.Yes)
            {
                System.Windows.Forms.MessageBox.Show("You're going to override file: " + fileInfo[0] + " with is " + fileInfo[1] + " revision" );
                foreach (var file in remoteFileListAll)
                {
                    if (file.fid.ToString().CompareTo(fileInfo[0]) == 0 && file.rev.ToString().CompareTo(fileInfo[1]) == 0)
                    {
                        synchClient.setFileInfoContainer(fileInfo);
                        ThreadStart MyDelegate = new ThreadStart(synchClient.overrideLocalCopy);
                        Thread MyThread = new Thread(MyDelegate);
                        MyThread.Start();
                        return;
                    }
                } 
            }
            else if (dialogResult == System.Windows.Forms.DialogResult.No)
            {
                System.Windows.Forms.MessageBox.Show("You're going to restore file: " + fileInfo[0] + " at revision " + fileInfo[1] + " in a new file");
                foreach (var file in remoteFileListAll)
                {
                    if (file.fid.ToString().CompareTo(fileInfo[0]) == 0 && file.rev.ToString().CompareTo(fileInfo[1]) == 0)
                    {
                        synchClient.setFileInfoContainer(fileInfo);
                        ThreadStart MyDelegate = new ThreadStart(synchClient.saveNewCopy);
                        Thread MyThread = new Thread(MyDelegate);
                        MyThread.Start();
                        return;
                    }
                } 
            }
            else if (dialogResult == System.Windows.Forms.DialogResult.Cancel)
            {
                System.Windows.Forms.MessageBox.Show("Operation aborted");
            }
        }

        private void updateRestoreButton_Click(object sender, RoutedEventArgs e)
        {
            //sposto sull'evento click updateRestoreButton
            if (sessionVars.path == null)
            {
                synchClient.getInitInformation(sessionVars);
            }

            treeView_2.Items.Clear();
            treeView_3.Items.Clear();
            dirLabel.Content = "";
            fileLabel.Content = "";

            ShowRemoteFileSystem(sessionVars.socketClient.getStream());
        }
    }
}