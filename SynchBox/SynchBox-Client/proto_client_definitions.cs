using System;
using System.Collections.Generic;
using System.Text;
using ProtoBuf;

namespace SynchBox_Client
{
    public static partial class proto_client
    {
        ///////////////--BEGIN--///////////////////////
        ///////////STRUCT DEFINITIONS /////////////////

        //TODO Synch with client!!
        public enum CmdType : byte { Login, Register, Logout, Test, ListRequest, GetList, Update, Delete, Add, BeginSession, EndSession, GetSynchId, Lock, LockAcquire, LockRelease };

        [ProtoContract]
        public class lock_c
        {
            [ProtoMember(1)]
            public byte lockType; //LockAcquire or Release


            [ProtoMember(2)]
            public bool succesfull;

            public string ToString()
            {
                StringBuilder str = new StringBuilder("lock_c");
                str.Append("|lockType->");
                str.Append(lockType);
                str.Append("|");
                return str.ToString();
            }

        }

        [ProtoContract]
        public class messagetype_c
        {
            [ProtoMember(1)]
            public byte msgtype;

            [ProtoMember(2)]
            public bool accepted;

            public string ToString()
            {
                StringBuilder str = new StringBuilder("messagetype_c");
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

            public int synchsessionid;

            public string ToString()
            {
                StringBuilder str = new StringBuilder("login_c");
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

        [ProtoContract]
        public class myObj
        {
            [ProtoMember(1)]
            public int int1;

            [ProtoMember(2)]
            public int int2;

            [ProtoMember(3)]
            public string s1;

            [ProtoMember(4)]
            public string s2;

            public string ToString()
            {
                StringBuilder str = new StringBuilder("myObj");
                str.Append("|int1->");
                str.Append(int1);
                str.Append("|int2->");
                str.Append(int2);
                str.Append("|s1->");
                str.Append(s2);
                str.Append("|s2->");
                str.Append(s2);
                str.Append("|");
                return str.ToString();
            }
        }


        [ProtoContract]
        public class test_c
        {
            [ProtoMember(1)]
            public List<myObj> intlist;
        }


        [ProtoContract]
        public class FileListItem
        {
            [ProtoMember(1)]
            public int fid;

            [ProtoMember(2)]
            public int rev;

            [ProtoMember(3)]
            public string filename;

            [ProtoMember(4)]
            public string folder;

            [ProtoMember(5)]
            public DateTime timestamp;

            [ProtoMember(6)]
            public string md5;

            [ProtoMember(7)]
            public Boolean deleted;

            //is a dir?
            [ProtoMember(8)]
            public Boolean dir;

            //pointer to fid of father folder.
            [ProtoMember(9)]
            public int folder_id;

            public string ToString()
            {
                StringBuilder str = new StringBuilder("FileListItem");
                str.Append("|fid->");
                str.Append(fid);
                str.Append("|rev->");
                str.Append(rev);
                str.Append("|filename->");
                str.Append(filename);
                str.Append("|folder->");
                str.Append(folder);
                str.Append("|timestamp->");
                str.Append(timestamp.ToString());
                str.Append("|md5->");
                str.Append(md5);
                str.Append("|folder->");
                str.Append(folder);
                str.Append("|dir(is a ?)->");
                str.Append(dir.ToString());
                str.Append("|folder_id->");
                str.Append(folder_id);
                str.Append("|");
                return str.ToString();
            }
        }

        enum ListRequestType : byte { Last, All, DateInterval, Filename };

        //LIST_REQUEST Richiede una versione al server di tipo enum listReqType
        [ProtoContract]
        public class ListRequest
        {
            [ProtoMember(1)]
            public byte listReqType;
            public string ToString()
            {
                StringBuilder str = new StringBuilder("ListRequest");
                str.Append("|listReqType->");
                str.Append(listReqType);
                str.Append("|");
                return str.ToString();
            }

        }

        [ProtoContract]
        public class ListResponse
        {
            [ProtoMember(1)]
            public List<FileListItem> fileList;

            public string ToString()
            {
                StringBuilder str = new StringBuilder("ListResponse\n");
                foreach (FileListItem fli in fileList)
                    str.Append(fli.ToString() + "\n");
                return str.ToString();
            }
        }


        [ProtoContract]
        public class FileToGet
        {
            [ProtoMember(1)]
            public int fid;

            [ProtoMember(2)]
            public int rev;

            [ProtoMember(3)]
            public string filename;

            [ProtoMember(4)]
            public string folder;

            [ProtoMember(5)]
            public DateTime timestamp;

            [ProtoMember(6)]
            public string md5;

            [ProtoMember(7)]
            public Boolean deleted;

            //is a dir?
            [ProtoMember(8)]
            public Boolean dir;

            //pointer to fid of father folder.
            [ProtoMember(9)]
            public int folder_id;

            public string ToString()
            {
                StringBuilder str = new StringBuilder("FileToGet");
                str.Append("|fid->");
                str.Append(fid);
                str.Append("|rev->");
                str.Append(rev);
                str.Append("|filename->");
                str.Append(filename);
                str.Append("|folder->");
                str.Append(folder);
                str.Append("|timestamp->");
                str.Append(timestamp.ToString());
                str.Append("|md5->");
                str.Append(md5);
                str.Append("|folder->");
                str.Append(folder);
                str.Append("|dir(is a ?)->");
                str.Append(dir.ToString());
                str.Append("|folder_id->");
                str.Append(folder_id);
                str.Append("|");
                return str.ToString();
            }
        }

        //GET_LIST richiede al server una lista di n file(DUMP FILE) 
        [ProtoContract]
        public class GetList
        {
            [ProtoMember(1)]
            public int n;

            [ProtoMember(2)]
            public List<FileToGet> fileList;

            public string ToString()
            {
                StringBuilder str = new StringBuilder("GetList n = " + n + "\n");
                foreach (FileToGet ftg in fileList)
                    str.Append(ftg.ToString() + "\n");
                return str.ToString();
            }

        }

        [ProtoContract]
        public class GetResponse
        {
            [ProtoMember(1)]
            public FileToGet fileInfo;

            //[ProtoMember(2)]
            //public int syncid;

            [ProtoMember(2)]
            public Byte[] fileDump;

            public string ToString()
            {
                StringBuilder str = new StringBuilder("GetResponse");
                str.Append("|fileInfo->");
                str.Append(fileInfo.ToString());
                //str.Append("|syncid->");
                //str.Append(syncid);
                ////NO PRINT BYTEARRAY BLOB
                str.Append("|");
                return str.ToString();
            }
        }

        [ProtoContract]
        public class BeginSession
        {
            [ProtoMember(1)]
            public int sessionid;

            public string ToString()
            {
                StringBuilder str = new StringBuilder("BeginSession");
                str.Append("|sessionid->");
                str.Append(sessionid);
                str.Append("|");
                return str.ToString();
            }
        }

        [ProtoContract]
        public class EndSession
        {
            [ProtoMember(1)]
            public int sessionid;

            [ProtoMember(2)]
            public bool succesful;

            public string ToString()
            {
                StringBuilder str = new StringBuilder("EndSession");
                str.Append("|sessionid->");
                str.Append(sessionid);
                str.Append("|successful->");
                str.Append(succesful);
                str.Append("|");
                return str.ToString();
            }
        }

        [ProtoContract]
        public class GetSynchid
        {
            [ProtoMember(1)]
            public int synchid;

            public string ToString()
            {
                StringBuilder str = new StringBuilder("GetSynchid");
                str.Append("|synchid->");
                str.Append(synchid);
                str.Append("|");
                return str.ToString();
            }
        }

        //DELETE_FILE ON SERVER

        [ProtoContract]
        public class Delete
        {
            [ProtoMember(1)]
            public int fid;

            public string ToString()
            {
                StringBuilder str = new StringBuilder("Delete");
                str.Append("|fid->");
                str.Append(fid);
                str.Append("|");
                return str.ToString();
            }
        }

        [ProtoContract]
        public class DeleteOk
        {
            [ProtoMember(1)]
            public int fid;

            //[ProtoMember(2)]
            //public int syncid;

            public string ToString()
            {
                StringBuilder str = new StringBuilder("DeleteOk");
                str.Append("|fid->");
                str.Append(fid);
                //str.Append("|syncid->");
                //str.Append(syncid);
                str.Append("|");
                return str.ToString();
            }
        }

        //UPDATE_FILE ON SERVER

        [ProtoContract]
        public class Update
        {
            [ProtoMember(1)]
            public int fid;

            [ProtoMember(2)]
            public byte[] fileDump;

            public string ToString()
            {
                StringBuilder str = new StringBuilder("Update");
                str.Append("|fid->");
                str.Append(fid);
                str.Append("|filedump->XXXXX");
                str.Append("|");
                return str.ToString();
            }
        }

        [ProtoContract]
        public class UpdateOk
        {
            [ProtoMember(1)]
            public int fid;

            [ProtoMember(2)]
            public int rev;

            //[ProtoMember(3)]
            //public int syncid;

            public string ToString()
            {
                StringBuilder str = new StringBuilder("UpdateOk");
                str.Append("|fid->");
                str.Append(fid);
                str.Append("|rev");
                str.Append(rev);
                //str.Append("|syncid");
                //str.Append(syncid);
                str.Append("|");
                return str.ToString();
            }
        }


        //ADD A FILE NOT PRESENT IN LOCAL 
        [ProtoContract]
        public class Add
        {
            [ProtoMember(1)]
            public string filename;

            [ProtoMember(2)]
            public string folder;

            [ProtoMember(3)]
            public bool dir = false;

            [ProtoMember(4)]
            public byte[] fileDump;

            public string ToString()
            {
                StringBuilder str = new StringBuilder("Add");
                str.Append("|filename->");
                str.Append(filename);
                str.Append("|folder");
                str.Append(folder);
                str.Append("|dir(is a dir?)");
                str.Append(dir.ToString());
                str.Append("|filedump->XXX");
                //str.Append(syncid);
                str.Append("|");
                return str.ToString();
            }
        }

        [ProtoContract]
        public class AddOk
        {
            [ProtoMember(1)]
            public int fid;

            [ProtoMember(2)]
            public int rev;

            //[ProtoMember(3)]
            //public int syncid;


            public string ToString()
            {
                StringBuilder str = new StringBuilder("AddOk");
                str.Append("|fid->");
                str.Append(fid);
                str.Append("|rev");
                str.Append(rev);
                //str.Append("|syncid");
                //str.Append(syncid);
                str.Append("|");
                return str.ToString();
            }
        }


        /////////////////--END--///////////////////////
        ///////////STRUCT DEFINITIONS /////////////////

    }
}