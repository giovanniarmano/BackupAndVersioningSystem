using System;
using System.Collections.Generic;
using System.Text;
using ProtoBuf;
using System.Net.Sockets;
using System.Threading;

namespace SynchBox_Client
{
    public static partial class proto_client
    {
        private static Mutex wrapMutex = new Mutex();
        private static Mutex getMutex = new Mutex();
        private static volatile int nGet = 0;

        public static ListResponse ListRequestAllWrapper(NetworkStream netStream)
        {
            wrapMutex.WaitOne();
            messagetype_c mt = new messagetype_c();
            mt.msgtype = (Byte)CmdType.ListRequest;

            Serializer.SerializeWithLengthPrefix<messagetype_c>(netStream, mt, PrefixStyle.Base128);
            mt = Serializer.DeserializeWithLengthPrefix<messagetype_c>(netStream, PrefixStyle.Base128);

            if (mt.accepted == false) throw new Exception("Message type not accepted");

            ListRequest listRequest= new ListRequest()
            {
                listReqType = (Byte)ListRequestType.All
            };

            Serializer.SerializeWithLengthPrefix<ListRequest>(netStream, listRequest, PrefixStyle.Base128);
            ListResponse listResponse = Serializer.DeserializeWithLengthPrefix<ListResponse>(netStream, PrefixStyle.Base128);
            wrapMutex.ReleaseMutex();
            return listResponse;
        }

        public static ListResponse ListRequestLastWrapper(NetworkStream netStream)
        {
            wrapMutex.WaitOne();
            messagetype_c mt = new messagetype_c();
            mt.msgtype = (Byte)CmdType.ListRequest;

            Serializer.SerializeWithLengthPrefix<messagetype_c>(netStream, mt, PrefixStyle.Base128);
            mt = Serializer.DeserializeWithLengthPrefix<messagetype_c>(netStream, PrefixStyle.Base128);

            if (mt.accepted == false) throw new Exception("Message type not accepted");

            ListRequest listRequest = new ListRequest()
            {
                listReqType = (Byte)ListRequestType.Last
            };

            Serializer.SerializeWithLengthPrefix<ListRequest>(netStream, listRequest, PrefixStyle.Base128);
            ListResponse listResponse = Serializer.DeserializeWithLengthPrefix<ListResponse>(netStream, PrefixStyle.Base128);

            wrapMutex.ReleaseMutex();
            return listResponse;

        }


        /// <summary>
        /// Inizia una nuova sessione di sincronizzazione
        /// </summary>
        /// <param name="netStream"></param>
        /// <returns>synchsessionid per la sessione di sincronizzazione richiesta</returns>
        public static int BeginSessionWrapper(NetworkStream netStream)
        {
            wrapMutex.WaitOne();
            messagetype_c mt = new messagetype_c();
            mt.msgtype = (Byte)CmdType.BeginSession;

            Serializer.SerializeWithLengthPrefix<messagetype_c>(netStream, mt, PrefixStyle.Base128);
            mt = Serializer.DeserializeWithLengthPrefix<messagetype_c>(netStream, PrefixStyle.Base128);

            if (mt.accepted == false) throw new Exception("Message type not accepted");

            BeginSession beginSession = new BeginSession()
            {
                sessionid = -1
            };

            Serializer.SerializeWithLengthPrefix<BeginSession>(netStream, beginSession, PrefixStyle.Base128);
            beginSession = Serializer.DeserializeWithLengthPrefix<BeginSession>(netStream, PrefixStyle.Base128);

            if (beginSession.sessionid < 0) Logging.WriteToLog("Error in starting new synch session. Returned sessionid <0");

            wrapMutex.ReleaseMutex();
            return beginSession.sessionid;
        }

        /// <summary>
        /// Termina la sessione di sincronizzazione sul server. Lancia un eccezione se qualcosa non va.
        /// </summary>
        /// <param name="netStream"></param>
        /// <param name="synchsessionid">synchsessionid corrente, per verificare di terminare la sessione di synch corretta!</param>
        public static void EndSessionWrapper(NetworkStream netStream,int synchsessionid)
        {
            wrapMutex.WaitOne();
            messagetype_c mt = new messagetype_c();
            mt.msgtype = (Byte)CmdType.EndSession;

            Serializer.SerializeWithLengthPrefix<messagetype_c>(netStream, mt, PrefixStyle.Base128);
            mt = Serializer.DeserializeWithLengthPrefix<messagetype_c>(netStream, PrefixStyle.Base128);

            if (mt.accepted == false) throw new Exception("Message type not accepted");

            EndSession endSession = new EndSession()
            {
                sessionid = synchsessionid,
                succesful = false
            };

            Serializer.SerializeWithLengthPrefix<EndSession>(netStream, endSession, PrefixStyle.Base128);
            endSession = Serializer.DeserializeWithLengthPrefix<EndSession>(netStream, PrefixStyle.Base128);

            if (endSession.succesful == false) Logging.WriteToLog("Error in ENDING synch session. Returned succesful = false");

            wrapMutex.ReleaseMutex();
            return;// endSession.sessionid;
        }


        /// <summary>
        /// Aggiunge un file sul server. Lancia Eccezione in caso di errori!
        /// </summary>
        /// <param name="netStream"></param>
        /// <param name="add">Add Class as ref parameter</param>
        /// <returns>Class AddOk con fid, rev del file aggiunto</returns>
        public static AddOk AddWrapper(NetworkStream netStream, ref Add add)
        {
            wrapMutex.WaitOne();
            messagetype_c mt = new messagetype_c();
            mt.msgtype = (Byte)CmdType.Add;

            Serializer.SerializeWithLengthPrefix<messagetype_c>(netStream, mt, PrefixStyle.Base128);
            mt = Serializer.DeserializeWithLengthPrefix<messagetype_c>(netStream, PrefixStyle.Base128);

            if (mt.accepted == false) throw new Exception("Message type not accepted");
            
            Serializer.SerializeWithLengthPrefix<Add>(netStream, add, PrefixStyle.Base128);
            AddOk addOk = Serializer.DeserializeWithLengthPrefix<AddOk>(netStream, PrefixStyle.Base128);

            if ((addOk.fid < 0)||(addOk.rev <0))// throw new Exception("Error in ADDING. fid < 0 !! rev < 0");
             Logging.WriteToLog("Error in ADDING. fid < 0 !! rev < 0");

            wrapMutex.ReleaseMutex();
            return addOk;
        }

        public static UpdateOk UpdateWrapper(NetworkStream netStream, ref Update update)
        {
            wrapMutex.WaitOne();
            messagetype_c mt = new messagetype_c();
            mt.msgtype = (Byte)CmdType.Update;

            Serializer.SerializeWithLengthPrefix<messagetype_c>(netStream, mt, PrefixStyle.Base128);
            mt = Serializer.DeserializeWithLengthPrefix<messagetype_c>(netStream, PrefixStyle.Base128);

            if (mt.accepted == false) throw new Exception("Message type not accepted");

            Serializer.SerializeWithLengthPrefix<Update>(netStream, update, PrefixStyle.Base128);
            UpdateOk updateOk = Serializer.DeserializeWithLengthPrefix<UpdateOk>(netStream, PrefixStyle.Base128);

            if ((updateOk.fid < 0) || (updateOk.rev < 0))
                Logging.WriteToLog("Error in UPDATING. fid < 0 !! rev < 0");

            wrapMutex.ReleaseMutex();
            return updateOk;
        }

        public static DeleteOk DeleteWrapper(NetworkStream netStream, ref Delete delete)
        {
            wrapMutex.WaitOne();
            messagetype_c mt = new messagetype_c();
            mt.msgtype = (Byte)CmdType.Delete;

            Serializer.SerializeWithLengthPrefix<messagetype_c>(netStream, mt, PrefixStyle.Base128);
            mt = Serializer.DeserializeWithLengthPrefix<messagetype_c>(netStream, PrefixStyle.Base128);

            if (mt.accepted == false) throw new Exception("Message type not accepted");

            Serializer.SerializeWithLengthPrefix<Delete>(netStream, delete, PrefixStyle.Base128);
            DeleteOk deleteOk = Serializer.DeserializeWithLengthPrefix<DeleteOk>(netStream, PrefixStyle.Base128);

            if ((deleteOk.fid < 0)) Logging.WriteToLog("Error in DELETING. fid < 0 ");

            wrapMutex.ReleaseMutex();
            return deleteOk;
        }

        public static DeleteOk DeleteFolderWrapper(NetworkStream netStream, ref Delete delete)
        {
            wrapMutex.WaitOne();
            messagetype_c mt = new messagetype_c();
            mt.msgtype = (Byte)CmdType.DeleteFolder;

            Serializer.SerializeWithLengthPrefix<messagetype_c>(netStream, mt, PrefixStyle.Base128);
            mt = Serializer.DeserializeWithLengthPrefix<messagetype_c>(netStream, PrefixStyle.Base128);

            if (mt.accepted == false) throw new Exception("Message type not accepted");

            Serializer.SerializeWithLengthPrefix<Delete>(netStream, delete, PrefixStyle.Base128);
            DeleteOk deleteOk = Serializer.DeserializeWithLengthPrefix<DeleteOk>(netStream, PrefixStyle.Base128);

            if ((deleteOk.fid < 0)) Logging.WriteToLog("Error in DELETING FOLDER. fid < 0 ");

            wrapMutex.ReleaseMutex();
            return deleteOk;
        }


        public static int GetSynchIdWrapper(NetworkStream netStream)
        {
            wrapMutex.WaitOne();
            messagetype_c mt = new messagetype_c();
            mt.msgtype = (Byte)CmdType.GetSynchId;

            Serializer.SerializeWithLengthPrefix<messagetype_c>(netStream, mt, PrefixStyle.Base128);
            mt = Serializer.DeserializeWithLengthPrefix<messagetype_c>(netStream, PrefixStyle.Base128);

            if (mt.accepted == false) throw new Exception("Message type not accepted");

            GetSynchid getSynchId = new GetSynchid() {
                synchid = -1
            };

            Serializer.SerializeWithLengthPrefix<GetSynchid>(netStream, getSynchId, PrefixStyle.Base128);
            getSynchId = Serializer.DeserializeWithLengthPrefix<GetSynchid>(netStream, PrefixStyle.Base128);

            if ((getSynchId.synchid < -1))
                Logging.WriteToLog("Error in GetSynchId. symnchid < -1 ");

            wrapMutex.ReleaseMutex();
            return getSynchId.synchid;
        }

        //GetListWrapper
        //TODO Potrebbe

        /// <summary>
        /// ATTENZIONE. DA Usare con con GetResponseWrapper n volte!
        /// </summary>
        /// <param name="netStream"></param>
        /// <param name="getList"></param>
        public static void GetListWrapper(NetworkStream netStream, ref GetList getList)
        {
            if (getList == null)
            {
                Logging.WriteToLog("getList null");
                return;
            }

            if (getList.fileList == null)
            {
                Logging.WriteToLog("getlist .filelist null");
                return;
            }
            if (getList.fileList.Count == 0) {
                Logging.WriteToLog("getlist filelist count == 0");
                return;
            }

            wrapMutex.WaitOne();
            Logging.WriteToLog("Acquire Mutex");
            nGet = getList.fileList.Count;
            Logging.WriteToLog("nGet = " + nGet);
            if (getList.n != nGet)
                Logging.WriteToLog("WARNING .... getList.n != nGet" + getList.n + "  " + nGet);

            messagetype_c mt = new messagetype_c();
            mt.msgtype = (Byte)CmdType.GetList;

            Serializer.SerializeWithLengthPrefix<messagetype_c>(netStream, mt, PrefixStyle.Base128);
            mt = Serializer.DeserializeWithLengthPrefix<messagetype_c>(netStream, PrefixStyle.Base128);

            if (mt.accepted == false) throw new Exception("Message type not accepted");

            Serializer.SerializeWithLengthPrefix<GetList>(netStream, getList, PrefixStyle.Base128);

            Logging.WriteToLog("Get List Wrapper End correctly");
            return;
        }



        /// <summary>
        /// DA CHIAMARE N Volte solo dopo GetListWrapper. La GetResponse deve essere passata per riferimento dal chiamante(anche vuota)
        /// </summary>
        /// <param name="netStream"></param>
        /// <param name="getResponse">Passata by ref dal chiamante!</param>
        public static void GetResponseWrapper(NetworkStream netStream, ref GetResponse getResponse)
        {
            if (nGet == 0) { 
                Logging.WriteToLog("panic call of getresponse no sense!");
                return;
            }

            getResponse = Serializer.DeserializeWithLengthPrefix<GetResponse>(netStream, PrefixStyle.Base128);
            nGet--;
            if (nGet == 0) {
                Logging.WriteToLog("Get  Response  Wrapper.   Releasing Mutex. nGet = " + nGet);
                wrapMutex.ReleaseMutex();
            }
            else
            {
                Logging.WriteToLog("Get Response Wrapper. NOT Releasing Mutex. nGet = " + nGet);
            }
            return;
        }

        public static bool LockAcquireWrapper(NetworkStream netStream)
        {
            wrapMutex.WaitOne();
            messagetype_c mt = new messagetype_c();
            mt.msgtype = (Byte)CmdType.Lock;

            Serializer.SerializeWithLengthPrefix<messagetype_c>(netStream, mt, PrefixStyle.Base128);
            mt = Serializer.DeserializeWithLengthPrefix<messagetype_c>(netStream, PrefixStyle.Base128);

            if (mt.accepted == false) throw new Exception("Message type not accepted");

            lock_c lock_c = new lock_c();
            lock_c.lockType = (byte)CmdType.LockAcquire;

            Serializer.SerializeWithLengthPrefix<lock_c>(netStream, lock_c, PrefixStyle.Base128);
            lock_c lock_response = Serializer.DeserializeWithLengthPrefix<lock_c>(netStream, PrefixStyle.Base128);

            wrapMutex.ReleaseMutex();
            return lock_response.succesfull;
        }

        public static bool LockReleaseWrapper(NetworkStream netStream)
        {
            wrapMutex.WaitOne();
            messagetype_c mt = new messagetype_c();
            mt.msgtype = (Byte)CmdType.Lock;

            Serializer.SerializeWithLengthPrefix<messagetype_c>(netStream, mt, PrefixStyle.Base128);
            mt = Serializer.DeserializeWithLengthPrefix<messagetype_c>(netStream, PrefixStyle.Base128);

            if (mt.accepted == false) throw new Exception("Message type not accepted");

            lock_c lock_c = new lock_c();
            lock_c.lockType = (byte)CmdType.LockRelease;

            Serializer.SerializeWithLengthPrefix<lock_c>(netStream, lock_c, PrefixStyle.Base128);
            lock_c lock_response = Serializer.DeserializeWithLengthPrefix<lock_c>(netStream, PrefixStyle.Base128);

            wrapMutex.ReleaseMutex();
            return lock_response.succesfull;
        }

    }
}