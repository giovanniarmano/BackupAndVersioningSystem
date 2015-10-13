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
        {/*
            myObj mobj = new myObj();
            mobj.int1 = 1;
            mobj.int2 = 2;
            mobj.s1 = "Ciao Mamma";
            mobj.s2 = "Protobuf HELP ME";

            test_c tc = new test_c();
            tc.intlist = new List<myObj>();

            int i = 0;
            for (i=0;i< n; i++)
            {
                tc.intlist.Add(mobj);
            }

            Logging.WriteToLog("------TESTING-----");
            Logging.WriteToLog("list tostring");

            for (i = 0; i < tc.intlist.Count; i++) {
                Logging.WriteToLog(i.ToString()+ tc.intlist[i].ToString());
            }

            messagetype_c msgtype = new messagetype_c
            {
                msgtype = (byte)CmdType.Test,
                accepted = false,
            };

            Serializer.SerializeWithLengthPrefix(netStream, msgtype, PrefixStyle.Base128);

            
            Serializer.SerializeWithLengthPrefix(netStream, tc, PrefixStyle.Base128);
            */

            try
            {
                

                //BeginSession
                BeginSession beginSession = new BeginSession();
                beginSession.sessionid = -1;

                messagetype_c mt = new messagetype_c();
                mt.msgtype = (Byte)CmdType.BeginSession;

                Serializer.SerializeWithLengthPrefix<messagetype_c>(netStream, mt, PrefixStyle.Base128);
                mt = Serializer.DeserializeWithLengthPrefix<messagetype_c>(netStream, PrefixStyle.Base128);

                Serializer.SerializeWithLengthPrefix<BeginSession>(netStream, beginSession, PrefixStyle.Base128);
                beginSession = Serializer.DeserializeWithLengthPrefix<BeginSession>(netStream, PrefixStyle.Base128);
                
                AddOk addOk;
                //Add
                int hh = 0;
                for (hh = 0; hh<5; hh++) {

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

                mt.msgtype = (Byte)CmdType.Add;

                Serializer.SerializeWithLengthPrefix<messagetype_c>(netStream, mt, PrefixStyle.Base128);
                mt = Serializer.DeserializeWithLengthPrefix<messagetype_c>(netStream, PrefixStyle.Base128);

                Serializer.SerializeWithLengthPrefix<Add>(netStream, add, PrefixStyle.Base128);
                addOk = Serializer.DeserializeWithLengthPrefix<AddOk>(netStream, PrefixStyle.Base128);

                }


                //EndSession
                mt.msgtype = (Byte)CmdType.EndSession;

                Serializer.SerializeWithLengthPrefix<messagetype_c>(netStream, mt, PrefixStyle.Base128);
                mt = Serializer.DeserializeWithLengthPrefix<messagetype_c>(netStream, PrefixStyle.Base128);

                EndSession endSession = new EndSession();
                endSession.sessionid = beginSession.sessionid;
                endSession.succesful = false;

                Serializer.SerializeWithLengthPrefix<EndSession>(netStream, endSession, PrefixStyle.Base128);
                endSession = Serializer.DeserializeWithLengthPrefix<EndSession>(netStream, PrefixStyle.Base128);

                Logging.WriteToLog(ListRequestAllWrapper(netStream).ToString());
                Logging.WriteToLog(ListRequestLastWrapper(netStream).ToString());

                /*
                var addOk = db.Add(ref add, currentUser.uid);

                var getResponse = db.GetResponse(addOk.fid, addOk.rev, currentUser.uid);
                Logging.WriteToLog(getResponse.ToString());

                proto_server.ListResponse listResponse = db.ListResponseLast(currentUser.uid);
                Logging.WriteToLog(listResponse.ToString());

                listResponse = db.ListResponseAll(currentUser.uid);
                Logging.WriteToLog(listResponse.ToString());
                */
            }
            catch (Exception e)
            {
                Logging.WriteToLog(e.ToString());
            }


            /*
            //READ DATATABLE
            Logging.WriteToLog("Trying Reading DT prom netStream ...");
            DataTable datat = new DataTable();

            using (IDataReader reader = DataSerializer.Deserialize(netStream))
            {
                datat.Load(reader);
            }
            Logging.WriteToLog("Trying Reading DT prom netStream ... DONE");
            Logging.WriteToLog("Dt received->\n" + DataTableToString(datat));
            */
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
    }
        
}
