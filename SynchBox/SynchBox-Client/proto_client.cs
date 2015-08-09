using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Windows;

namespace SynchBox_Client
{
    class proto_client
    {
        private Socket socket = null;

        //ctor
        public proto_client(Socket s){ socket = s; }

        ///////////////--BEGIN--///////////////////////
        ///////////STRUCT DEFINITIONS /////////////////

        

        public struct login_s {
            bool is_logged;
            int uid;
            string username;
            string password;
        }

        /////////////////--END--///////////////////////
        ///////////STRUCT DEFINITIONS /////////////////
        
        public int do_login(string username, string password){
            

        }

    }
}
