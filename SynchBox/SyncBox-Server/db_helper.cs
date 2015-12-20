using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows;
using System.Data.SQLite;
using System.Data;
using System.Configuration;

namespace SyncBox_Server
{
    public static partial class db
    {

        public static int BeginSession(int uid)
        {
            int synchsessionid;
            SQLiteConnection cnn = new SQLiteConnection(dbConnection);
            cnn.Open();
            using (SQLiteCommand mycommand = new SQLiteCommand(cnn))
            {
                try
                {
                    //BEGIN TRANSACTION
                    using (var transaction = cnn.BeginTransaction())
                    {
                        //seleziono massimo sessionsynchid per uid
                        mycommand.CommandText = @"SELECT MAX(SYNCH_SESSION.synchsessionid) AS maxsynchsessionid
                                                FROM SYNCH_SESSION
                                                WHERE SYNCH_SESSION.uid = @uid
                                                ;";
                        mycommand.Prepare();
                        mycommand.Parameters.AddWithValue("@uid", uid);

                        object value = mycommand.ExecuteScalar();
                        int maxsynchsessionid;
                        try
                        {
                            maxsynchsessionid = int.Parse(value.ToString());
                        }
                        catch (Exception e)
                        {
                            maxsynchsessionid = 0;
                        }

                        synchsessionid = maxsynchsessionid + 1;

                        //inserisco la synchSession
                        mycommand.CommandText = @"INSERT INTO SYNCH_SESSION(uid,synchsessionid,timestamp,n_added,n_updated,n_deleted)
                                                VALUES (@uid,@synchsessionid,datetime('NOW'),0,0,0)
                                                ;";
                        mycommand.Prepare();
                        mycommand.Parameters.AddWithValue("@uid", uid);
                        mycommand.Parameters.AddWithValue("@synchsessionid", synchsessionid);
                        
                        int nUpdated = mycommand.ExecuteNonQuery();
                        if (nUpdated != 1)
                            throw new Exception("No Row updated! Rollback");
                        
                        //END TRANSACTION
                        transaction.Commit();


                        return synchsessionid;

                    }
                }
                catch
                {
                    throw;
                }

            }
            
        }

        //ADD
        public static proto_server.AddOk Add(ref proto_server.Add add, ref proto_server.login_c currentUser)
        {
            //add.filename;add.folder;add.fileDump; uid;
            proto_server.AddOk addOk = new proto_server.AddOk();

            //calcolo md5 & campi timestamp
            string md5 = proto_server.CalculateMD5Hash(add.fileDump);
            // string timestamp = DateTime.Now.ToString();
            // DateTime dateTime = DateTime.Parse(timestamp);   

            SQLiteConnection cnn = new SQLiteConnection(dbConnection);
            cnn.Open();
            using (SQLiteCommand mycommand = new SQLiteCommand(cnn))
            {
                try
                {
                    //BEGIN TRANSACTION
                    using (var transaction = cnn.BeginTransaction())
                    {                        
                        //controllo se filename folder not present
                        //se si lancio eccezione!
                        mycommand.CommandText = @"SELECT COUNT(*)
                                            FROM HISTORY
                                            WHERE HISTORY.uid = @uid
                                            AND HISTORY.filename = @filename
                                            AND HISTORY.folder = @folder
                                            AND HISTORY.dir = @dir
                                            ; ";
                        mycommand.Prepare();
                        mycommand.Parameters.AddWithValue("@uid", currentUser.uid);
                        mycommand.Parameters.AddWithValue("@filename", add.filename);
                        mycommand.Parameters.AddWithValue("@folder", add.folder);
                        mycommand.Parameters.AddWithValue("@dir", add.dir);
                        object value = mycommand.ExecuteScalar();

                        //TODO IMPROVEEEE
                        if (value == null)
                        {
                            throw new Exception("Querydb: count(*) if filename/folder is present in the folder for uid. RETURN NULL. PANIC");
                        }
                        if (value.ToString().CompareTo("0") != 0)
                        {
                            Logging.WriteToLog("file/folder already present in db." + add.ToString());
                            //fai query e ritorna fir, rev
                            mycommand.CommandText = @"SELECT fid,rev
                                            FROM HISTORY
                                            WHERE HISTORY.uid = @uid
                                            AND HISTORY.filename = @filename
                                            AND HISTORY.folder = @folder
                                            AND HISTORY.dir = @dir
                                            ; ";
                            mycommand.Prepare();
                            mycommand.Parameters.AddWithValue("@uid", currentUser.uid);
                            mycommand.Parameters.AddWithValue("@filename", add.filename);
                            mycommand.Parameters.AddWithValue("@folder", add.folder);
                            mycommand.Parameters.AddWithValue("@dir", add.dir);

                            SQLiteDataReader reader = mycommand.ExecuteReader();
                            DataTable dt = new DataTable();
                            dt.Load(reader);

                            DataRow row = dt.Rows[0];
                            int fileid, rev;

                            try
                            {
                                var values = row.ItemArray;

                                fileid = int.Parse(values[0].ToString());
                                rev = int.Parse(values[1].ToString());
                            }
                            catch (Exception e)
                            {
                                Logging.WriteToLog("ERROR PARSING!in Add already present file/folder method" + e.ToString());
                                throw;
                            }
                            addOk.fid = fileid;
                            addOk.rev = rev;
                            return addOk;

                        }

                        //seleziono  max syncid tra uid
                        mycommand.CommandText = @"SELECT MAX(SNAPSHOT.syncid)
                                                FROM SNAPSHOT
                                                WHERE SNAPSHOT.uid = @uid
                                                ;";
                        mycommand.Prepare();
                        mycommand.Parameters.AddWithValue("@uid", currentUser.uid);
                        value = mycommand.ExecuteScalar();
                        int maxsyncId = -1;
                        try
                        {
                            maxsyncId = int.Parse(value.ToString());
                        }
                        catch (Exception e)
                        {
                            Logging.WriteToLog("User not still present in history, snapshot, filedump!\n e-> " + e.ToString());
                            maxsyncId = 0;
                        }

                        int syncId = maxsyncId + 1;

                        //seleziono  max fid tra uid
                        mycommand.CommandText = @"SELECT MAX(HISTORY.fid)
                                                FROM HISTORY
                                                WHERE HISTORY.uid = @uid
                                                ;";
                        mycommand.Prepare();
                        mycommand.Parameters.AddWithValue("@uid", currentUser.uid);
                        value = mycommand.ExecuteScalar();
                        int maxfid = -1;

                        try
                        {
                            maxfid = int.Parse(value.ToString());
                        }
                        catch (Exception e)
                        {
                            maxfid = 0;
                        }
                        //fid
                        int fid = maxfid + 1;

                        int folderid = -1;
                        
                            //trovo il folder id 
                            //seleziono  max fid tra uid
                            mycommand.CommandText = @"SELECT fid
                                                        FROM HISTORY
                                                        WHERE (folder || filename) = @concatfolder
                                                        AND dir = @dir
                                                        AND uid = @uid
                                                        ; ";
                            mycommand.Prepare();
                            mycommand.Parameters.AddWithValue("@uid", currentUser.uid);
                            mycommand.Parameters.AddWithValue("@concatfolder", add.folder);
                            mycommand.Parameters.AddWithValue("@dir", true);
                            value = mycommand.ExecuteScalar();

                            folderid = -2;

                            try
                            {
                                folderid = int.Parse(value.ToString());
                            }
                            catch (Exception e)
                            {
                                folderid = 0;
                            }

                        //inserisco in HISTORY SNAPSJOT  e FILE_DUMP
                        //Add vuol dire che non ho da fare update ma solo insert
                        //HISTORY INSERT
                        mycommand.CommandText = @"INSERT INTO HISTORY(uid,fid,rev,filename,folder,timestamp,md5,deleted,synchsessionid,dir,folder_id)
                                            VALUES (@uid,@fid,@rev,@filename,@folder,DATETIME('NOW'),@md5,@deleted,@synchsessionid,@dir,@folder_id)
                                                ; ";
                        mycommand.Prepare();
                        mycommand.Parameters.AddWithValue("@uid", currentUser.uid);
                        mycommand.Parameters.AddWithValue("@fid", fid);
                        mycommand.Parameters.AddWithValue("@rev", 1);
                        
                        mycommand.Parameters.AddWithValue("@folder", add.folder);
                        //mycommand.Parameters.AddWithValue("@timestamp", timestamp);
                        
                        mycommand.Parameters.AddWithValue("@deleted", false);
                        mycommand.Parameters.AddWithValue("@synchsessionid", currentUser.synchsessionid);
                        mycommand.Parameters.AddWithValue("@dir", add.dir);
                        mycommand.Parameters.AddWithValue("@folder_id", folderid);

                        switch (add.dir)
                        {
                            case true:
                                //ADD DIRECTORY
                                mycommand.Parameters.AddWithValue("@md5", null);
                                //TOCHECK
                                if (!add.filename[add.filename.Length-1].Equals('\\') ){
                                    add.filename += '\\';
                                }
                                mycommand.Parameters.AddWithValue("@filename", add.filename);
                                break;

                            case false:
                                //ADD FILE

                                //DEVO FARE ALTRA QUERY PER capire a che folder faccio riferimento
                                //lookup di add.folder in concat folder+filename and dir = true in HISTORY

                                mycommand.Parameters.AddWithValue("@filename", add.filename);
                                mycommand.Parameters.AddWithValue("@md5", md5);
                               
                                break;
                        }

                        //DEBUG HERE!!!
                        int nUpdated = mycommand.ExecuteNonQuery();
                        if (nUpdated != 1)
                            throw new Exception("No Row updated! Rollback");

                        //SNAPSHOT INSERT
                        //HISTORY INSERT
                        mycommand.CommandText = @"INSERT INTO SNAPSHOT(uid,fid,rev,syncid)
                                            VALUES (@uid,@fid,@rev,@syncid)
                                            ;";
                        mycommand.Prepare();
                        mycommand.Parameters.AddWithValue("@uid", currentUser.uid);
                        mycommand.Parameters.AddWithValue("@fid", fid);
                        mycommand.Parameters.AddWithValue("@rev", 1);
                        mycommand.Parameters.AddWithValue("@syncid", syncId);

                        nUpdated = mycommand.ExecuteNonQuery();
                        if (nUpdated != 1)
                            throw new Exception("No Row updated! Rollback");
                        
                        //update # added
                        mycommand.CommandText = @"UPDATE SYNCH_SESSION
                                                SET n_added = n_added +1
                                                WHERE uid = @uid
                                                AND synchsessionid = @synchsessionid
                                                ;";
                        mycommand.Prepare();
                        mycommand.Parameters.AddWithValue("@uid", currentUser.uid);
                        mycommand.Parameters.AddWithValue("@syncsessionid", currentUser.synchsessionid);

                        nUpdated = mycommand.ExecuteNonQuery();
                        if (nUpdated != 1)
                            throw new Exception("No Row updated! Rollback");
                        
                        if (!add.dir) { 
                            //FILEDUMP INSERT
                            mycommand.CommandText = @"INSERT INTO FILES_DUMP(uid,fid,rev,filedump)
                                                VALUES (@uid,@fid,@rev,@filedump)
                                                ;";
                            mycommand.Prepare();
                            mycommand.Parameters.AddWithValue("@uid", currentUser.uid);
                            mycommand.Parameters.AddWithValue("@fid", fid);
                            mycommand.Parameters.AddWithValue("@rev", 1);
                            mycommand.Parameters.AddWithValue("@filedump", add.fileDump);

                            nUpdated = mycommand.ExecuteNonQuery();
                            if (nUpdated != 1)
                                throw new Exception("No Row updated! Rollback");
                            
                        }
                        addOk.fid = fid;
                        addOk.rev = 1;
                        //END TRANSACTION
                        transaction.Commit();
                    }
                }
                catch (Exception e)
                {
                    Logging.WriteToLog(e.ToString());
                    addOk.fid = -1;
                    return addOk;
                }
                //manage try catch transaction commit
            }
            return addOk;
        }

        public static proto_server.lock_c Lock(ref proto_server.lock_c lock_c, ref proto_server.login_c currentUser)
        {
            proto_server.lock_c lock_response = new proto_server.lock_c();
            lock_response.succesfull = false;

            SQLiteConnection cnn = new SQLiteConnection(dbConnection);
            cnn.Open();
            using (SQLiteCommand mycommand = new SQLiteCommand(cnn))
            {
                try
                {
                    //BEGIN TRANSACTION
                    using (var transaction = cnn.BeginTransaction())
                    {
                        //GET HISTORY INFORMATION
                        mycommand.CommandText = @"SELECT USERS.lock
                                                FROM USERS
                                                WHERE USERS.uid=@uid 
                                                ;";
                        mycommand.Prepare();
                        mycommand.Parameters.AddWithValue("@uid", currentUser.uid);
                        
                        SQLiteDataReader reader = mycommand.ExecuteReader();
                        DataTable dt = new DataTable();
                        dt.Load(reader);
                        DataRow row = dt.Rows[0];

                        int lock_db = -1;

                        try
                        {
                            var values = row.ItemArray;
                            lock_db = int.Parse(values[0].ToString());
                        }
                        catch (Exception e)
                        {
                            Logging.WriteToLog("ERROR PARSING!in lock" + e.ToString());
                            throw;
                        }                 
                        
                        mycommand.CommandText = @"UPDATE USERS
                                                SET lock = @lock
                                                WHERE uid = @uid
                                                ;";
                        mycommand.Prepare();
                        mycommand.Parameters.AddWithValue("@uid", currentUser.uid);
                        
                        switch (lock_c.lockType)
                        {
                            case (byte)proto_server.CmdType.LockAcquire:
                                if (lock_db == 1)
                                {
                                    lock_response.succesfull = false;
                                }
                                else
                                {
                                    //query acquire lock
                                    mycommand.Parameters.AddWithValue("@lock", 1);
                                    lock_response.succesfull = true;
                                }
                                break;

                            case (byte)proto_server.CmdType.LockRelease:
                                if (lock_db == 0)
                                {
                                    lock_response.succesfull = false;
                                }
                                else
                                {
                                    //release lock query
                                    mycommand.Parameters.AddWithValue("@lock", 0);
                                    lock_response.succesfull = true;
                                }
                                break;
                        }

                        if (lock_response.succesfull == true)
                        {
                            int nUpdated = mycommand.ExecuteNonQuery();
                            if (nUpdated != 1)
                            {
                                transaction.Rollback();
                                lock_response.succesfull = false;
                            }
                        }
                        //END TRANSACTION
                        transaction.Commit();

                    }
                }
                catch (Exception e)
                {
                    Logging.WriteToLog(e.ToString());
                }
                return lock_response;
            }
        }
        


        //UPDATE

        //Molto simile ad add
        //cerco fid -> max(rev)
        //Crea nuova entry in HISTORY con rev+1
        //Update entry in snapshot con uid,fid   syncid= newsyncid AND rev = newrev
        //NEW Entry in filedump
        public static proto_server.UpdateOk Update(ref proto_server.Update update, ref proto_server.login_c currentUser)
        {
            //add.filename;add.folder;add.fileDump; uid;
            proto_server.UpdateOk updateOk = new proto_server.UpdateOk();
            updateOk.fid = -1;
            updateOk.rev = -1;

            string md5 = proto_server.CalculateMD5Hash(update.fileDump);

            SQLiteConnection cnn = new SQLiteConnection(dbConnection);
            cnn.Open();
            using (SQLiteCommand mycommand = new SQLiteCommand(cnn))
            {
                try
                {
                    //BEGIN TRANSACTION
                    using (var transaction = cnn.BeginTransaction())
                    {
                        //GET HISTORY INFORMATION
                        mycommand.CommandText = @"SELECT HISTORY.fid,MAX( HISTORY.rev) AS maxrev,HISTORY.filename, HISTORY.folder, datetime(HISTORY.timestamp, 'localtime') as timestamp, dir , folder_id
                                                FROM HISTORY
                                                WHERE HISTORY.fid=@fid 
                                                AND HISTORY.uid=@uid 
                                                ;";
                        mycommand.Prepare();
                        mycommand.Parameters.AddWithValue("@uid", currentUser.uid);
                        mycommand.Parameters.AddWithValue("@fid", update.fid);


                        SQLiteDataReader reader = mycommand.ExecuteReader();
                        DataTable dt = new DataTable();
                        dt.Load(reader);

                        DataRow row = dt.Rows[0];

                        int maxrev; string filename; string folder; DateTime timestamp;
                        bool dir;
                        int folder_id;
                        try
                        {
                            var values = row.ItemArray;

                            maxrev = int.Parse(values[1].ToString());
                            filename = values[2].ToString();
                            folder = values[3].ToString();
                            timestamp = DateTime.Parse(values[4].ToString());
                            dir = bool.Parse(values[5].ToString());
                            folder_id = int.Parse(values[6].ToString());
                        }
                        catch (Exception e)
                        {
                            Logging.WriteToLog("ERROR PARSING!in Delete method" + e.ToString());
                            throw;
                        }
                        int rev = maxrev + 1;

                        //INSERT IN HISTORY

                        mycommand.CommandText = @"INSERT INTO HISTORY(uid, fid, rev, filename, folder, timestamp, md5 ,deleted, synchsessionid,dir,folder_id)
                                                VALUES(@uid, @fid, @rev, @filename, @folder ,DATETIME('NOW'),@md5,@deleted,@synchsessionid,@dir,@folder_id)
                                                ; ";
                        mycommand.Prepare();
                        mycommand.Parameters.AddWithValue("@uid", currentUser.uid);
                        mycommand.Parameters.AddWithValue("@fid", update.fid);
                        mycommand.Parameters.AddWithValue("@rev", rev);
                        mycommand.Parameters.AddWithValue("@filename", filename);
                        mycommand.Parameters.AddWithValue("@folder", folder);
                        mycommand.Parameters.AddWithValue("@md5", md5);
                        mycommand.Parameters.AddWithValue("@deleted", false);
                        mycommand.Parameters.AddWithValue("@synchsessionid", currentUser.synchsessionid);
                        mycommand.Parameters.AddWithValue("@dir", dir);
                        mycommand.Parameters.AddWithValue("@folder_id", folder_id);

                        int nUpdated = mycommand.ExecuteNonQuery();
                        if (nUpdated != 1)
                            throw new Exception("No Row updated! Rollback");

                        //UPDATE SNAPSHOT

                        mycommand.CommandText = @"SELECT MAX(syncid)
                                                FROM SNAPSHOT
                                                WHERE SNAPSHOT.uid = @uid
                                                ;";
                        mycommand.Prepare();
                        mycommand.Parameters.AddWithValue("@uid", currentUser.uid);

                        object value = mycommand.ExecuteScalar();
                        int maxsynchid;
                        try
                        {
                            maxsynchid = int.Parse(value.ToString());
                        }
                        catch (Exception e)
                        {
                            Logging.WriteToLog("EXception parsing synchid" + e.ToString());
                            throw;
                        }
                        int synchid = maxsynchid + 1;

                        //UPDATE SNAPSHOT
                        mycommand.CommandText = @"UPDATE SNAPSHOT
                                                SET rev = @rev , syncid = @synchid
                                                WHERE uid = @uid
                                                AND fid = @fid
                                                ;";
                        mycommand.Prepare();
                        mycommand.Parameters.AddWithValue("@uid", currentUser.uid);
                        mycommand.Parameters.AddWithValue("@fid", update.fid);
                        mycommand.Parameters.AddWithValue("@rev", rev);
                        mycommand.Parameters.AddWithValue("@synchid", synchid);

                        
                        nUpdated = mycommand.ExecuteNonQuery();
                        if (nUpdated != 1)
                            throw new Exception("No Row updated! Rollback");

                        //UPDATE SYNCHSESSION
                        //update # updated
                        mycommand.CommandText = @"UPDATE SYNCH_SESSION
                                                SET n_updated = n_updated +1
                                                WHERE uid = @uid
                                                AND synchsessionid = @synchsessionid
                                                ;";
                        mycommand.Prepare();
                        mycommand.Parameters.AddWithValue("@uid", currentUser.uid);
                        mycommand.Parameters.AddWithValue("@syncsessionid", currentUser.synchsessionid);

                        nUpdated = mycommand.ExecuteNonQuery();
                        if (nUpdated != 1)
                            throw new Exception("No Row updated! Rollback");

                        //insert FILEDUMP
                        //FILEDUMP INSERT
                        mycommand.CommandText = @"INSERT INTO FILES_DUMP(uid,fid,rev,filedump)
                                            VALUES (@uid,@fid,@rev,@filedump)
                                            ;";
                        mycommand.Prepare();
                        mycommand.Parameters.AddWithValue("@uid", currentUser.uid);
                        mycommand.Parameters.AddWithValue("@fid", update.fid);
                        mycommand.Parameters.AddWithValue("@rev", rev);
                        mycommand.Parameters.AddWithValue("@filedump", update.fileDump);

                        nUpdated = mycommand.ExecuteNonQuery();
                        if (nUpdated != 1)
                            throw new Exception("No Row updated! Rollback");


                        updateOk.fid = update.fid;
                        updateOk.rev = rev;

                        //END TRANSACTION
                        transaction.Commit();
                    }
                }
                catch (Exception e)
                {
                    Logging.WriteToLog(e.ToString());
                }
            }
            return updateOk;
        }


        public static int GetSynchId(int uid) {
            int synchid = -1;
            SQLiteConnection cnn = new SQLiteConnection(dbConnection);
            cnn.Open();
            using (SQLiteCommand mycommand = new SQLiteCommand(cnn))
            {
                //GET MAX SYNCHID for my user
                mycommand.CommandText = @"SELECT MAX(syncid)
                                                FROM SNAPSHOT
                                                WHERE SNAPSHOT.uid = @uid
                                                ;";
                mycommand.Prepare();
                mycommand.Parameters.AddWithValue("@uid", uid);

                object value = mycommand.ExecuteScalar();
                
                try
                {
                    synchid = int.Parse(value.ToString());
                }
                catch (Exception e)
                {
                    Logging.WriteToLog("EXception parsing synchid" + e.ToString());
                    return synchid;
                    throw;
                }
            }

            return synchid;
        }


        //DELETE

        public static proto_server.DeleteOk Delete(ref proto_server.Delete delete, ref proto_server.login_c currentUser)
        {
            //add.filename;add.folder;add.fileDump; uid;
            proto_server.DeleteOk deleteOk = new proto_server.DeleteOk();
            deleteOk.fid = -1;
            
            SQLiteConnection cnn = new SQLiteConnection(dbConnection);
            cnn.Open();
            using (SQLiteCommand mycommand = new SQLiteCommand(cnn))
            {
                try
                {
                    //BEGIN TRANSACTION
                    using (var transaction = cnn.BeginTransaction())
                    {
                        //GET HISTORY INFORMATION
                        mycommand.CommandText = @"SELECT HISTORY.fid,MAX( HISTORY.rev) AS maxrev,HISTORY.filename, HISTORY.folder, datetime(HISTORY.timestamp, 'localtime') as timestamp, dir, folder_id
                                                FROM HISTORY
                                                WHERE HISTORY.fid=@fid 
                                                AND HISTORY.uid=@uid 
                                                ;";
                        mycommand.Prepare();
                        mycommand.Parameters.AddWithValue("@uid", currentUser.uid);
                        mycommand.Parameters.AddWithValue("@fid", delete.fid);


                        SQLiteDataReader reader = mycommand.ExecuteReader();
                        DataTable dt = new DataTable();
                        dt.Load(reader);

                        DataRow row = dt.Rows[0];

                        int maxrev; string filename; string folder; DateTime timestamp;
                        bool dir;
                        int folder_id;
                        
                        try
                        {
                            var values = row.ItemArray;
                            
                            maxrev = int.Parse(values[1].ToString());
                            filename = values[2].ToString();
                            folder = values[3].ToString();
                            timestamp = DateTime.Parse(values[4].ToString());
                            dir = bool.Parse(values[5].ToString());
                            folder_id = int.Parse(values[6].ToString());
                        }
                        catch (Exception e)
                        {
                            Logging.WriteToLog("ERROR PARSING!in Delete method"+e.ToString());
                            throw;
                        }
                        int rev = maxrev + 1;

                        //INSERT IN HISTORY

                        mycommand.CommandText = @"INSERT INTO HISTORY(uid, fid, rev, filename, folder, timestamp, deleted, synchsessionid,dir,folder_id)
                                                VALUES(@uid, @fid, @rev, @filename, @folder ,DATETIME('NOW'),@deleted,@synchsessionid,@dir,@folder_id)
                                                ; ";
                        mycommand.Prepare();
                        mycommand.Parameters.AddWithValue("@uid", currentUser.uid);
                        mycommand.Parameters.AddWithValue("@fid", delete.fid);
                        mycommand.Parameters.AddWithValue("@rev", rev);
                        mycommand.Parameters.AddWithValue("@filename", filename);
                        mycommand.Parameters.AddWithValue("@folder", folder);
                        mycommand.Parameters.AddWithValue("@deleted", true);
                        mycommand.Parameters.AddWithValue("@synchsessionid", currentUser.synchsessionid);
                        mycommand.Parameters.AddWithValue("@dir", dir);
                        mycommand.Parameters.AddWithValue("@folder_id", folder_id);

                        int nUpdated = mycommand.ExecuteNonQuery();
                        if (nUpdated != 1)
                            throw new Exception("No Row updated! Rollback");


                        //UPDATE SNAPSHOT

                        mycommand.CommandText = @"SELECT MAX(syncid)
                                                FROM SNAPSHOT
                                                WHERE SNAPSHOT.uid = @uid
                                                ;";
                        mycommand.Prepare();
                        mycommand.Parameters.AddWithValue("@uid", currentUser.uid);

                        object value = mycommand.ExecuteScalar();
                        int maxsynchid;
                        try
                        {
                            maxsynchid = int.Parse(value.ToString());
                        }
                        catch (Exception e)
                        {
                            Logging.WriteToLog("EXception parsing synchid" + e.ToString());
                            throw;                           
                        }
                        int synchid = maxsynchid + 1;

                        //UPDATE SNAPSHOT
                        mycommand.CommandText = @"UPDATE SNAPSHOT
                                                SET rev = @rev , syncid = @synchid
                                                WHERE uid = @uid
                                                AND fid = @fid
                                                ;";
                        mycommand.Prepare();
                        mycommand.Parameters.AddWithValue("@uid", currentUser.uid);
                        mycommand.Parameters.AddWithValue("@fid", delete.fid);
                        mycommand.Parameters.AddWithValue("@rev", rev);
                        mycommand.Parameters.AddWithValue("@synchid", synchid);

                        nUpdated = mycommand.ExecuteNonQuery();
                        if (nUpdated != 1)
                            throw new Exception("No Row updated! Rollback");

                        //UPDATE SYNCHSESSION
                        //update # deleted
                        mycommand.CommandText = @"UPDATE SYNCH_SESSION
                                                SET n_deleted = n_deleted +1
                                                WHERE uid = @uid
                                                AND synchsessionid = @synchsessionid
                                                ;";
                        mycommand.Prepare();
                        mycommand.Parameters.AddWithValue("@uid", currentUser.uid);
                        mycommand.Parameters.AddWithValue("@syncsessionid", currentUser.synchsessionid);

                        nUpdated = mycommand.ExecuteNonQuery();
                        if (nUpdated != 1)
                            throw new Exception("No Row updated! Rollback");

                        deleteOk.fid = delete.fid;

                        //END TRANSACTION
                        transaction.Commit();
                    }
                }
                catch (Exception e)
                {
                    Logging.WriteToLog(e.ToString());
                }
            }
            return deleteOk;
        }

        public static proto_server.DeleteOk DeleteFolder(ref proto_server.Delete delete, ref proto_server.login_c currentUser)
        {
            //add.filename;add.folder;add.fileDump; uid;
            proto_server.DeleteOk deleteOk = new proto_server.DeleteOk();
            deleteOk.fid = -1;
            
            SQLiteConnection cnn = new SQLiteConnection(dbConnection);
            cnn.Open();
            SQLiteCommand mycommand = new SQLiteCommand(cnn);
            try
            {
                //BEGIN TRANSACTION
                var transaction = cnn.BeginTransaction();
                //GET HISTORY INFORMATION
                mycommand.CommandText = @"SELECT HISTORY.fid,MAX( HISTORY.rev) AS maxrev,HISTORY.filename, HISTORY.folder, datetime(HISTORY.timestamp, 'localtime') as timestamp, dir, folder_id
                                        FROM HISTORY
                                        WHERE HISTORY.fid=@fid 
                                        AND HISTORY.uid=@uid 
                                        ;";
                mycommand.Prepare();
                mycommand.Parameters.AddWithValue("@uid", currentUser.uid);
                mycommand.Parameters.AddWithValue("@fid", delete.fid);


                SQLiteDataReader reader = mycommand.ExecuteReader();
                DataTable dt = new DataTable();
                dt.Load(reader);

                DataRow row = dt.Rows[0];

                int maxrev; string filename; string folder; DateTime timestamp;
                bool dir;
                int folder_id;
                var values = row.ItemArray;
                try
                {
                            
                    maxrev = int.Parse(values[1].ToString());
                    filename = values[2].ToString();
                    folder = values[3].ToString();
                    timestamp = DateTime.Parse(values[4].ToString());
                    dir = bool.Parse(values[5].ToString());
                    folder_id = int.Parse(values[6].ToString());
                }
                catch (Exception e)
                {
                    Logging.WriteToLog("ERROR PARSING!in Delete method"+e.ToString());
                    throw;
                }
                int rev = maxrev + 1;

                SyncBox_Server.db.DeleteFolder(ref delete, ref currentUser, values, mycommand);

                deleteOk.fid = delete.fid;

                //END TRANSACTION
                transaction.Commit();
            }
            catch (Exception e)
            {
                Logging.WriteToLog(e.ToString());
            }
            return deleteOk;
        }

        public static void DeleteFolder(ref proto_server.Delete delete, ref proto_server.login_c currentUser, object[] valuesParent, SQLiteCommand mycommand)
        {
            try
            {
                mycommand.CommandText = @"SELECT HISTORY.fid,MAX( HISTORY.rev) AS maxrev,HISTORY.filename, HISTORY.folder, datetime(HISTORY.timestamp, 'localtime') as timestamp, dir, folder_id
                                        FROM HISTORY
                                        WHERE HISTORY.uid=@uid
                                        AND HISTORY.deleted = 0
                                        GROUP BY HISTORY.fid
                                        ;";
                mycommand.Prepare();
                mycommand.Parameters.AddWithValue("@uid", currentUser.uid);

                SQLiteDataReader reader = mycommand.ExecuteReader();
                DataTable dt = new DataTable();
                dt.Load(reader);

                List<proto_server.FileListItem> fileList = new List<proto_server.FileListItem>();
                var currentValues = dt.Rows[0].ItemArray;
                foreach (DataRow row in dt.Rows)
                {
                    currentValues = row.ItemArray;
                    try
                    {
                        var fileListItem = new proto_server.FileListItem();
                        fileListItem.fid = int.Parse(currentValues[0].ToString());
                        fileListItem.rev = int.Parse(currentValues[1].ToString());
                        fileListItem.filename = currentValues[2].ToString();
                        fileListItem.folder = currentValues[3].ToString();
                        fileListItem.timestamp = DateTime.Parse(currentValues[4].ToString());
                        fileListItem.md5 = null;
                        fileListItem.dir = bool.Parse(currentValues[5].ToString());
                        fileListItem.folder_id = int.Parse(currentValues[6].ToString());
                        fileList.Add(fileListItem);
                    }
                    catch (Exception e)
                    {
                        Logging.WriteToLog("NON riesco a fare il parsing di un campo da db" + e.ToString());
                        throw;
                    }
                }

                foreach (proto_server.FileListItem item in fileList)
                {
                    if (item.folder_id == int.Parse(valuesParent[0].ToString()))
                    {
                        if (item.dir)
                        {
                            SyncBox_Server.db.DeleteFolder(ref delete, ref currentUser, currentValues, mycommand);
                        }
                        else
                        {
                            //GET HISTORY INFORMATION
                            mycommand.CommandText = @"SELECT HISTORY.fid,MAX( HISTORY.rev) AS maxrev,HISTORY.filename, HISTORY.folder, datetime(HISTORY.timestamp, 'localtime') as timestamp, dir, folder_id
                                            FROM HISTORY
                                            WHERE HISTORY.fid=@fid 
                                            AND HISTORY.uid=@uid 
                                            ;";
                            mycommand.Prepare();
                            mycommand.Parameters.AddWithValue("@uid", currentUser.uid);
                            mycommand.Parameters.AddWithValue("@fid", item.fid);


                            reader = mycommand.ExecuteReader();
                            dt = new DataTable();
                            dt.Load(reader);

                            DataRow row = dt.Rows[0];

                            int maxrev; string filename; string folder; DateTime timestamp;
                            bool dir;
                            int folder_id;
                            var values = row.ItemArray;
                            try
                            {
                                maxrev = int.Parse(values[1].ToString());
                                filename = values[2].ToString();
                                folder = values[3].ToString();
                                timestamp = DateTime.Parse(values[4].ToString());
                                dir = bool.Parse(values[5].ToString());
                                folder_id = int.Parse(values[6].ToString());
                            }
                            catch (Exception e)
                            {
                                Logging.WriteToLog("ERROR PARSING!in Delete method" + e.ToString());
                                throw;
                            }
                            int rev = maxrev + 1;

                            //INSERT IN HISTORY

                            mycommand.CommandText = @"INSERT INTO HISTORY(uid, fid, rev, filename, folder, timestamp, deleted, synchsessionid,dir,folder_id)
                                            VALUES(@uid, @fid, @rev, @filename, @folder ,DATETIME('NOW'),@deleted,@synchsessionid,@dir,@folder_id)
                                            ; ";
                            mycommand.Prepare();
                            mycommand.Parameters.AddWithValue("@uid", currentUser.uid);
                            mycommand.Parameters.AddWithValue("@fid", item.fid);
                            mycommand.Parameters.AddWithValue("@rev", rev);
                            mycommand.Parameters.AddWithValue("@filename", filename);
                            mycommand.Parameters.AddWithValue("@folder", folder);
                            mycommand.Parameters.AddWithValue("@deleted", true);
                            mycommand.Parameters.AddWithValue("@synchsessionid", currentUser.synchsessionid);
                            mycommand.Parameters.AddWithValue("@dir", dir);
                            mycommand.Parameters.AddWithValue("@folder_id", folder_id);

                            int nUpdated = mycommand.ExecuteNonQuery();
                            if (nUpdated != 1)
                                throw new Exception("No Row updated! Rollback");


                            //UPDATE SNAPSHOT

                            mycommand.CommandText = @"SELECT MAX(syncid)
                                            FROM SNAPSHOT
                                            WHERE SNAPSHOT.uid = @uid
                                            ;";
                            mycommand.Prepare();
                            mycommand.Parameters.AddWithValue("@uid", currentUser.uid);

                            object value = mycommand.ExecuteScalar();
                            int maxsynchid;
                            try
                            {
                                maxsynchid = int.Parse(value.ToString());
                            }
                            catch (Exception e)
                            {
                                Logging.WriteToLog("EXception parsing synchid" + e.ToString());
                                throw;
                            }
                            int synchid = maxsynchid + 1;

                            //UPDATE SNAPSHOT
                            mycommand.CommandText = @"UPDATE SNAPSHOT
                                            SET rev = @rev , syncid = @synchid
                                            WHERE uid = @uid
                                            AND fid = @fid
                                            ;";
                            mycommand.Prepare();
                            mycommand.Parameters.AddWithValue("@uid", currentUser.uid);
                            mycommand.Parameters.AddWithValue("@fid", item.fid);
                            mycommand.Parameters.AddWithValue("@rev", rev);
                            mycommand.Parameters.AddWithValue("@synchid", synchid);

                            nUpdated = mycommand.ExecuteNonQuery();
                            if (nUpdated != 1)
                                throw new Exception("No Row updated! Rollback");

                            //UPDATE SYNCHSESSION
                            //update # deleted
                            mycommand.CommandText = @"UPDATE SYNCH_SESSION
                                            SET n_deleted = n_deleted +1
                                            WHERE uid = @uid
                                            AND synchsessionid = @synchsessionid
                                            ;";
                            mycommand.Prepare();
                            mycommand.Parameters.AddWithValue("@uid", currentUser.uid);
                            mycommand.Parameters.AddWithValue("@syncsessionid", currentUser.synchsessionid);

                            nUpdated = mycommand.ExecuteNonQuery();
                            if (nUpdated != 1)
                                throw new Exception("No Row updated! Rollback");
                        }
                    }
                }
                //INSERT IN HISTORY

                mycommand.CommandText = @"INSERT INTO HISTORY(uid, fid, rev, filename, folder, timestamp, deleted, synchsessionid,dir,folder_id)
                                                VALUES(@uid, @fid, @rev, @filename, @folder ,DATETIME('NOW'),@deleted,@synchsessionid,@dir,@folder_id)
                                                ; ";
                mycommand.Prepare();
                mycommand.Parameters.AddWithValue("@uid", currentUser.uid);
                mycommand.Parameters.AddWithValue("@fid", int.Parse(valuesParent[0].ToString()));
                mycommand.Parameters.AddWithValue("@rev", int.Parse(valuesParent[1].ToString())+1);
                mycommand.Parameters.AddWithValue("@filename", valuesParent[2].ToString());
                mycommand.Parameters.AddWithValue("@folder", valuesParent[3].ToString());
                mycommand.Parameters.AddWithValue("@deleted", true);
                mycommand.Parameters.AddWithValue("@synchsessionid", currentUser.synchsessionid);
                mycommand.Parameters.AddWithValue("@dir", true);
                mycommand.Parameters.AddWithValue("@folder_id", int.Parse(valuesParent[6].ToString()));

                int nUpdated2 = mycommand.ExecuteNonQuery();
                if (nUpdated2 != 1)
                    throw new Exception("No Row updated! Rollback");


                //UPDATE SNAPSHOT

                mycommand.CommandText = @"SELECT MAX(syncid)
                                                FROM SNAPSHOT
                                                WHERE SNAPSHOT.uid = @uid
                                                ;";
                mycommand.Prepare();
                mycommand.Parameters.AddWithValue("@uid", currentUser.uid);

                object value2 = mycommand.ExecuteScalar();
                int maxsynchid2;
                try
                {
                    maxsynchid2 = int.Parse(value2.ToString());
                }
                catch (Exception e)
                {
                    Logging.WriteToLog("EXception parsing synchid" + e.ToString());
                    throw;
                }
                int synchid2 = maxsynchid2 + 1;

                //UPDATE SNAPSHOT
                mycommand.CommandText = @"UPDATE SNAPSHOT
                                                SET rev = @rev , syncid = @synchid
                                                WHERE uid = @uid
                                                AND fid = @fid
                                                ;";
                mycommand.Prepare();
                mycommand.Parameters.AddWithValue("@uid", currentUser.uid);
                mycommand.Parameters.AddWithValue("@fid", int.Parse(valuesParent[0].ToString()));
                mycommand.Parameters.AddWithValue("@rev", int.Parse(valuesParent[1].ToString())+1);
                mycommand.Parameters.AddWithValue("@synchid", synchid2);

                nUpdated2 = mycommand.ExecuteNonQuery();
                if (nUpdated2 != 1)
                    throw new Exception("No Row updated! Rollback");

                //UPDATE SYNCHSESSION
                //update # deleted
                mycommand.CommandText = @"UPDATE SYNCH_SESSION
                                                SET n_deleted = n_deleted +1
                                                WHERE uid = @uid
                                                AND synchsessionid = @synchsessionid
                                                ;";
                mycommand.Prepare();
                mycommand.Parameters.AddWithValue("@uid", currentUser.uid);
                mycommand.Parameters.AddWithValue("@syncsessionid", currentUser.synchsessionid);

                nUpdated2 = mycommand.ExecuteNonQuery();
                if (nUpdated2 != 1)
                    throw new Exception("No Row updated! Rollback");

            }
            catch (Exception e)
            {
                Logging.WriteToLog(e.ToString());
            }
        }


        //TODO Complete!
        public static proto_server.GetResponse GetResponse(int fid, int rev, int uid)
        {
            try
            {
                SQLiteConnection cnn = new SQLiteConnection(dbConnection);
                cnn.Open();
                using (SQLiteCommand mycommand = new SQLiteCommand(cnn))
                {

                    proto_server.GetResponse getResponse;// = new proto_server.GetResponse();
                    //check if folder
                    mycommand.CommandText = @"SELECT HISTORY.fid, HISTORY.rev,HISTORY.filename, HISTORY.folder, datetime(HISTORY.timestamp, 'localtime') as timestamp ,HISTORY.deleted, dir, folder_id
                                            FROM HISTORY
                                            WHERE HISTORY.fid = @fid
                                            AND HISTORY.rev = @rev
                                            AND HISTORY.uid = @uid
                                            ;";
                    mycommand.Prepare();
                    mycommand.Parameters.AddWithValue("@uid", uid);
                    mycommand.Parameters.AddWithValue("@fid", fid);
                    mycommand.Parameters.AddWithValue("@rev", rev);

                    SQLiteDataReader reader = mycommand.ExecuteReader();
                    DataTable dt = new DataTable();
                    dt.Load(reader);

                    if (dt.Rows.Count != 1)
                        throw new Exception("Get file/folder not existing. row count-> " + dt.Rows.Count);

                    foreach (DataRow row in dt.Rows)
                    {
                        var values = row.ItemArray;

                        getResponse = new proto_server.GetResponse()
                        {
                            fileInfo = new proto_server.FileToGet()
                            {
                                fid = fid,
                                rev = rev,
                                filename = values[2].ToString(),
                                folder = values[3].ToString(),
                                timestamp = DateTime.Parse(values[4].ToString()),

                                deleted = Boolean.Parse(values[5].ToString()),
                                dir = bool.Parse(values[6].ToString()),
                                folder_id = int.Parse(values[7].ToString())
                            },

                        };
                        //IS A DIR
                        if (getResponse.fileInfo.dir)
                            return getResponse;
                    }

                    //IS A FILE
           
                 mycommand.CommandText = @"SELECT HISTORY.fid, HISTORY.rev,HISTORY.filename, HISTORY.folder, datetime(HISTORY.timestamp, 'localtime') as timestamp ,HISTORY.md5,HISTORY.deleted, FILES_DUMP.filedump, dir, folder_id
                                            FROM FILES_DUMP,HISTORY
                                            WHERE FILES_DUMP.uid = @uid
                                            AND FILES_DUMP.fid = @fid
                                            AND FILES_DUMP.rev = @rev
                                            AND FILES_DUMP.uid = HISTORY.uid
                                            AND FILES_DUMP.fid = HISTORY.fid
                                            AND FILES_DUMP.rev = HISTORY.rev
                                            ;";
                    mycommand.Prepare();
                    mycommand.Parameters.AddWithValue("@uid", uid);
                    mycommand.Parameters.AddWithValue("@fid", fid);
                    mycommand.Parameters.AddWithValue("@rev", rev);

                    reader = mycommand.ExecuteReader();
                    dt = new DataTable();
                    dt.Load(reader);
                    reader.Close();
                    cnn.Close();

                    if (dt.Rows.Count != 1)
                        throw new Exception("I expected 1 file in FILES_DUMP -> obtain " + dt.Rows.Count);

                    

                    foreach (DataRow row in dt.Rows)
                    {
                        var values = row.ItemArray;

                        getResponse = new proto_server.GetResponse()
                        {
                            fileInfo = new proto_server.FileToGet()
                            {
                                fid = fid,
                                rev = rev,
                                filename = values[2].ToString(),
                                folder = values[3].ToString(),
                                timestamp = DateTime.Parse(values[4].ToString()),
                                md5 = values[5].ToString(),
                                deleted = Boolean.Parse(values[6].ToString()),
                                dir = bool.Parse(values[8].ToString()),
                                folder_id = int.Parse(values[9].ToString())
                            },
                            fileDump = (byte[])row["filedump"]
                        };

                        return getResponse;
                    }
                    Logging.WriteToLog("PANIC! No file found");
                    throw new Exception("panic no reach code!");
                }
            }
            catch (Exception e)
            {
                Logging.WriteToLog(e.ToString());
                proto_server.GetResponse getResponse = new proto_server.GetResponse();

                proto_server.FileToGet ftg = new proto_server.FileToGet();
                getResponse.fileInfo = ftg;
                getResponse.fileInfo.fid = 0;
                getResponse.fileInfo.rev = 0;

                return getResponse;
            }
        }

        public static proto_server.ListResponse ListResponseLast(int uid)
        {
            try
            {
                SQLiteConnection cnn = new SQLiteConnection(dbConnection);
                cnn.Open();
                using (SQLiteCommand mycommand = new SQLiteCommand(cnn))
                {
                    mycommand.CommandText = @"SELECT HISTORY.fid, HISTORY.rev,HISTORY.filename, HISTORY.folder, datetime(HISTORY.timestamp, 'localtime') as timestamp ,HISTORY.md5,HISTORY.deleted,dir,folder_id
                                            FROM HISTORY, SNAPSHOT
                                            WHERE HISTORY.fid=SNAPSHOT.fid and HISTORY.uid=SNAPSHOT.uid and HISTORY.rev=SNAPSHOT.rev
                                            AND HISTORY.uid = @uid                                            ;";
                    mycommand.Prepare();
                    mycommand.Parameters.AddWithValue("@uid", uid);

                    SQLiteDataReader reader = mycommand.ExecuteReader();
                    DataTable dt = new DataTable();
                    dt.Load(reader);
                    reader.Close();
                    cnn.Close();

                    proto_server.ListResponse listResponse = new proto_server.ListResponse();
                    listResponse.fileList = new List<proto_server.FileListItem>(dt.Rows.Count);
                    foreach (DataRow row in dt.Rows)
                    {
                        try
                        {
                            var values = row.ItemArray;
                            var fileListItem = new proto_server.FileListItem()
                            {
                                fid = int.Parse(values[0].ToString()),
                                rev = int.Parse(values[1].ToString()),
                                filename = values[2].ToString(),
                                folder = values[3].ToString(),
                                timestamp = DateTime.Parse(values[4].ToString()),
                                md5 = values[5].ToString(),
                                deleted = Boolean.Parse(values[6].ToString()),
                                dir = bool.Parse(values[7].ToString()),
                                folder_id = int.Parse(values[8].ToString())
                            };
                            listResponse.fileList.Add(fileListItem);
                        }
                        catch (Exception e)
                        {
                            Logging.WriteToLog("NON riesco a fare il parsing di un campo da db" + e.ToString());
                            throw;
                        }
                    }
                    return listResponse;
                }
            }
            catch
            {
                throw;
            }
            return null;
        }


        public static proto_server.ListResponse ListResponseAll(int uid)
        {
            try
            {
                SQLiteConnection cnn = new SQLiteConnection(dbConnection);
                cnn.Open();
                using (SQLiteCommand mycommand = new SQLiteCommand(cnn))
                {
                    mycommand.CommandText = @"SELECT H1.fid, H1.rev, H1.filename, H1.folder, datetime(H1.timestamp, 'localtime') as timestamp ,H1.md5, H1.deleted, H1.dir, H1.folder_id
                                            FROM HISTORY H1
                                            WHERE H1.uid = @uid
                                            ORDER BY H1.folder
                                            ;";
                    mycommand.Prepare();
                    mycommand.Parameters.AddWithValue("@uid", uid);

                    SQLiteDataReader reader = mycommand.ExecuteReader();
                    DataTable dt = new DataTable();
                    dt.Load(reader);
                    reader.Close();
                    cnn.Close();

                    proto_server.ListResponse listResponse = new proto_server.ListResponse();
                    listResponse.fileList = new List<proto_server.FileListItem>(dt.Rows.Count);
                    foreach (DataRow row in dt.Rows)
                    {
                        try
                        {
                            var values = row.ItemArray;
                            var fileListItem = new proto_server.FileListItem()
                            {
                                fid = int.Parse(values[0].ToString()),
                                rev = int.Parse(values[1].ToString()),
                                filename = values[2].ToString(),
                                folder = values[3].ToString(),
                                timestamp = DateTime.Parse(values[4].ToString()),
                                md5 = values[5].ToString(),
                                deleted = Boolean.Parse(values[6].ToString()),
                                dir = bool.Parse(values[7].ToString()),
                                folder_id = int.Parse(values[8].ToString())
                            };
                            listResponse.fileList.Add(fileListItem);
                        }
                        catch (Exception e)
                        {
                            Logging.WriteToLog("NON riesco a fare il parsing di un campo da db" + e.ToString());
                            throw;
                        }
                    }
                    return listResponse;
                }
            }
            catch
            {
                throw;
            }
            return null;
        }



        public static void RegisterUser( ref proto_server.login_c currentUser)
        {

            

            SQLiteConnection cnn = new SQLiteConnection(dbConnection);
            cnn.Open();
            using (SQLiteCommand mycommand = new SQLiteCommand(cnn))
            {
                try
                {
                    //BEGIN TRANSACTION
                    using (var transaction = cnn.BeginTransaction())
                    {
 
                        //inserisco in HISTORY SNAPSJOT  e FILE_DUMP
                        //Add vuol dire che non ho da fare update ma solo insert
                        //HISTORY INSERT
                        mycommand.CommandText = @"INSERT INTO HISTORY(uid,fid,rev,filename,folder,timestamp,md5,deleted,synchsessionid,dir,folder_id)
                                            VALUES (@uid,@fid,@rev,@filename,@folder,DATETIME('NOW'),@md5,@deleted,@synchsessionid,@dir,@folder_id)
                                                ; ";
                        mycommand.Prepare();
                        mycommand.Parameters.AddWithValue("@uid", currentUser.uid);
                        mycommand.Parameters.AddWithValue("@fid", 1);
                        mycommand.Parameters.AddWithValue("@rev", 1);

                        mycommand.Parameters.AddWithValue("@folder", "\\");
                        //mycommand.Parameters.AddWithValue("@timestamp", timestamp);

                        mycommand.Parameters.AddWithValue("@deleted", false);
                        mycommand.Parameters.AddWithValue("@synchsessionid", 0);
                        mycommand.Parameters.AddWithValue("@dir", true);


                        mycommand.Parameters.AddWithValue("@filename", "");
                        mycommand.Parameters.AddWithValue("@md5", null);
                        mycommand.Parameters.AddWithValue("@folder_id", 1);
                        
                        int nUpdated = mycommand.ExecuteNonQuery();
                        if (nUpdated != 1)
                            throw new Exception("No Row updated! Rollback");

                        //SNAPSHOT INSERT
                        //HISTORY INSERT
                        mycommand.CommandText = @"INSERT INTO SNAPSHOT(uid,fid,rev,syncid)
                                            VALUES (@uid,@fid,@rev,@syncid)
                                            ;";
                        mycommand.Prepare();
                        mycommand.Parameters.AddWithValue("@uid", currentUser.uid);
                        mycommand.Parameters.AddWithValue("@fid", 1);
                        mycommand.Parameters.AddWithValue("@rev", 1);
                        mycommand.Parameters.AddWithValue("@syncid", 1);

                        nUpdated = mycommand.ExecuteNonQuery();
                        if (nUpdated != 1)
                            throw new Exception("No Row updated! Rollback");
                        
                        //END TRANSACTION
                        transaction.Commit();
                    }
                }
                catch (Exception e)
                {
                    Logging.WriteToLog(e.ToString());
                    return ;
                }
            }
            return ;
        }



    }
}