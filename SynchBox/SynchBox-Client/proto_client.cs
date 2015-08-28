﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Windows;
using ProtoBuf;
using System.Threading;

namespace SynchBox_Client
{
    enum CmdType : byte { Login, Register, Logout };

    public static class proto_client
    {
        //private NetworkStream netStream= null;
        //Logging log;

        //ctor
        //public proto_client(NetworkStream s) { netStream = s; }

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
                str.Append("|msgtype->");
                str.Append(msgtype);
                str.Append("|accepted->");
                str.Append(accepted);
                str.Append("|");
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

        //compare filesystems
        {   
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

    }
}
