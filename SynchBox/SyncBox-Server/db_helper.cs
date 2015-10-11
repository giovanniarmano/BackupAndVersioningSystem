﻿using System;
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
        public static proto_server.AddOk Add(ref proto_server.Add add, int uid)
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
                        mycommand.Parameters.AddWithValue("@uid", uid);
                        mycommand.Parameters.AddWithValue("@filename", add.filename);
                        mycommand.Parameters.AddWithValue("@folder", add.folder);
                        object value = mycommand.ExecuteScalar();

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
                        mycommand.Parameters.AddWithValue("@uid", uid);
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
                        mycommand.Parameters.AddWithValue("@uid", uid);
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
                        mycommand.CommandText = @"INSERT INTO HISTORY(uid,fid,rev,filename,folder,timestamp,md5,deleted)
                                            VALUES (@uid,@fid,@rev,@filename,@folder,DATETIME('NOW'),@md5,@deleted)
                                                ; ";
                        mycommand.Prepare();
                        mycommand.Parameters.AddWithValue("@uid", uid);
                        mycommand.Parameters.AddWithValue("@fid", fid);
                        mycommand.Parameters.AddWithValue("@rev", 1);
                        mycommand.Parameters.AddWithValue("@filename", add.filename);
                        mycommand.Parameters.AddWithValue("@folder", add.folder);
                        //mycommand.Parameters.AddWithValue("@timestamp", timestamp);
                        mycommand.Parameters.AddWithValue("@md5", md5);
                        mycommand.Parameters.AddWithValue("@deleted", false);

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
                        mycommand.Parameters.AddWithValue("@uid", uid);
                        mycommand.Parameters.AddWithValue("@fid", fid);
                        mycommand.Parameters.AddWithValue("@rev", 1);
                        mycommand.Parameters.AddWithValue("@syncid", syncId);

                        nUpdated = mycommand.ExecuteNonQuery();
                        if (nUpdated != 1)
                            throw new Exception("No Row updated! Rollback");

                        //FILEDUMP INSERT
                        mycommand.CommandText = @"INSERT INTO FILES_DUMP(uid,fid,rev,filedump)
                                            VALUES (@uid,@fid,@rev,@filedump)
                                            ;";
                        mycommand.Prepare();
                        mycommand.Parameters.AddWithValue("@uid", uid);
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
            //Crea nuova entry in HISTORY con rev++
            //Update entry in snapshot con uid,fid   syncid= newsyncid AND rev = newrev
            //NEW Entry in filedump

        public static proto_server.DeleteOk Delete(ref proto_server.Delete delete, int uid)
        {
            //add.filename;add.folder;add.fileDump; uid;
            proto_server.DeleteOk  deleteOk = new proto_server.DeleteOk();

            
            //verifica se presente, già incluso in remove

            //crea una nuova versione con flag deleted = true

            //remove from snapshot
            
            SQLiteConnection cnn = new SQLiteConnection(dbConnection);
            cnn.Open();
            using (SQLiteCommand mycommand = new SQLiteCommand(cnn))
            {
                try
                {
                    //BEGIN TRANSACTION
                    using (var transaction = cnn.BeginTransaction())
                    {

                        //Use a datatable onlyonequery
                        mycommand.CommandText = @"SELECT MAX(HISTORY.rev)
                                                FROM HISTORY
                                                WHERE HISTORY.uid = @uid
                                                AND HISTORY.fid = @fid
                                                ; ";
                        mycommand.Prepare();
                        mycommand.Parameters.AddWithValue("@uid", uid);
                        mycommand.Parameters.AddWithValue("@fid", delete.fid);
                        object value = mycommand.ExecuteScalar();

                        int maxrev = int.Parse(value.ToString());
                        int rev = maxrev + 1;

                        /*

                        //seleziono  max syncid tra uid
                        mycommand.CommandText = @"SELECT MAX(SNAPSHOT.syncid)
                                                FROM SNAPSHOT
                                                WHERE SNAPSHOT.uid = @uid
                                                ;";
                        mycommand.Prepare();
                        mycommand.Parameters.AddWithValue("@uid", uid);
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
                        mycommand.Parameters.AddWithValue("@uid", uid);
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
                        mycommand.CommandText = @"INSERT INTO HISTORY(uid,fid,rev,filename,folder,timestamp,md5,deleted)
                                            VALUES (@uid,@fid,@rev,@filename,@folder,DATETIME('NOW'),@md5,@deleted)
                                                ; ";
                        mycommand.Prepare();
                        mycommand.Parameters.AddWithValue("@uid", uid);
                        mycommand.Parameters.AddWithValue("@fid", fid);
                        mycommand.Parameters.AddWithValue("@rev", 1);
                        mycommand.Parameters.AddWithValue("@filename", add.filename);
                        mycommand.Parameters.AddWithValue("@folder", add.folder);
                        //mycommand.Parameters.AddWithValue("@timestamp", timestamp);
                        mycommand.Parameters.AddWithValue("@md5", md5);
                        mycommand.Parameters.AddWithValue("@deleted", false);

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
                        mycommand.Parameters.AddWithValue("@uid", uid);
                        mycommand.Parameters.AddWithValue("@fid", fid);
                        mycommand.Parameters.AddWithValue("@rev", 1);
                        mycommand.Parameters.AddWithValue("@syncid", syncId);

                        nUpdated = mycommand.ExecuteNonQuery();
                        if (nUpdated != 1)
                            throw new Exception("No Row updated! Rollback");

                        //FILEDUMP INSERT
                        mycommand.CommandText = @"INSERT INTO FILES_DUMP(uid,fid,rev,filedump)
                                            VALUES (@uid,@fid,@rev,@filedump)
                                            ;";
                        mycommand.Prepare();
                        mycommand.Parameters.AddWithValue("@uid", uid);
                        mycommand.Parameters.AddWithValue("@fid", fid);
                        mycommand.Parameters.AddWithValue("@rev", 1);
                        mycommand.Parameters.AddWithValue("@filedump", add.fileDump);

                        nUpdated = mycommand.ExecuteNonQuery();
                        if (nUpdated != 1)
                            throw new Exception("No Row updated! Rollback");

                        addOk.fid = fid;
                        addOk.rev = 1;
                        */
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
            //return addOk;
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
                    mycommand.CommandText = @"SELECT FILES_DUMP.filedump
                                            FROM FILES_DUMP
                                            WHERE FILES_DUMP.uid = @uid
                                            AND FILES_DUMP.fid = @fid
                                            AND FILES_DUMP.rev = @rev
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
                        //var values = row.ItemArray;

                        getResponse = new proto_server.GetResponse()
                        {
                            fileInfo = new proto_server.FileToGet()
                            {
                                fid = fid,
                                rev = rev
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