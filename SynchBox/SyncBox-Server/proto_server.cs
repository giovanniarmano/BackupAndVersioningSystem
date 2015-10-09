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
using ProtoBuf.Data;

//TODO save login info in all register login logout methods


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
                        manage_test(netStream);
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

        private static void manage_Add(NetworkStream netStream, ref login_c currentUser)
        {
            //throw new NotImplementedException();
            Add add = Serializer.DeserializeWithLengthPrefix<Add>(netStream, PrefixStyle.Base128);
             db.Add(ref add, currentUser.uid);
        }

        private static void manage_Delete(NetworkStream netStream, ref login_c currentUser)
        {
            throw new NotImplementedException();
        }

        private static void manage_Update(NetworkStream netStream, ref login_c currentUser)
        {
            throw new NotImplementedException();
        }

        private static void manage_GetList(NetworkStream netStream, ref login_c currentUser)
        {
            //throw new NotImplementedException();
            GetList getList = Serializer.DeserializeWithLengthPrefix<GetList>(netStream, PrefixStyle.Base128);
            Logging.WriteToLog("GET LIST request ...\n"+getList.ToString());
            int i = 0;
            for (i = 0; i < getList.fileList.Count; i++)
            {
                GetResponse getResponse = db.GetResponse(getList.fileList[i].fid, getList.fileList[i].rev,currentUser.uid);
                if (getResponse == null)
                {
                    Logging.WriteToLog("No file found ... PAnic //TODO manage");
                }
                else
                {
                    Serializer.SerializeWithLengthPrefix(netStream, getResponse, PrefixStyle.Base128);
                }
                
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
                    ListResponse listResponse = db.ListResponseLast(currentUser.uid);
                    Serializer.SerializeWithLengthPrefix(netStream, listResponse, PrefixStyle.Base128);
                    break;

                case (byte)ListRequestType.All:
                    Logging.WriteToLog("LIST REQUEST (All) ...");
                    throw new NotImplementedException();
                   // ListResponse listResponse;// = db.ListResponseLast();
                    Serializer.SerializeWithLengthPrefix(netStream, listResponse, PrefixStyle.Base128);
                    break;

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

                default:
                    Logging.WriteToLog("LIST REQUEST Default case ... (panic!!!)");
                    break;
            }
        }


        public static void manage_test(NetworkStream netStream)
        {
            test_c tc = Serializer.DeserializeWithLengthPrefix<test_c>(netStream, PrefixStyle.Base128);

            Logging.WriteToLog("-------TEST LIST RECEIVED--------\n" + tc.intlist.ToString());

            int i;

            for (i = 0; i < tc.intlist.Count; i++)
            {
                Logging.WriteToLog(tc.intlist[i].ToString());
            }

           // var aus = db.ListResponseLast();

            //Il client mi manda una lista, vedo riesco a vederla!! Faigo!
            /*
            Logging.WriteToLog("Ora il SERVER interroga il server chiedendogli una DataTable");

            string sql = @"  select TEST_META.id, filename,path,md5,data_raw
                              from TEST_META, TEST_DATA
                              where TEST_META.id = TEST_DATA.id; ";

            DataTable dt = db.GetDataTable(sql);

            Logging.WriteToLog("dt.tostring->" + DataTableToString(dt));

            dt.Rows[0][0] = 1000;
            dt.Columns.Add("newColumn");
            dt.Rows[0]["newColumn"] = "firts";

            Logging.WriteToLog("dt.tostring->" + DataTableToString(dt));

            Logging.WriteToLog("Trying to Serialize dt.DataReader ...");

            //TRYING TO SERIALIZE A DATATABLE
            Stream buffer = new MemoryStream();
            var reader = dt.CreateDataReader();
            DataSerializer.Serialize(buffer, dt);

            Logging.WriteToLog("Trying to Serialize dt.DataReader DONE");

            Logging.WriteToLog("Trying to DESerialize dt.DataReader ...");
            
            //DataTable dt2 = new DataTable();
            buffer.Seek(0, SeekOrigin.Begin);
            DataTable dt2 = DataSerializer.DeserializeDataTable(buffer);
            //{
            //    dt2.Load(reader2);
            //}
            Logging.WriteToLog("Trying to DESerialize dt.DataReader DONE");

            Logging.WriteToLog("dt2.tostring->" + DataTableToString(dt2));
            */
            /*
            //SEND DATATABLE
            //netStream;
            Logging.WriteToLog("SERVER is SENDING DATATABLE...");
            using (IDataReader reader_ = dt2.CreateDataReader())
            {
                DataSerializer.Serialize(netStream, reader_);
            }
            Logging.WriteToLog("SERVER is SENDING DATATABLE... SENT !!!!");
            Logging.WriteToLog("dt->\n" + DataTableToString(dt2));
            */
         
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
                string sqlupdate = "insert into USERS (user,md5) values ('" + login_r.username + "','" + md5pwd + "');";

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
