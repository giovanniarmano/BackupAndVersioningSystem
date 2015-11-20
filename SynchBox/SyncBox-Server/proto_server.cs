using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;
using System.Net;
using System.Net.Sockets;
using System.Windows;
using System.Data;
using System.Security.Cryptography;
using System.Threading;
using System.IO;
//using ProtoBuf.Data;

//TODO save login info in all register login logout methods
//TODO add a xontrol con il cancellation token
//TODO prova a portare il multithreading al livello superiore, salvando il current user nel ciclo che gestisce i task
//-> e testa le prestazioni a confronto!

//TODO Logout method-> unset login_session
namespace SyncBox_Server
{
    //Tante istanze quanti sono i processi attivi sul server!
    public static partial class proto_server
    {
       
        public static void manage(NetworkStream netStream,CancellationToken ct,ref bool exc)
        {
            //while da testare! Uso questo per mantenere come local var il login dell'utente che sto gestendo
            login_c currentUser = new login_c();
            currentUser.is_logged = false;

            while (!exc && !ct.IsCancellationRequested) { 
            try
            {   //magari si può mettere nella chiamata sopra!
                //NetworkStream netStream = new NetworkStream(s, false); ///false = not own the socket
                //Logging.WriteToLog("sleeping ...");
                //System.Threading.Thread.Sleep(5000);
                //Logging.WriteToLog("sleeping DONE");
                   
                messagetype_c msgtype_r = Serializer.DeserializeWithLengthPrefix<messagetype_c>(netStream, PrefixStyle.Base128);

                //DEBUG DEBUG DEBUG DEBUG DEBUG DEBUG DEBUG DEBUG DEBUG DEBUG DEBUG DEBUG
                //System.Threading.Thread.Sleep(500);
                
                switch (msgtype_r.msgtype)
                {
                    case (byte)CmdType.Login:
                        Logging.WriteToLog("manage LOGIN CASE ...");
                        msgtype_r.accepted = true;
                        Serializer.SerializeWithLengthPrefix(netStream, msgtype_r, PrefixStyle.Base128);
                        manage_login(netStream,ref currentUser);
                        break;

                    case (byte)CmdType.Register:
                        Logging.WriteToLog("manage REGISTER CASE ...");
                        msgtype_r.accepted = true;
                        Serializer.SerializeWithLengthPrefix(netStream, msgtype_r, PrefixStyle.Base128);
                        manage_register(netStream, ref currentUser);
                        break;

                    case (byte)CmdType.Logout:
                        Logging.WriteToLog("manage LOGOUT CASE ...");
                        msgtype_r.accepted = true;
                            //TODO ADAPT
                        //Serializer.SerializeWithLengthPrefix(netStream, msgtype_r, PrefixStyle.Base128);
                        manage_logout(netStream,ref currentUser);
                        break;

                    case (byte)CmdType.Test:
                        Logging.WriteToLog("manage TEST CASE ...");
                        msgtype_r.accepted = true;
                        //Serializer.SerializeWithLengthPrefix(netStream, msgtype_r, PrefixStyle.Base128);
                        manage_test(netStream, ref currentUser);
                        break;

                    case (byte)CmdType.ListRequest:
                        Logging.WriteToLog("manage ListRequest CASE ...");
                        msgtype_r.accepted = true;
                        Serializer.SerializeWithLengthPrefix(netStream, msgtype_r, PrefixStyle.Base128);
                        manage_ListRequest(netStream, ref currentUser);
                        break;

                    case (byte)CmdType.GetList:
                        Logging.WriteToLog("manage GetList CASE ...");
                        msgtype_r.accepted = true;
                        Serializer.SerializeWithLengthPrefix(netStream, msgtype_r, PrefixStyle.Base128);
                        manage_GetList(netStream, ref currentUser);
                        break;

                    case (byte)CmdType.Update:
                        Logging.WriteToLog("manage Update CASE ...");
                        msgtype_r.accepted = true;
                        Serializer.SerializeWithLengthPrefix(netStream, msgtype_r, PrefixStyle.Base128);
                        manage_Update(netStream, ref currentUser);
                        break;

                    case (byte)CmdType.Delete:
                        Logging.WriteToLog("manage Delete CASE ...");
                        msgtype_r.accepted = true;
                        Serializer.SerializeWithLengthPrefix(netStream, msgtype_r, PrefixStyle.Base128);
                        manage_Delete(netStream, ref currentUser);
                        break;

                    case (byte)CmdType.Add:
                        Logging.WriteToLog("manage Add CASE ...");
                        msgtype_r.accepted = true;
                        Serializer.SerializeWithLengthPrefix(netStream, msgtype_r, PrefixStyle.Base128);
                        manage_Add(netStream, ref currentUser);
                        break;

                    case (byte)CmdType.BeginSession:
                        Logging.WriteToLog("manage BeginSession CASE ...");
                        msgtype_r.accepted = true;
                        Serializer.SerializeWithLengthPrefix(netStream, msgtype_r, PrefixStyle.Base128);
                        manage_BeginSession(netStream, ref currentUser);
                        break;

                    case (byte)CmdType.EndSession:
                        Logging.WriteToLog("manage EndSession CASE ...");
                        msgtype_r.accepted = true;
                        Serializer.SerializeWithLengthPrefix(netStream, msgtype_r, PrefixStyle.Base128);
                        manage_EndSession(netStream, ref currentUser);
                        break;

                    case (byte)CmdType.GetSynchId:
                        Logging.WriteToLog("manage GetSynchId CASE ...");
                        msgtype_r.accepted = true;
                        Serializer.SerializeWithLengthPrefix(netStream, msgtype_r, PrefixStyle.Base128);
                        manage_GetSynchId(netStream, ref currentUser);
                        break;

                    case (byte)CmdType.Lock:
                        Logging.WriteToLog("manage Lock CASE ...");
                        msgtype_r.accepted = true;
                        Serializer.SerializeWithLengthPrefix(netStream, msgtype_r, PrefixStyle.Base128);
                        manage_Lock(netStream, ref currentUser);
                        break;

                        default:
                        Logging.WriteToLog("manage DEFAULT CASE ... (panic!!!)");
                        break;
                }
            }
            catch (Exception ex) {
                exc = true;
                Logging.WriteToLog(ex.ToString());
            }
            }
        }

        private static void manage_Lock(NetworkStream netStream, ref login_c currentUser)
        {
            lock_c lock_c = Serializer.DeserializeWithLengthPrefix<lock_c>(netStream, PrefixStyle.Base128);
            lock_c lock_c_response = db.Lock(ref lock_c, ref currentUser);
            Serializer.SerializeWithLengthPrefix<lock_c>(netStream, lock_c_response, PrefixStyle.Base128);
        }

        private static void manage_GetSynchId(NetworkStream netStream, ref login_c currentUser)
        {
            GetSynchid getSynchId = Serializer.DeserializeWithLengthPrefix<GetSynchid>(netStream, PrefixStyle.Base128);
            getSynchId.synchid = db.GetSynchId(currentUser.uid);
            Serializer.SerializeWithLengthPrefix<GetSynchid>(netStream, getSynchId, PrefixStyle.Base128);
            //throw new NotImplementedException();
        }

        private static void manage_EndSession(NetworkStream netStream, ref login_c currentUser)
        {
            EndSession endSession = Serializer.DeserializeWithLengthPrefix<EndSession>(netStream, PrefixStyle.Base128);
            if(currentUser.synchsessionid == endSession.sessionid) { 
                currentUser.synchsessionid = -1;
                endSession.succesful = true;
            }
            Serializer.SerializeWithLengthPrefix<EndSession>(netStream, endSession, PrefixStyle.Base128);

        }

        private static void manage_BeginSession(NetworkStream netStream, ref login_c currentUser)
        {
            BeginSession beginSession = Serializer.DeserializeWithLengthPrefix<BeginSession>(netStream, PrefixStyle.Base128);
            currentUser.synchsessionid = db.BeginSession(currentUser.uid);
            beginSession.sessionid = currentUser.synchsessionid;
            Serializer.SerializeWithLengthPrefix<BeginSession>(netStream, beginSession, PrefixStyle.Base128);

        }

        private static void manage_Add(NetworkStream netStream, ref login_c currentUser)
        {
            //throw new NotImplementedException();
            Add add = Serializer.DeserializeWithLengthPrefix<Add>(netStream, PrefixStyle.Base128);
            if (currentUser.synchsessionid == -1) throw new Exception("Add out of SynchSession");
            AddOk addOk = db.Add(ref add, ref currentUser);
            Serializer.SerializeWithLengthPrefix<AddOk>(netStream, addOk, PrefixStyle.Base128);
        }

        private static void manage_Delete(NetworkStream netStream, ref login_c currentUser)
        {
            Delete delete = Serializer.DeserializeWithLengthPrefix<Delete>(netStream, PrefixStyle.Base128);
            if (currentUser.synchsessionid == -1) throw new Exception("Delete out of SynchSession");
            DeleteOk deleteOk = db.Delete(ref delete, ref currentUser);
            Serializer.SerializeWithLengthPrefix<DeleteOk>(netStream, deleteOk, PrefixStyle.Base128);
        }

        private static void manage_Update(NetworkStream netStream, ref login_c currentUser)
        {
            Update update = Serializer.DeserializeWithLengthPrefix<Update>(netStream, PrefixStyle.Base128);
            if (currentUser.synchsessionid == -1) throw new Exception("Delete out of SynchSession");
            UpdateOk updateOk = db.Update(ref update, ref currentUser);
            Serializer.SerializeWithLengthPrefix<UpdateOk>(netStream, updateOk, PrefixStyle.Base128);
            //throw new NotImplementedException();
        }

        private static void manage_GetList(NetworkStream netStream, ref login_c currentUser)
        {
            GetList getList = Serializer.DeserializeWithLengthPrefix<GetList>(netStream, PrefixStyle.Base128);
            Logging.WriteToLog("GET LIST request ...\n"+getList.ToString());
            int i = 0;
            for (i = 0; i < getList.fileList.Count; i++)
            {
                GetResponse getResponse = db.GetResponse(getList.fileList[i].fid, getList.fileList[i].rev,currentUser.uid);
                Serializer.SerializeWithLengthPrefix(netStream, getResponse, PrefixStyle.Base128);
                Logging.WriteToLog("Managed "+ (i + 1).ToString() + " of "+ getList.fileList.Count + " \n" +getResponse.ToString());
            }
        }

        private static void manage_ListRequest(NetworkStream netStream, ref login_c currentUser)
        {
            //throw new NotImplementedException();
            ListRequest listRequest = Serializer.DeserializeWithLengthPrefix<ListRequest>(netStream, PrefixStyle.Base128);
            //TODO Switch case
            switch (listRequest.listReqType)
            {
                case (byte)ListRequestType.Last:
                    Logging.WriteToLog("LIST REQUEST (Last) ...");
                    ListResponse listResponseLast = db.ListResponseLast(currentUser.uid);
                    Serializer.SerializeWithLengthPrefix(netStream, listResponseLast, PrefixStyle.Base128);
                    break;

                case (byte)ListRequestType.All:
                    Logging.WriteToLog("LIST REQUEST (All) ...");
                    //throw new NotImplementedException();
                    ListResponse listResponseAll = db.ListResponseAll(currentUser.uid);
                    Serializer.SerializeWithLengthPrefix(netStream, listResponseAll, PrefixStyle.Base128);
                    break;
/*
//IO NON IMPLEMENTEREI QUESTE COSE BRUTTE xS Ma qualcosa è da fare

                case (byte)ListRequestType.DateInterval:
                    Logging.WriteToLog("LIST REQUEST (DateInterval) ...");
                    throw new NotImplementedException();
                    //ListResponse listResponse = db.ListResponseLast();
                    Serializer.SerializeWithLengthPrefix(netStream, listResponse, PrefixStyle.Base128);
                    break;

                case (byte)ListRequestType.Filename:
                    Logging.WriteToLog("LIST REQUEST (filename) ...");
                    throw new NotImplementedException();
                   // ListResponse listResponse = db.ListResponseLast();
                    Serializer.SerializeWithLengthPrefix(netStream, listResponse, PrefixStyle.Base128);
                    break;
*/
                default:
                    Logging.WriteToLog("LIST REQUEST Default case ... (panic!!!)");
                    break;
            }
        }


        public static void manage_test(NetworkStream netStream, ref login_c currentUser)
        {
            test_c tc = Serializer.DeserializeWithLengthPrefix<test_c>(netStream, PrefixStyle.Base128);

            Logging.WriteToLog("-------TEST LIST RECEIVED--------\n" + tc.intlist.ToString());

            int i;

            for (i = 0; i < tc.intlist.Count; i++)
            {
                Logging.WriteToLog(tc.intlist[i].ToString());
            }

            try
            {   
                string filename = RandomString(5) + ".txt";
                string folder = "\\temp\\" + filename;
                string filePath = "C:\\backup\\temp\\" + filename;
                var fileStream = File.Create(filePath);
                fileStream.Close();
                Logging.WriteToLog("Writing on file " + filePath);
                string text = RandomString(20);
                Logging.WriteToLog("Text__>" + text);
                File.WriteAllText(filePath,text);


                Add add = new Add();
                add.filename = filename;
                add.folder = folder;
                add.fileDump = File.ReadAllBytes(filePath);

                var addOk = db.Add(ref add, ref currentUser);

                var getResponse = db.GetResponse(addOk.fid, addOk.rev, currentUser.uid);
                Logging.WriteToLog(getResponse.ToString());

                proto_server.ListResponse listResponse = db.ListResponseLast(currentUser.uid);
                Logging.WriteToLog(listResponse.ToString());

                listResponse = db.ListResponseAll(currentUser.uid);
                Logging.WriteToLog(listResponse.ToString());

            }
            catch (Exception e)
            {
                Logging.WriteToLog(e.ToString());
            }

            // Provo ad aprire un file di test random

            //genero random text

            //aggiungo in db
        }

        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private static void manage_logout(NetworkStream netStream, ref login_c currentUser)
        {
            netStream.Close();
            currentUser.uid = -1;
            currentUser.username = "";
            currentUser.is_logged = false;
            throw new Exception("Logout msg received!");
        }


        public static void manage_login(NetworkStream netStream, ref login_c currentUser){
       
            login_c login_r = Serializer.DeserializeWithLengthPrefix<login_c>(netStream, PrefixStyle.Base128);

            Logging.WriteToLog("received - " + login_r.ToString());

            //CHECK credentials

            string sql = "select * from USERS where user = '" + login_r.username + "' ;";

            string md5pwd = CalculateMD5Hash(login_r.password);

            DataTable dt = db.GetDataTable(sql);

            bool success = false;

            if (dt.Rows.Count == 0) {
                success = false;
            }
            else if (dt.Rows.Count == 1) {
                if (dt.Rows[0]["user"].ToString().Equals(login_r.username) && dt.Rows[0]["md5"].ToString().Equals(md5pwd))
                    success = true;
                else
                    success = false;
            }
            else {
                success = false;
            }

            if (success)
            {
                login_r.uid = int.Parse(dt.Rows[0]["uid"].ToString());
                login_r.is_logged = true;
            }
            else {
                login_r.is_logged = false;
            }
            login_r.password = null;

            //salvo nella sessione utente login!
            currentUser = login_r; 
            //login_session = login_r;

            Serializer.SerializeWithLengthPrefix(netStream, login_r, PrefixStyle.Base128);

            Logging.WriteToLog("sent + " + login_r.ToString());

        }

        public static void manage_register(NetworkStream netStream, ref login_c currentUser)
        {
            login_c login_r = Serializer.DeserializeWithLengthPrefix<login_c>(netStream, PrefixStyle.Base128);

            Logging.WriteToLog("received - " + login_r.ToString());

            string sql = "select * from USERS where user = '" + login_r.username + "' ;";

            DataTable dt = db.GetDataTable(sql);

            if (dt.Rows.Count == 0)
            {
                string md5pwd = CalculateMD5Hash(login_r.password);
                string sqlupdate = "insert into USERS (user,md5,lock) values ('" + login_r.username + "','" + md5pwd + "','0');";

                int row_updated = db.ExecuteNonQuery(sqlupdate);

                string sql_ = "select * from USERS where user = '" + login_r.username + "' ;";

                DataTable dt_ = db.GetDataTable(sql_);

                login_r.uid = int.Parse(dt_.Rows[0]["uid"].ToString());
                login_r.is_logged = true;

            }
            else if (dt.Rows.Count >= 1)
            {
                login_r.is_logged = false;
            }
            
            login_r.password = null;

            //salvo nella sessione utente login!
            currentUser = login_r; 
            //login_session = login_r;

            //   MessageBox.Show("GOT CONNECTION Stream: sneding data...");
            Serializer.SerializeWithLengthPrefix(netStream, login_r, PrefixStyle.Base128);

            Logging.WriteToLog("sent - " + login_r.ToString());

        }


       




        public static string CalculateMD5Hash(string input)
        {
            // step 1, calculate MD5 hash from input
            MD5 md5 = System.Security.Cryptography.MD5.Create();
            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
            byte[] hash = md5.ComputeHash(inputBytes);

            // step 2, convert byte array to hex string
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("X2"));
            }
            return sb.ToString();
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
