using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Windows;
using ProtoBuf;
using System.Threading;
using System.Data;
using System.IO;
using System.Security.Cryptography;
//using ProtoBuf.Data;

namespace SynchBox_Client
{
   

    public static partial class proto_client
    {
        
        //private NetworkStream netStream= null;
        //Logging log;

        //ctor
        //public proto_client(NetworkStream s) { netStream = s; }

        public static void do_test(NetworkStream netStream, int n, CancellationToken ct)
        {
            try
            {
                /*
                int sessionid = BeginSessionWrapper(netStream);

                AddOk addOk;

                int hh = 0;
                for (hh = 0; hh < 5; hh++)
                {

                    string filename = RandomString(5) + ".txt";
                    string folder = "\\temp\\" + filename;
                    string filePath = "C:\\backup\\temp\\" + filename;
                    var fileStream = File.Create(filePath);
                    fileStream.Close();
                    Logging.WriteToLog("Writing on file " + filePath);
                    string text = RandomString(20);
                    Logging.WriteToLog("Text__>" + text);
                    File.WriteAllText(filePath, text);

                    Add add = new Add();

                    add.filename = filename;
                    add.folder = folder;
                    add.fileDump = File.ReadAllBytes(filePath);

                    addOk = AddWrapper(netStream, ref add);
                    Logging.WriteToLog(addOk.ToString());



                }

                EndSessionWrapper(netStream, sessionid);



                Logging.WriteToLog(ListRequestAllWrapper(netStream).ToString());
                Logging.WriteToLog(ListRequestLastWrapper(netStream).ToString());

                Logging.WriteToLog("SyncId =" + GetSynchIdWrapper(netStream).ToString());
                */

                //TODO

                //ListLast
                //ListAll
                //GetSync id

                string basepath = "C:\\backup\\temp";
                string rand = RandomString(4);

                string temp_rand = "\\" + rand + "\\";
                string temp_rand_restore = "\\" + rand + "_restore\\";

                Directory.CreateDirectory(basepath + temp_rand);
                Directory.CreateDirectory(basepath + temp_rand_restore);

                //folder /temp/RAND/
                int session = BeginSessionWrapper(netStream);
                //Begin Session
                //Add 15 01_RAND.txt 15_RAND.txt Files
                int i = 0;
                string filename;
                string text;
                string bff;
                //basepath
                //folder
                //filename

                //bff (basepath+folder+filename)
                AddOk addOk;
                string folder = temp_rand;

                FileListItem[] fileItemList = new FileListItem[16];
                for (i = 0; i < 16; i++)
                    fileItemList[i] = new FileListItem();

                for (i = 1; i <= 15; i++)
                {
                    //create file
                    filename = i.ToString() + "_" + rand + ".txt";
                    bff = basepath + folder + filename;
                    var fileStream = File.Create(bff);
                    fileStream.Close();

                    //write random string
                    text = RandomString(20);
                    File.WriteAllText(bff, text);
                    //create add struct & populate
                    Add add = new Add();

                    add.filename = filename;
                    add.folder = folder;
                    add.fileDump = File.ReadAllBytes(bff);

                    addOk = AddWrapper(netStream, ref add);
                    fileItemList[i].fid = addOk.fid;
                    fileItemList[i].rev = addOk.rev;
                    Logging.WriteToLog(addOk.ToString());

                }

                //end session
                EndSessionWrapper(netStream, session);

                session = BeginSessionWrapper(netStream);
                //begin session
                //update 03-07_RAND.txt
                UpdateOk updateOk;
                for (i = 3; i < 8; i++)
                {
                    //create file
                    filename = i.ToString() + "_" + rand + ".txt";
                    bff = basepath + folder + filename;
                    //write random string
                    text = RandomString(20);
                    File.WriteAllText(bff, text);
                    //create add struct & populate

                    Update update = new Update();

                    update.fid = fileItemList[i].fid;
                    update.fileDump = File.ReadAllBytes(bff);

                    updateOk = UpdateWrapper(netStream, ref update);
                    fileItemList[i].rev = updateOk.rev;
                    Logging.WriteToLog(updateOk.ToString());
                }



                //delete 11-14_RAND.txt

                DeleteOk deleteOk;
                for (i = 11; i <= 14; i++)
                {
                    //create file
                    filename = i.ToString() + "_" + rand + ".txt";
                    bff = basepath + folder + filename;
                    //write random string
                    //text = RandomString(20);
                    //File.WriteAllText(bff, text);
                    //create add struct & populate

                    Delete delete = new Delete();


                    delete.fid = fileItemList[i].fid;

                    deleteOk = DeleteWrapper(netStream, ref delete);
                    fileItemList[i].fid = -1;
                    Logging.WriteToLog(deleteOk.ToString());
                }

                EndSessionWrapper(netStream, session);

                //end session

                //folder /temp/RAND_restore/
                //getlastlist
                Logging.WriteToLog(ListRequestLastWrapper(netStream).ToString());
                Logging.WriteToLog(ListRequestAllWrapper(netStream).ToString());

                GetList getList = new GetList();
                getList.fileList = new List<FileToGet>();
                n = 0;
                for (i = 1; i <= 15; i++)
                {
                    FileToGet fileToGet = new FileToGet();
                    if (fileItemList[i].fid > 0)
                    {
                        n++;
                        fileToGet.fid = fileItemList[i].fid;
                        fileToGet.rev = fileItemList[i].rev;
                        getList.fileList.Add(fileToGet);
                    }
                }
                getList.n = n;
                                
                GetListWrapper(netStream, ref getList);
                GetResponse getResponse = new GetResponse();
                folder = temp_rand_restore;
                for (i = 0; i <n ; i++)
                {
                    GetResponseWrapper(netStream, ref getResponse);

                    filename = getResponse.fileInfo.fid + "_" + rand + ".txt";
                    bff = basepath + folder + filename;
                    var fileStream = File.Create(bff);
                    fileStream.Close();
                    
                    File.WriteAllBytes(bff, getResponse.fileDump);
                }

            }
            catch (Exception e)
            {
                Logging.WriteToLog(e.ToString());
            }

        }

        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public static void do_logout(NetworkStream netStream)
        {
            try { 
            messagetype_c msgtype = new messagetype_c
            {
                msgtype = (byte)CmdType.Logout,
                accepted = false,
            };

            Logging.WriteToLog("LOGGING OUT (sending logout msg to the server) ...");
            Serializer.SerializeWithLengthPrefix(netStream, msgtype, PrefixStyle.Base128);
            Logging.WriteToLog("LOGGING OUT DONE");
            }
            catch (Exception e)
            {
                Logging.WriteToLog(e.ToString());
                //DEBUG --remove in release
                MessageBox.Show(e.Message);
            }
        }

        public static login_c do_login(NetworkStream netStream,string _username, string _password,CancellationToken ct){
            messagetype_c msgtype = new messagetype_c
            {
                msgtype = (byte)CmdType.Login,
                accepted = false,
            };

            Logging.WriteToLog("LOGGING IN ...");
            Serializer.SerializeWithLengthPrefix(netStream, msgtype, PrefixStyle.Base128);

            //Logging.WriteToLog("Attempting reading data!");
            messagetype_c msgtype_r = Serializer.DeserializeWithLengthPrefix<messagetype_c>(netStream, PrefixStyle.Base128);

            if (msgtype_r.accepted == false)
                throw new Exception("Message Type not Accepted by Server.\n" + msgtype_r.ToString());

            login_c login = new login_c
            {
                is_logged = false,
                uid = -1,
                username = _username,
                password = _password
            };

            //MessageBox.Show("GOT CONNECTION Stream: sending data...");
            Serializer.SerializeWithLengthPrefix(netStream, login, PrefixStyle.Base128);

            //MessageBox.Show("Attempting reading data!");
            login_c login_r = Serializer.DeserializeWithLengthPrefix<login_c>(netStream, PrefixStyle.Base128);
            

            Logging.WriteToLog("sent - " + login.ToString() + "\nreceived - " + login_r.ToString());
            //Logging.WriteToLog("logging in: sending data...");
            

            return login_r;
        }

        public static login_c do_register(NetworkStream netStream,string _username, string _password,CancellationToken ct)
        {
            messagetype_c msgtype = new messagetype_c
            {
                msgtype = (byte)CmdType.Register,
                accepted = false,
            };

            Logging.WriteToLog("REGISTER ...");

            //MessageBox.Show("GOT CONNECTION Stream: sending data...");
            Serializer.SerializeWithLengthPrefix(netStream, msgtype, PrefixStyle.Base128);

            //MessageBox.Show("Attempting reading data!");
            messagetype_c msgtype_r = Serializer.DeserializeWithLengthPrefix<messagetype_c>(netStream, PrefixStyle.Base128);

            if (msgtype_r.accepted == false)
                throw new Exception("Message Type not Accepted by Server.\n" + msgtype_r.ToString());

            login_c login = new login_c
            {
                is_logged = false,
                uid = -1,
                username = _username,
                password = _password
            };

            //MessageBox.Show("GOT CONNECTION Stream: sending data...");
            Serializer.SerializeWithLengthPrefix(netStream, login, PrefixStyle.Base128);

            //MessageBox.Show("Attempting reading data!");
            login_c login_r = Serializer.DeserializeWithLengthPrefix<login_c>(netStream, PrefixStyle.Base128);


            //Logging.WriteToLog("SENT REGISTER\n" + login.ToString() + "\n\nRCVD REGISTER\n" + login_r.ToString());
            Logging.WriteToLog("sent - " + login.ToString() + "\nreceived - " + login_r.ToString());

            return login_r;
        }

        //public void my_sender(enum CmdType, )


        /*
        public static void do_sync(NetworkStream netStream, SessionVars vars, CancellationToken ct)
        {

            //controllo se vars.uid ha già effettuato almeno una sincronizzazione!

            //list last -> server

            //list last response

            //HO filesystem_server

            //se no 
            {
                //foreach item in list
                { 
                    //get

                    //get response

                    //write on disk
                }
            }

            while (!ct.IsCancellationRequested) { 
                //update/populate filesystem struct
                //HO filesystem_client

                //compare_filesystems(server,client);

                //sleep (timesleep)
            }

        }
        */


        /*
        private static DataTable populateFS(ref DataTable dt, string rootFolder){
                dt = new DataTable();
                dt.Clear();
                dt.Columns.Add("uid");   //user_id
                dt.Columns.Add("fid");   //file id
                dt.Columns.Add("cid");   //changeset id 
                dt.Columns.Add("rev");   //revision
                dt.Columns.Add("name");  //filename
                dt.Columns.Add("folder_path");//path
                dt.Columns.Add("md5");   //md5
                
                List<string> search = Directory.GetFiles(rootFolder, "*.*").ToList();

                foreach(string item in search)
                {
                
                        //calculate md5 in bytearray
                        byte[] filehash;

                        using (var md5 = MD5.Create())
                        {
                            using (var stream = File.OpenRead(item))
                            {
                                filehash =  md5.ComputeHash(stream);
                            }
                        }


                }   
    
        }
        */

        /*
        //compare filesystems
        {   
            //ADD A FAST METHOD TO RECOGNISE nochange CASE- for example id sync

            //request changeset #
            //begin new changeset state = not finished!

            //foreach client file
            {
                //if present in the server
                    //scegli il più recente!

                    //se client + recente, update

                    //se server + recente, get last version

                //if not present in the server add

                //if non ho dei file che sono sul server
                    //get di tutti quei file
             
                //SEMPRE
                //fai tipo un update del numero di modifiche apportate al changeset!
                
            }
            
        }
         */


        public static string DataTableToString(DataTable Table)
        {
            StringBuilder sb = new StringBuilder("");
            foreach (DataRow dataRow in Table.Rows)
            {
                sb.Append("\n");
                foreach (var item in dataRow.ItemArray)
                {
                    sb.Append(item + "|");
                    //Console.WriteLine(item);
                }
            }
            return sb.ToString();
        }

        public static void populate_dictionary(string path)
        {
            Dictionary<string, string> localFiles = new Dictionary<string, string>();
            DirSearch(path,localFiles);
            //string[] directories = Directory.GetDirectories(path);

        }

        private static void DirSearch(string sDir, Dictionary<string, string>  localFiles)
        {
            try
            {
                var md5 = MD5.Create();
                foreach (string d in Directory.GetDirectories(sDir))
                {
                    foreach (string f in Directory.GetFiles(d))
                    {
                        using (var stream = File.OpenRead(f))
                        {
                            localFiles.Add(f,System.Convert.ToBase64String(md5.ComputeHash(stream)));
                        }
                    }
                    DirSearch(d,localFiles);
                }
            }
            catch (System.Exception excpt)
            {
                Console.WriteLine(excpt.Message);
            }
        }

        public static string CalculateMD5Hash(byte[] byteArray)
        {
            // step 1, calculate MD5 hash from input
            MD5 md5 = System.Security.Cryptography.MD5.Create();
            //byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
            byte[] hash = md5.ComputeHash(byteArray);

            // step 2, convert byte array to hex string
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("X2"));
            }
            return sb.ToString();
        }

    }
        
}
