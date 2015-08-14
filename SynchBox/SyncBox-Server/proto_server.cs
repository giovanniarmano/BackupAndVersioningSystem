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

//TODO Logout method-> unset login_session
namespace SyncBox_Server
{
    //TODO Synch with client!!
    enum CmdType : byte { Login, Register };

    //Tante istanze quanti sono i processi attivi sul server!
    class proto_server
    {
         private NetworkStream netStream= null;
         public login_c login_session;
         private db db_handle;
         private string dbConnection;
         private Logging log;

        //ctor
         public proto_server(NetworkStream s, string dbConnection,Logging log) { 
             netStream = s; 
             this.dbConnection = dbConnection;
             db_handle = new db(dbConnection);
             this.log = log;
         }

        ///////////////--BEGIN--///////////////////////
        ///////////STRUCT DEFINITIONS /////////////////

        [ProtoContract]
        public class messagetype_c
        {

            [ProtoMember(1)]
            public byte msgtype;

            [ProtoMember(2)]
            public bool accepted;

            public string ToString()
            {
                StringBuilder str = new StringBuilder("");
                str.Append("|msgtype->");
                str.Append(msgtype);
                str.Append("|accepted->");
                str.Append(accepted);
                str.Append("|");
                return str.ToString();
            }
        }


        [ProtoContract]
        public class login_c
        {

            [ProtoMember(1)]
            public bool is_logged;

            [ProtoMember(2)]
            public int uid;

            [ProtoMember(3)]
            public string username;

            [ProtoMember(4)]
            public string password;

            public string ToString()
            {
                StringBuilder str = new StringBuilder("");
                str.Append("|is_logged->");
                str.Append(is_logged);
                str.Append("|uid->");
                str.Append(uid);
                str.Append("|username->");
                str.Append(username);
                str.Append("|npassword->");
                str.Append(password);
                str.Append("|");
                return str.ToString();
            }
        }

        /////////////////--END--///////////////////////
        ///////////STRUCT DEFINITIONS /////////////////


        public void manage() {

            messagetype_c msgtype_r = Serializer.DeserializeWithLengthPrefix<messagetype_c>(netStream, PrefixStyle.Base128);
            
            switch (msgtype_r.msgtype){
                case (byte)CmdType.Login:
                    log.WriteToLog("manage LOGIN CASE ...");
                    msgtype_r.accepted = true;
                    Serializer.SerializeWithLengthPrefix(netStream, msgtype_r, PrefixStyle.Base128);
                    manage_login();
                break;

                case (byte)CmdType.Register:
                log.WriteToLog("manage REGISTER CASE ...");
                msgtype_r.accepted = true;
                Serializer.SerializeWithLengthPrefix(netStream, msgtype_r, PrefixStyle.Base128);
                manage_register();
                break;

                default:
                   log.WriteToLog("manage DEFAULT CASE ... (panic!!!)");
                break;
            }
        }


        public void manage_login(){
       
            login_c login_r = Serializer.DeserializeWithLengthPrefix<login_c>(netStream, PrefixStyle.Base128);

            log.WriteToLog("received - " + login_r.ToString());

            //CHECK credentials

            string sql = "select * from USERS where user = '" + login_r.username + "' ;";

            string md5pwd = CalculateMD5Hash(login_r.password);

            DataTable dt = db_handle.GetDataTable(sql);

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
            login_session = login_r;

            Serializer.SerializeWithLengthPrefix(netStream, login_r, PrefixStyle.Base128);

            log.WriteToLog("sent + " + login_r.ToString());

        }

        public void manage_register()
        {
            login_c login_r = Serializer.DeserializeWithLengthPrefix<login_c>(netStream, PrefixStyle.Base128);

            log.WriteToLog("received - " + login_r.ToString());

            string sql = "select * from USERS where user = '" + login_r.username + "' ;";

            DataTable dt = db_handle.GetDataTable(sql);

            if (dt.Rows.Count == 0)
            {
                string md5pwd = CalculateMD5Hash(login_r.password);
                string sqlupdate = "insert into USERS (user,md5) values ('" + login_r.username + "','" + md5pwd + "');";

                int row_updated = db_handle.ExecuteNonQuery(sqlupdate);

                string sql_ = "select * from USERS where user = '" + login_r.username + "' ;";

                DataTable dt_ = db_handle.GetDataTable(sql_);

                login_r.uid = int.Parse(dt_.Rows[0]["uid"].ToString());
                login_r.is_logged = true;

            }
            else if (dt.Rows.Count >= 1)
            {
                login_r.is_logged = false;
            }
            
            login_r.password = null;

            //salvo nella sessione utente login! 
            login_session = login_r;

            //   MessageBox.Show("GOT CONNECTION Stream: sneding data...");
            Serializer.SerializeWithLengthPrefix(netStream, login_r, PrefixStyle.Base128);

            log.WriteToLog("sent - " + login_r.ToString());

        }

        public string CalculateMD5Hash(string input)
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

    }
}
