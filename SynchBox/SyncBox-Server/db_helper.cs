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

    //use lock to execute query??
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

        //TODO Add verifica se il file era presente come cancellato e a questo punto può aggiungerlo!!!
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
                                            ; ";
                        mycommand.Prepare();
                        mycommand.Parameters.AddWithValue("@uid", currentUser.uid);
                        mycommand.Parameters.AddWithValue("@filename", add.filename);
                        mycommand.Parameters.AddWithValue("@folder", add.folder);
                        object value = mycommand.ExecuteScalar();

                        //TODO IMPROVEEEE
                        if (value == null)
                        {
                            throw new Exception("Querydb: count(*) if filename is present in the folder for uid. RETURN NULL. PANIC");
                        }
                        if (value.ToString().CompareTo("0") != 0)
                        {
                            throw new Exception("file already present in db." + add.ToString());
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
                        //int maxsyncId = -1;
                        //if (value == null)
                        //{
                        //    maxsyncId = 0;
                        //}
                        //else {
                        //    maxsyncId = int.Parse(value.ToString());
                        //}
                        //syncId
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



                        //inserisco in HISTORY SNAPSJOT  e FILE_DUMP
                        //Add vuol dire che non ho da fare update ma solo insert
                        //HISTORY INSERT
                        mycommand.CommandText = @"INSERT INTO HISTORY(uid,fid,rev,filename,folder,timestamp,md5,deleted,synchsessionid)
                                            VALUES (@uid,@fid,@rev,@filename,@folder,DATETIME('NOW'),@md5,@deleted,@synchsessionid)
                                                ; ";
                        mycommand.Prepare();
                        mycommand.Parameters.AddWithValue("@uid", currentUser.uid);
                        mycommand.Parameters.AddWithValue("@fid", fid);
                        mycommand.Parameters.AddWithValue("@rev", 1);
                        mycommand.Parameters.AddWithValue("@filename", add.filename);
                        mycommand.Parameters.AddWithValue("@folder", add.folder);
                        //mycommand.Parameters.AddWithValue("@timestamp", timestamp);
                        mycommand.Parameters.AddWithValue("@md5", md5);
                        mycommand.Parameters.AddWithValue("@deleted", false);
                        mycommand.Parameters.AddWithValue("@synchsessionid", currentUser.synchsessionid);

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
                        mycommand.CommandText = @"SELECT HISTORY.fid,MAX( HISTORY.rev) AS maxrev,HISTORY.filename, HISTORY.folder, datetime(HISTORY.timestamp, 'localtime') as timestamp
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

                        try
                        {
                            var values = row.ItemArray;

                            maxrev = int.Parse(values[1].ToString());
                            filename = values[2].ToString();
                            folder = values[3].ToString();
                            timestamp = DateTime.Parse(values[4].ToString());
                        }
                        catch (Exception e)
                        {
                            Logging.WriteToLog("ERROR PARSING!in Delete method" + e.ToString());
                            throw;
                        }
                        int rev = maxrev + 1;

                        //INSERT IN HISTORY

                        mycommand.CommandText = @"INSERT INTO HISTORY(uid, fid, rev, filename, folder, timestamp, md5 ,deleted, synchsessionid)
                                                VALUES(@uid, @fid, @rev, @filename, @folder ,DATETIME('NOW'),@md5,@deleted,@synchsessionid)
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
                                                SET rev = @rev AND syncid = @synchid
                                                WHERE uid = @uid
                                                AND fid = @fid
                                                ;";
                        mycommand.Prepare();
                        mycommand.Parameters.AddWithValue("@uid", currentUser.uid);
                        mycommand.Parameters.AddWithValue("@fid", update.fid);
                        mycommand.Parameters.AddWithValue("@rev", rev);
                        mycommand.Parameters.AddWithValue("@synchid", synchid);

                        int nUpdated = mycommand.ExecuteNonQuery();
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
                    //addOk.fid = -1;
                    //return addOk;
                }
                //manage try catch transaction commit
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
        //???? Un file cancellato può essere restored ? 
        //rm from snapshot NO! Mi serve synchid -> faccio update in snapshot ----> ma quando poi faccio una getlist considero anche quelli deleted!
        //add new revision in HISTORY deleted = true
        //update SYNCHSESSION

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
                        mycommand.CommandText = @"SELECT HISTORY.fid,MAX( HISTORY.rev) AS maxrev,HISTORY.filename, HISTORY.folder, datetime(HISTORY.timestamp, 'localtime') as timestamp
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

                        try
                        {
                            var values = row.ItemArray;
                            
                            maxrev = int.Parse(values[1].ToString());
                            filename = values[2].ToString();
                            folder = values[3].ToString();
                            timestamp = DateTime.Parse(values[4].ToString());
                        }
                        catch (Exception e)
                        {
                            Logging.WriteToLog("ERROR PARSING!in Delete method"+e.ToString());
                            throw;
                        }
                        int rev = maxrev + 1;

                        //INSERT IN HISTORY

                        mycommand.CommandText = @"INSERT INTO HISTORY(uid, fid, rev, filename, folder, timestamp, deleted, synchsessionid)
                                                VALUES(@uid, @fid, @rev, @filename, @folder ,DATETIME('NOW'),@deleted,@synchsessionid)
                                                ; ";
                        mycommand.Prepare();
                        mycommand.Parameters.AddWithValue("@uid", currentUser.uid);
                        mycommand.Parameters.AddWithValue("@fid", delete.fid);
                        mycommand.Parameters.AddWithValue("@rev", rev);
                        mycommand.Parameters.AddWithValue("@filename", filename);
                        mycommand.Parameters.AddWithValue("@folder", folder);
                        mycommand.Parameters.AddWithValue("@deleted", true);
                        mycommand.Parameters.AddWithValue("@synchsessionid", currentUser.synchsessionid);
                        
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
                                                SET rev = @rev AND syncid = @synchid
                                                WHERE uid = @uid
                                                AND fid = @fid
                                                ;";
                        mycommand.Prepare();
                        mycommand.Parameters.AddWithValue("@uid", currentUser.uid);
                        mycommand.Parameters.AddWithValue("@fid", delete.fid);
                        mycommand.Parameters.AddWithValue("@rev", rev);
                        mycommand.Parameters.AddWithValue("@synchid", synchid);

                        int nUpdated = mycommand.ExecuteNonQuery();
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
                    //addOk.fid = -1;
                    //return addOk;
                }
                //manage try catch transaction commit
            }
            return deleteOk;
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
                    mycommand.CommandText = @"SELECT HISTORY.fid, HISTORY.rev,HISTORY.filename, HISTORY.folder, datetime(HISTORY.timestamp, 'localtime') as timestamp ,HISTORY.md5,HISTORY.deleted, FILES_DUMP.filedump
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

                    SQLiteDataReader reader = mycommand.ExecuteReader();
                    DataTable dt = new DataTable();
                    dt.Load(reader);
                    reader.Close();
                    cnn.Close();

                    if (dt.Rows.Count != 1)
                        throw new Exception("I expected 1 file in FILES_DUMP -> obtain " + dt.Rows.Count);

                    proto_server.GetResponse getResponse;// = new proto_server.GetResponse();

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
                                deleted = Boolean.Parse(values[6].ToString())
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
                throw;
                //return null;
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
                    mycommand.CommandText = @"SELECT HISTORY.fid, HISTORY.rev,HISTORY.filename, HISTORY.folder, datetime(HISTORY.timestamp, 'localtime') as timestamp ,HISTORY.md5,HISTORY.deleted
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
                                deleted = Boolean.Parse(values[6].ToString())
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
        //method ended

        public static proto_server.ListResponse ListResponseAll(int uid)
        {
            try
            {
                SQLiteConnection cnn = new SQLiteConnection(dbConnection);
                cnn.Open();
                using (SQLiteCommand mycommand = new SQLiteCommand(cnn))
                {
                    mycommand.CommandText = @"SELECT HISTORY.fid, HISTORY.rev,HISTORY.filename, HISTORY.folder, datetime(HISTORY.timestamp, 'localtime') as timestamp ,HISTORY.md5,HISTORY.deleted
                                            FROM HISTORY
                                            WHERE HISTORY.uid = @uid                                            ;";
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
                                //TODO Capire come gestire un eccezione di parsing... ha senso farla in modo diverso?
                                fid = int.Parse(values[0].ToString()),
                                rev = int.Parse(values[1].ToString()),
                                filename = values[2].ToString(),
                                folder = values[3].ToString(),
                                timestamp = DateTime.Parse(values[4].ToString()),
                                md5 = values[5].ToString(),
                                deleted = Boolean.Parse(values[6].ToString())
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
        //method ended


    }
}