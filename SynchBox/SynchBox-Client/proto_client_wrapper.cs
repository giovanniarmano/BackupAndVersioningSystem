using System;
using System.Collections.Generic;
using System.Text;
using ProtoBuf;
using System.Net.Sockets;
using System.Threading;
using System.Windows;

namespace SynchBox_Client
{
    public static partial class proto_client
    {
        private static Mutex wrapMutex = new Mutex();
        //private static Mutex getMutex = new Mutex();//unused
        private static volatile int nGet = 0;
        private static volatile int nGetSynchId = 0;

        public static ListResponse ListRequestAllWrapper(NetworkStream netStream)
        {
            ListResponse listResponse = null;
            try {
                Logging.WriteToLog("ListRequest ALL Wrapper .....");

                wrapMutex.WaitOne();
                messagetype_c mt = new messagetype_c();
                mt.msgtype = (Byte)CmdType.ListRequest;

                Serializer.SerializeWithLengthPrefix<messagetype_c>(netStream, mt, PrefixStyle.Base128);
                mt = Serializer.DeserializeWithLengthPrefix<messagetype_c>(netStream, PrefixStyle.Base128);

                if (mt.accepted == false) throw new Exception("Message type not accepted");

                ListRequest listRequest = new ListRequest()
                {
                    listReqType = (Byte)ListRequestType.All
                };

                Serializer.SerializeWithLengthPrefix<ListRequest>(netStream, listRequest, PrefixStyle.Base128);
                listResponse = Serializer.DeserializeWithLengthPrefix<ListResponse>(netStream, PrefixStyle.Base128);
                Logging.WriteToLog("ListRequest ALL Wrapper DONE");
            }
            catch (System.IO.IOException sie)
            {
                Logging.WriteToLog("Wrapper System Io Exception. Connection closed by party.\n--->Logout current session & cleanup\n" + sie.ToString());
                doLogout();
            }
            catch (Exception e)
            {
                Logging.WriteToLog("Generic Exception in wrapper -> toserver communication.\n" + e.ToString());
               // throw;
            }
            finally
            {
                wrapMutex.ReleaseMutex();
            }
            return listResponse;
        }

        public static ListResponse ListRequestLastWrapper(NetworkStream netStream)
        {
            ListResponse listResponse = null;
            try {
                Logging.WriteToLog("ListRequest LAST Wrapper .....");

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
                listResponse = Serializer.DeserializeWithLengthPrefix<ListResponse>(netStream, PrefixStyle.Base128);

                Logging.WriteToLog("ListRequest LAST Wrapper DONE");
            }
            catch (System.IO.IOException sie)
            {
                Logging.WriteToLog("Wrapper System Io Exception. Connection closed by party.\n--->Logout current session & cleanup\n" + sie.ToString());
                doLogout();
            }
            catch (Exception e)
            {
                Logging.WriteToLog("Generic Exception in wrapper -> toserver communication.\n" + e.ToString());
                //throw;
            }
            finally
            {
                wrapMutex.ReleaseMutex();
            }
            return listResponse;

        }


        /// <summary>
        /// Inizia una nuova sessione di sincronizzazione
        /// </summary>
        /// <param name="netStream"></param>
        /// <returns>synchsessionid per la sessione di sincronizzazione richiesta</returns>
        public static int BeginSessionWrapper(NetworkStream netStream)
        {
            BeginSession beginSession = null;
            try {
                Logging.WriteToLog("BeginSession Wrapper .....");

                wrapMutex.WaitOne();
                messagetype_c mt = new messagetype_c();
                mt.msgtype = (Byte)CmdType.BeginSession;

                Serializer.SerializeWithLengthPrefix<messagetype_c>(netStream, mt, PrefixStyle.Base128);
                mt = Serializer.DeserializeWithLengthPrefix<messagetype_c>(netStream, PrefixStyle.Base128);

                if (mt.accepted == false) throw new Exception("Message type not accepted");

                beginSession = new BeginSession()
                {
                    sessionid = -1
                };

                Serializer.SerializeWithLengthPrefix<BeginSession>(netStream, beginSession, PrefixStyle.Base128);
                beginSession = Serializer.DeserializeWithLengthPrefix<BeginSession>(netStream, PrefixStyle.Base128);

                if (beginSession.sessionid < 0) Logging.WriteToLog("Error in starting new synch session. Returned sessionid <0");


                Logging.WriteToLog("BeginSession Wrapper DONE");
            }
            catch (System.IO.IOException sie)
            {
                Logging.WriteToLog("Wrapper System Io Exception. Connection closed by party.\n--->Logout current session & cleanup\n" + sie.ToString());
                doLogout();
            }
            catch (Exception e)
            {
                Logging.WriteToLog("Generic Exception in wrapper -> toserver communication.\n" + e.ToString());
                //throw;
            }
            finally
            {
                wrapMutex.ReleaseMutex();
            }
            if (beginSession!=null)
                return beginSession.sessionid;
            return -1; //TODO ??
        }

        /// <summary>
        /// Termina la sessione di sincronizzazione sul server. Lancia un eccezione se qualcosa non va.
        /// </summary>
        /// <param name="netStream"></param>
        /// <param name="synchsessionid">synchsessionid corrente, per verificare di terminare la sessione di synch corretta!</param>
        public static void EndSessionWrapper(NetworkStream netStream,int synchsessionid)
        {

            try
            {
                Logging.WriteToLog("EndSession Wrapper .....");

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

                Logging.WriteToLog("EndSession Wrapper DONE");
            }
            catch (System.IO.IOException sie)
            {
                Logging.WriteToLog("Wrapper System Io Exception. Connection closed by party.\n--->Logout current session & cleanup\n" + sie.ToString());
                doLogout();
            }
            catch (Exception e)
            {
                Logging.WriteToLog("Generic Exception in wrapper -> toserver communication.\n" + e.ToString());
                //throw;
            }
            finally
            {
                wrapMutex.ReleaseMutex();
            }
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
            AddOk addOk = null;
            try {
                Logging.WriteToLog("Add Wrapper .....");

                wrapMutex.WaitOne();
                messagetype_c mt = new messagetype_c();
                mt.msgtype = (Byte)CmdType.Add;

                Serializer.SerializeWithLengthPrefix<messagetype_c>(netStream, mt, PrefixStyle.Base128);
                mt = Serializer.DeserializeWithLengthPrefix<messagetype_c>(netStream, PrefixStyle.Base128);

                if (mt.accepted == false) throw new Exception("Message type not accepted");

                Serializer.SerializeWithLengthPrefix<Add>(netStream, add, PrefixStyle.Base128);
                addOk = Serializer.DeserializeWithLengthPrefix<AddOk>(netStream, PrefixStyle.Base128);

                if ((addOk.fid < 0) || (addOk.rev < 0))// throw new Exception("Error in ADDING. fid < 0 !! rev < 0");
                    Logging.WriteToLog("Error in ADDING. fid < 0 !! rev < 0");

                Logging.WriteToLog("Add Wrapper DONE");
            }
            catch (System.IO.IOException sie)
            {
                Logging.WriteToLog("Wrapper System Io Exception. Connection closed by party.\n--->Logout current session & cleanup\n" + sie.ToString());
                doLogout();
            }
            catch (Exception e)
            {
                Logging.WriteToLog("Generic Exception in wrapper -> toserver communication.\n" + e.ToString());
                //throw;
            }
            finally
            {
                wrapMutex.ReleaseMutex();
            }
            return addOk;
        }

        public static UpdateOk UpdateWrapper(NetworkStream netStream, ref Update update)
        {
            UpdateOk updateOk = null;
            try {
                Logging.WriteToLog("Update Wrapper .....");

                wrapMutex.WaitOne();
                messagetype_c mt = new messagetype_c();
                mt.msgtype = (Byte)CmdType.Update;

                Serializer.SerializeWithLengthPrefix<messagetype_c>(netStream, mt, PrefixStyle.Base128);
                mt = Serializer.DeserializeWithLengthPrefix<messagetype_c>(netStream, PrefixStyle.Base128);

                if (mt.accepted == false) throw new Exception("Message type not accepted");

                Serializer.SerializeWithLengthPrefix<Update>(netStream, update, PrefixStyle.Base128);
                updateOk = Serializer.DeserializeWithLengthPrefix<UpdateOk>(netStream, PrefixStyle.Base128);

                if ((updateOk.fid < 0) || (updateOk.rev < 0))
                    Logging.WriteToLog("Error in UPDATING. fid < 0 !! rev < 0");

                Logging.WriteToLog("Update Wrapper DONE");
            }
            catch (System.IO.IOException sie)
            {
                Logging.WriteToLog("Wrapper System Io Exception. Connection closed by party.\n--->Logout current session & cleanup\n" + sie.ToString());
                doLogout();
            }
            catch (Exception e)
            {
                Logging.WriteToLog("Generic Exception in wrapper -> toserver communication.\n" + e.ToString());
                //throw;
            }
            finally
            {
                wrapMutex.ReleaseMutex();
            }
            
            return updateOk;
        }

        public static DeleteOk DeleteWrapper(NetworkStream netStream, ref Delete delete)
        {
            DeleteOk deleteOk = null;
            try {
                Logging.WriteToLog("Delete Wrapper .....");
                wrapMutex.WaitOne();
                messagetype_c mt = new messagetype_c();
                mt.msgtype = (Byte)CmdType.Delete;

                Serializer.SerializeWithLengthPrefix<messagetype_c>(netStream, mt, PrefixStyle.Base128);
                mt = Serializer.DeserializeWithLengthPrefix<messagetype_c>(netStream, PrefixStyle.Base128);

                if (mt.accepted == false) throw new Exception("Message type not accepted");

                Serializer.SerializeWithLengthPrefix<Delete>(netStream, delete, PrefixStyle.Base128);
                deleteOk = Serializer.DeserializeWithLengthPrefix<DeleteOk>(netStream, PrefixStyle.Base128);

                if ((deleteOk.fid < 0)) Logging.WriteToLog("Error in DELETING. fid < 0 ");

                Logging.WriteToLog("Delete Wrapper DONE");
            }
            catch (System.IO.IOException sie)
            {
                Logging.WriteToLog("Wrapper System Io Exception. Connection closed by party.\n--->Logout current session & cleanup\n" + sie.ToString());
                doLogout();
            }
            catch (Exception e)
            {
                Logging.WriteToLog("Generic Exception in wrapper -> toserver communication.\n" + e.ToString());
                //throw;
            }
            finally
            {
                wrapMutex.ReleaseMutex();
            }
            return deleteOk;
        }

        public static DeleteOk DeleteFolderWrapper(NetworkStream netStream, ref Delete delete)
        {
            DeleteOk deleteOk = null;
            try {
                Logging.WriteToLog("Delete Folder Wrapper .....");
                wrapMutex.WaitOne();
                messagetype_c mt = new messagetype_c();
                mt.msgtype = (Byte)CmdType.DeleteFolder;

                Serializer.SerializeWithLengthPrefix<messagetype_c>(netStream, mt, PrefixStyle.Base128);
                mt = Serializer.DeserializeWithLengthPrefix<messagetype_c>(netStream, PrefixStyle.Base128);

                if (mt.accepted == false) throw new Exception("Message type not accepted");

                Serializer.SerializeWithLengthPrefix<Delete>(netStream, delete, PrefixStyle.Base128);
                deleteOk = Serializer.DeserializeWithLengthPrefix<DeleteOk>(netStream, PrefixStyle.Base128);

                if ((deleteOk.fid < 0)) Logging.WriteToLog("Error in DELETING FOLDER. fid < 0 ");


                Logging.WriteToLog("Delete Folder Wrapper DONE");
            }
            catch (System.IO.IOException sie)
            {
                Logging.WriteToLog("Wrapper System Io Exception. Connection closed by party.\n--->Logout current session & cleanup\n" + sie.ToString());
                doLogout();
            }
            catch (Exception e)
            {
                Logging.WriteToLog("Generic Exception in wrapper -> toserver communication.\n" + e.ToString());
               // throw;
            }
            finally
            {
                wrapMutex.ReleaseMutex();
            }
            return deleteOk;
        }


        public static int GetSynchIdWrapper(NetworkStream netStream)
        {
            GetSynchid getSynchId = null;
            try {
                if (++nGetSynchId % 100 == 0) Logging.WriteToLog("Get Synch Id Wrapper (100) ......");
                wrapMutex.WaitOne();
                messagetype_c mt = new messagetype_c();
                mt.msgtype = (Byte)CmdType.GetSynchId;

                Serializer.SerializeWithLengthPrefix<messagetype_c>(netStream, mt, PrefixStyle.Base128);
                mt = Serializer.DeserializeWithLengthPrefix<messagetype_c>(netStream, PrefixStyle.Base128);

                if (mt.accepted == false) throw new Exception("Message type not accepted");

                getSynchId = new GetSynchid() {
                    synchid = -1
                };

                Serializer.SerializeWithLengthPrefix<GetSynchid>(netStream, getSynchId, PrefixStyle.Base128);
                getSynchId = Serializer.DeserializeWithLengthPrefix<GetSynchid>(netStream, PrefixStyle.Base128);

                if ((getSynchId.synchid < -1))
                    Logging.WriteToLog("Error in GetSynchId. symnchid < -1 ");


                if (nGetSynchId % 100 == 0) Logging.WriteToLog("Get Synch Id Wrapper (100) DONE");
            }
            catch (System.IO.IOException sie)
            {
                Logging.WriteToLog("Wrapper System Io Exception. Connection closed by party.\n--->Logout current session & cleanup\n" + sie.ToString());
                doLogout();
            }
            catch (Exception e)
            {
                Logging.WriteToLog("Generic Exception in wrapper -> toserver communication.\n" + e.ToString());
                //throw;
            }
            finally
            {
                wrapMutex.ReleaseMutex();
            }
            if(getSynchId!=null)
                return getSynchId.synchid;
            return -1; //TODO ??
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
            //Initial not null checks
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

            //acquire mutex, try to deserialize!
            try {
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
            }
            catch (System.IO.IOException sie)
            {
                Logging.WriteToLog("Wrapper System Io Exception. Connection closed by party.\n--->Logout current session & cleanup\n" + sie.ToString());
                doLogout();
                wrapMutex.ReleaseMutex();
            }
            catch (Exception e)
            {
                Logging.WriteToLog("Generic Exception in wrapper -> toserver communication.\n" + e.ToString());
                wrapMutex.ReleaseMutex();
                //throw;
            }
            
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

            // se durante la getResponse vengo interrotto... faccio logout se sono il primo a riscontrare sie Exception
            //ma in ogni caso rilascio il mutex
            //se non lo rilasciassi, quando il thread termina potrebbero succedere casini!
            try {
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
            }
            catch (System.IO.IOException sie)
            {
                Logging.WriteToLog("Wrapper System Io Exception. Connection closed by party.\n--->Logout current session & cleanup\n" + sie.ToString());
                doLogout();
                Logging.WriteToLog("RILASCIO UGUALMENTE IL MUTEX");
                wrapMutex.ReleaseMutex();
            }
            catch (Exception e)
            {
                Logging.WriteToLog("Generic Exception in wrapper -> toserver communication.\n" + e.ToString());
                Logging.WriteToLog("RILASCIO UGUALMENTE IL MUTEX");
                wrapMutex.ReleaseMutex();
            }
            
            return;
        }

        public static bool LockAcquireWrapper(NetworkStream netStream)
        {
            lock_c lock_response = null;
            try { 
                Logging.WriteToLog("LockAcquire db Wrapper .....");

                wrapMutex.WaitOne();
                messagetype_c mt = new messagetype_c();
                mt.msgtype = (Byte)CmdType.Lock;

                Serializer.SerializeWithLengthPrefix<messagetype_c>(netStream, mt, PrefixStyle.Base128);
                mt = Serializer.DeserializeWithLengthPrefix<messagetype_c>(netStream, PrefixStyle.Base128);

                if (mt.accepted == false) throw new Exception("Message type not accepted");

                lock_c lock_c = new lock_c();
                lock_c.lockType = (byte)CmdType.LockAcquire;

                Serializer.SerializeWithLengthPrefix<lock_c>(netStream, lock_c, PrefixStyle.Base128);
                lock_response = Serializer.DeserializeWithLengthPrefix<lock_c>(netStream, PrefixStyle.Base128);   

            }
            catch (System.IO.IOException sie)
            {
                Logging.WriteToLog("Wrapper System Io Exception. Connection closed by party.\n--->Logout current session & cleanup\n" + sie.ToString());
                doLogout();
            }
            catch (Exception e)
            {
                Logging.WriteToLog("Generic Exception in wrapper -> toserver communication.\n" + e.ToString());
                //throw;
            }
            finally
            {
                Logging.WriteToLog("LockAcquire db Wrapper DONE");
                wrapMutex.ReleaseMutex();
            }

            if (lock_response!=null)
                    return lock_response.succesfull;
                return false;
        }

        public static bool LockReleaseWrapper(NetworkStream netStream)
        {
            lock_c lock_response  = null;
            try {
                Logging.WriteToLog("LockRelease db Wrapper .....");

                wrapMutex.WaitOne();
                messagetype_c mt = new messagetype_c();
                mt.msgtype = (Byte)CmdType.Lock;

                Serializer.SerializeWithLengthPrefix<messagetype_c>(netStream, mt, PrefixStyle.Base128);
                mt = Serializer.DeserializeWithLengthPrefix<messagetype_c>(netStream, PrefixStyle.Base128);

                if (mt.accepted == false) throw new Exception("Message type not accepted");

                lock_c lock_c = new lock_c();
                lock_c.lockType = (byte)CmdType.LockRelease;

                Serializer.SerializeWithLengthPrefix<lock_c>(netStream, lock_c, PrefixStyle.Base128);
                lock_response = Serializer.DeserializeWithLengthPrefix<lock_c>(netStream, PrefixStyle.Base128);

                
                
            } catch (System.IO.IOException sie)
            {
                Logging.WriteToLog("Wrapper System Io Exception. Connection closed by party.\n--->Logout current session & cleanup\n" + sie.ToString());
                doLogout();
            }
            catch (Exception e)
            {
                Logging.WriteToLog("Generic Exception in wrapper -> toserver communication.\n" + e.ToString());
               // throw;
            }
            finally {
                Logging.WriteToLog("LockRelease db Wrapper DONE");
                wrapMutex.ReleaseMutex();
            }
            if(lock_response!=null)
                return lock_response.succesfull;
            return false;
        }

        private static void doLogout()
        {
            System.Windows.MessageBox.Show("Connection Closed by Server!");

            MainWindow.tw.Dispatcher.Invoke(
                new Action(() => MainWindow.tw.my_do_logout()), System.Windows.Threading.DispatcherPriority.Background, null);
          
        }
    }
}