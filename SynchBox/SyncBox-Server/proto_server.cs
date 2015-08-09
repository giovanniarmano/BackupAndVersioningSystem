using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;
using System.Net;
using System.Net.Sockets;
using System.Windows;

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

        //ctor
        public proto_server(NetworkStream s) { netStream = s; }

        public void setDb(db handle) { db_handle = handle; }

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
                str.Append("msgtype->");
                str.Append(msgtype);
                str.Append("\naccepted->");
                str.Append(accepted);
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
                str.Append("is_logged->");
                str.Append(is_logged);
                str.Append("\nuid->");
                str.Append(uid);
                str.Append("\nusername->");
                str.Append(username);
                str.Append("\npassword->");
                str.Append(password);
                return str.ToString();
            }
        }

        /////////////////--END--///////////////////////
        ///////////STRUCT DEFINITIONS /////////////////


        public void manage() {

            messagetype_c msgtype_r = Serializer.DeserializeWithLengthPrefix<messagetype_c>(netStream, PrefixStyle.Base128);


            switch (msgtype_r.msgtype){
                case (byte)CmdType.Login:
                    MessageBox.Show("LOGIN CASE");
                    msgtype_r.accepted = true;
                    Serializer.SerializeWithLengthPrefix(netStream, msgtype_r, PrefixStyle.Base128);
                    manage_login();
                break;
               
                default:
                   MessageBox.Show("DEFAULT");
                break;
            }
        }


        public void manage_login(){
       
            //MessageBox.Show("Attempting reading data!");
            login_c login_r = Serializer.DeserializeWithLengthPrefix<login_c>(netStream, PrefixStyle.Base128);

            MessageBox.Show("RCV LOGIN->" + login_r.ToString());

            //CHECK credentials
            //set id
            //rmv pwd

            login_r.is_logged = true;
            login_r.password = "";
            login_r.uid = 1;

            //salvo nella sessione utente login! 
            login_session = login_r;

       //   MessageBox.Show("GOT CONNECTION Stream: sneding data...");
            Serializer.SerializeWithLengthPrefix(netStream, login_r, PrefixStyle.Base128);

            MessageBox.Show("SENT LOGIN->" + login_r.ToString());

        }

    }
}
