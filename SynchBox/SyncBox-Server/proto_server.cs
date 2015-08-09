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

    class proto_server
    {
         private NetworkStream netStream= null;

        //ctor
        public proto_server(NetworkStream s) { netStream = s; }

        ///////////////--BEGIN--///////////////////////
        ///////////STRUCT DEFINITIONS /////////////////

        [ProtoContract]
        public class messagetype_c {

            [ProtoMember(1)]
            public byte msgtype;

            [ProtoMember(2)]
            public bool accepted;
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
        }

        /////////////////--END--///////////////////////
        ///////////STRUCT DEFINITIONS /////////////////
        
        public login_c manage_login(){
            messagetype_c msgtype = new messagetype_c
            {
                msgtype = (byte)CmdType.Login,
                accepted = false,
            };
            
            login_c login = new login_c
            {
                is_logged = false,
                uid = -1
            };

            MessageBox.Show("Attempting reading data!");
            messagetype_c msgtype_r = Serializer.DeserializeWithLengthPrefix<messagetype_c>(netStream, PrefixStyle.Base128);

            //CHECK credentials

            msgtype_r.accepted = true;

            MessageBox.Show("GOT CONNECTION Stream: sneding data...");
            Serializer.SerializeWithLengthPrefix(netStream, msgtype_r, PrefixStyle.Base128);

           
            MessageBox.Show("DEBUG HERE!");

            return login;
        }


    }
}
