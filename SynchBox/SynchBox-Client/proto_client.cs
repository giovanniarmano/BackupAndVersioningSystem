using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Windows;
using ProtoBuf;

namespace SynchBox_Client
{
    enum CmdType : byte { Login , Register };

    class proto_client
    {
        private NetworkStream netStream= null;

        //ctor
        public proto_client(NetworkStream s) { netStream = s; }

        ///////////////--BEGIN--///////////////////////
        ///////////STRUCT DEFINITIONS /////////////////

        [ProtoContract]
        public class messagetype_c {

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
        public class login_c {
            
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
        
        public login_c do_login(string _username, string _password){
            messagetype_c msgtype = new messagetype_c
            {
                msgtype = (byte)CmdType.Login,
                accepted = false,
            };

            MessageBox.Show("GOT CONNECTION Stream: sending data...");
            Serializer.SerializeWithLengthPrefix(netStream, msgtype, PrefixStyle.Base128);

            MessageBox.Show("Attempting reading data!");
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


            MessageBox.Show("SENT LOGIN\n" + login.ToString() + "\n\nRCVD LOGIN\n" + login_r.ToString());

            return login_r;
        }

        //public void my_sender(enum CmdType, )

    }
}
