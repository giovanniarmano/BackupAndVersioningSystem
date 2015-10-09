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
        public static proto_server.AddOk Add(ref proto_server.Add add, int uid)
        {
            //add.filename;add.folder;add.fileDump; uid;
            proto_server.AddOk addOk = new proto_server.AddOk();

            //calcolo md5 & campi timestamp
            string md5 = proto_server.CalculateMD5Hash(add.fileDump);
            string timestamp = DateTime.Today.ToString();

            
            SQLiteConnection cnn = new SQLiteConnection(dbConnection);
            cnn.Open();
            using (SQLiteCommand mycommand = new SQLiteCommand(cnn))
            {   //BEGIN TRANSACTION
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
                    if(value.ToString().CompareTo("0") != 0)
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
                    if (value == null)
                    {
                        maxsyncId = 0;
                    }
                    else {
                        maxsyncId = int.Parse(value.ToString());
                    }
                    //syncId
                    int syncId = maxsyncId++;

                    //seleziono  max fid tra uid
                    mycommand.CommandText = @"SELECT MAX(HISTORY.fid)
                                                FROM HISTORY
                                                WHERE HISTORY.uid = @uid
                                                ;";
                    mycommand.Prepare();
                    mycommand.Parameters.AddWithValue("@uid", uid);
                    value = mycommand.ExecuteScalar();
                    int maxfid = -1;
                    if (value == null)
                    {
                        maxfid = 0;
                    }
                    else
                    {
                        maxfid = int.Parse(value.ToString());
                    }
                    //fid
                    int fid = maxfid++;



                    //inserisco in HISTORY SNAPSJOT  e FILE_DUMP
                    //Add vuol dire che non ho da fare update ma solo insert
                    //HISTORY INSERT
                    mycommand.CommandText = @"INSERT INTO HISTORY(uid,fid,rev,filename,folder,timestamp,md5,deleted)
                                            VALUES (@uid,@fid,@rev,@filename,@folder,@timestamp,@md5,@deleted)
                                                ; ";
                    mycommand.Prepare();
                    mycommand.Parameters.AddWithValue("@uid", uid);
                    mycommand.Parameters.AddWithValue("@fid", fid);
                    mycommand.Parameters.AddWithValue("@rev", 1);
                    mycommand.Parameters.AddWithValue("@filename", add.filename);
                    mycommand.Parameters.AddWithValue("@folder", add.folder);
                    mycommand.Parameters.AddWithValue("@timestamp", timestamp);
                    mycommand.Parameters.AddWithValue("@deleted", false);

                    int nUpdated = mycommand.ExecuteNonQuery();
                    if (nUpdated != 1)
                        throw new Exception("No Row updated! Rollback");

                    //SNAPSHOT INSERT

                    //FILEDUMP INSERT


                    //END TRANSACTION
                    transaction.Commit();
                }
            }
            return addOk;
        }

        //TODO Complete!
        public static proto_server.GetResponse GetResponse(int fid, int rev, int uid)    
        {
            try
            {
                string sql = @"SELECT HISTORY.fid, HISTORY.rev,HISTORY.filename, HISTORY.folder, HISTORY.timestamp,HISTORY.md5,HISTORY.deleted
                            FROM HISTORY, SNAPSHOT
                            WHERE HISTORY.fid=SNAPSHOT.fid and HISTORY.uid=SNAPSHOT.uid and HISTORY.rev=SNAPSHOT.rev
                            AND HISTORY.uid = " + uid + @"
                            ;";

            DataTable dt = db.GetDataTable(sql);
                
            proto_server.GetResponse getResponse;// = new proto_server.GetResponse();

            foreach (DataRow row in dt.Rows)
            {
                var values = row.ItemArray;

                getResponse = new proto_server.GetResponse()
                {
                    fileInfo = new proto_server.FileToGet()
                    {
                        fid = fid,
                        rev = rev
                    },    
                    fileDump = null      
                };
                return getResponse;
            }
            Logging.WriteToLog("PANIC! No file found");
            return null;
            }
            catch (Exception e)
            {
                Logging.WriteToLog(e.ToString());
                return null;
            }

        }

        public static proto_server.ListResponse ListResponseLast(int uid)
        {
            string sql = @"SELECT HISTORY.fid, HISTORY.rev,HISTORY.filename, HISTORY.folder, HISTORY.timestamp,HISTORY.md5,HISTORY.deleted
                            FROM HISTORY, SNAPSHOT
                            WHERE HISTORY.fid=SNAPSHOT.fid and HISTORY.uid=SNAPSHOT.uid and HISTORY.rev=SNAPSHOT.rev
                            AND HISTORY.uid = "+uid+@"
                            ;";

            DataTable dt = db.GetDataTable(sql);

            proto_server.ListResponse listResponse = new proto_server.ListResponse();
            listResponse.fileList = new List<proto_server.FileListItem>(dt.Rows.Count);
            foreach(DataRow row in dt.Rows)
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
                    Logging.WriteToLog(e.ToString());
                }
            }
            
            return listResponse;
        }
    }
}