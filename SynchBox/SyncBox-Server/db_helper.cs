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