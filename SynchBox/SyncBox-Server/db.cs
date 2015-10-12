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
    public static partial class  db
    {
        static string dbConnection; 
        static string dbPath;
       
        //public static db(string dbConn)
        //{
        //    //dbConnection = "e:\\backup\\db.db";
        //    //dbConnection = dbConn;
        //    dbPath = dbConn;
        //    dbConnection = string.Format(@"Data Source={0}; Pooling=false; FailIfMissing=false;", dbConn);

        //}


        public static void setDbConn(string dbConn)
        {
            //dbConnection = "e:\\backup\\db.db";
            //dbConnection = dbConn;
            dbPath = dbConn;
            dbConnection = string.Format(@"Data Source={0}; Pooling=false; FailIfMissing=false;", dbConn);
        }
        //public db() { }

       // public void set_path(string path)
       // {
        //    this.dbConnection = path;
       // }

        //start viene chiamata solo dal main thread del server, che verifica se il percorso è valido etc ec e
        // ... e se il db non esiste o èp vuoto ne crea uno con le tabelle di default!

        public static void start()
        {
            bool create_table = false;
            //if not db, create db
            if (!File.Exists(dbPath))
            {
                if (MessageBox.Show("File '" + db.dbPath + "' Not Exist. Do you want to create a new db file?", "Create new db ?", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
                {
                    throw new Exception("No db opened or created!");
                }
                else
                {
                    create_table = true;
                    SQLiteConnection.CreateFile(dbPath);
                }
            }
            if (create_table)
            {
                create_db_table();
            }
        }

        private static void create_db_table()
        {
            var connString = string.Format(@"Data Source={0}; Pooling=false; FailIfMissing=false;", dbPath);
            SQLiteConnection cnntodb = new SQLiteConnection(connString);
            cnntodb.Open();

            //----------HERE INITIAL TABLES-------------------
            //string sql = @"CREATE TABLE [USERS] ( [uid] INTEGER  NOT NULL PRIMARY KEY AUTOINCREMENT, [user] TEXT  UNIQUE NOT NULL, [md5] TEXT  NOT NULL);";

            string sql = @"
                            --
                            -- File generated with SQLiteStudio v3.0.6 on lun ott 12 20:45:25 2015
                            --
                            -- Text encoding used: windows-1252
                            --
                            PRAGMA foreign_keys = off;
                            BEGIN TRANSACTION;

                            -- Table: HISTORY
                            CREATE TABLE HISTORY (uid INTEGER NOT NULL, fid INTEGER NOT NULL, rev INTEGER NOT NULL, filename TEXT, folder TEXT, timestamp DATETIME, md5 TEXT, deleted BOOLEAN, synchsessionid INTEGER, PRIMARY KEY (uid, fid, rev));

                            -- Table: USERS
                            CREATE TABLE [USERS] ( [uid] INTEGER  NOT NULL PRIMARY KEY AUTOINCREMENT, [user] TEXT  UNIQUE NOT NULL, [md5] TEXT  NOT NULL);

                            -- Table: FILES_DUMP
                            CREATE TABLE [FILES_DUMP] (

                                                        [uid] INTEGER  NOT NULL,

                                                        [fid] INTEGER  NOT NULL,

                                                        [rev] INTEGER  NOT NULL,

                                                        [filedump] BLOB  NULL,

                                                        PRIMARY KEY ([uid],[fid],[rev])

                                                        );

                            -- Table: SNAPSHOT
                            CREATE TABLE [SNAPSHOT] (

                                                        [uid] INTEGER  NOT NULL,

                                                        [fid] INTEGER  NOT NULL,

                                                        [rev] INTEGER  NULL,

                                                        [syncid] INTEGER  NULL,

                                                        PRIMARY KEY ([uid],[fid])

                                                        );

                            -- Table: SYNCH_SESSION
                            CREATE TABLE SYNCH_SESSION (uid INTEGER NOT NULL, synchsessionid INTEGER NOT NULL, timestamp DATETIME, n_added INTEGER, n_updated INTEGER, n_deleted INTEGER, PRIMARY KEY (uid, synchsessionid));

                            COMMIT TRANSACTION;
                            PRAGMA foreign_keys = on;


                        ";


            SQLiteCommand command = new SQLiteCommand(sql, cnntodb);
            command.ExecuteNonQuery();
            cnntodb.Close();
        }

        /// <summary>  
        ///     Single Param Constructor for specifying the DB file.  
        /// </summary>  
        /// <param name="inputFile">The File containing the DB</param>  

        //public db(string DBDirectoryInfo, String inputFile)
        //{
        //    string sourceFile = Path.Combine(DBDirectoryInfo, inputFile);
        //    dbConnection = String.Format("Data Source={0}", sourceFile);
        //}

        /// <summary>  
        ///     Single Param Constructor for specifying advanced connection options.  
        /// </summary>  
        /// <param name="connectionOpts">A dictionary containing all desired options and their 
        //   values</param>  
        //public db(Dictionary<String, String> connectionOpts)
        //{
        //    String str = "";
        //    foreach (KeyValuePair<String, String> row in connectionOpts)
        //    {
        //        str += String.Format("{0}={1}; ", row.Key, row.Value);
        //    }
        //    str = str.Trim().Substring(0, str.Length - 1);
        //    dbConnection = str;
        //}

        /// <summary>  
        ///     Allows the programmer to run a query against the Database.  
        /// </summary>  
        /// <param name="sql">The SQL to run</param>  
        /// <returns>A DataTable containing the result set.</returns>  
        public static DataTable GetDataTable(string sql)
        {
            DataTable dt = new DataTable();
            try
            {
                SQLiteConnection cnn = new SQLiteConnection(dbConnection);
                cnn.Open();
                SQLiteCommand mycommand = new SQLiteCommand(cnn);
                mycommand.CommandText = sql;
                SQLiteDataReader reader = mycommand.ExecuteReader();
                dt.Load(reader);
                reader.Close();
                cnn.Close();
            }
            catch (Exception ex)
            {
                throw;
            }
            return dt;
        }

        /// <summary>  
        ///     Allows the programmer to interact with the database for purposes other than a query.  
        /// </summary>  
        /// <param name="sql">The SQL to be run.</param>  
        /// <returns>An Integer containing the number of rows updated.</returns>  
        public static  int ExecuteNonQuery(string sql)
        {
            SQLiteConnection cnn = new SQLiteConnection(dbConnection);
            cnn.Open();
            SQLiteCommand mycommand = new SQLiteCommand(cnn);
            mycommand.CommandText = sql;
            int rowsUpdated = mycommand.ExecuteNonQuery();
            cnn.Close();
            return rowsUpdated;
        }

        /// <summary>  
        ///     Allows the programmer to retrieve single items from the DB.  
        /// </summary>  
        /// <param name="sql">The query to run.</param>  
        /// <returns>A string.</returns>  
        public static string ExecuteScalar(string sql)
        {
            SQLiteConnection cnn = new SQLiteConnection(dbConnection);
            cnn.Open();
            SQLiteCommand mycommand = new SQLiteCommand(cnn);
            mycommand.CommandText = sql;
            object value = mycommand.ExecuteScalar();
            cnn.Close();
            if (value != null)
            {
                return value.ToString();
            }
            return "";
        }

        /// <summary>  
        ///     Allows the programmer to easily update rows in the DB.  
        /// </summary>  
        /// <param name="tableName">The table to update.</param>  
        /// <param name="data">A dictionary containing Column names and their new values.</param>  
        /// <param name="where">The where clause for the update statement.</param>  
        /// <returns>A boolean true or false to signify success or failure.</returns>  
        public static bool Update(String tableName, Dictionary<String, String> data, String where)
        {
            String vals = "";
            Boolean returnCode = true;
            if (data.Count >= 1)
            {
                foreach (KeyValuePair<String, String> val in data)
                {
                    vals += String.Format(" {0} = '{1}',", val.Key.ToString(), val.Value.ToString());
                }
                vals = vals.Substring(0, vals.Length - 1);
            }
            try
            {
                db.ExecuteNonQuery(String.Format("update {0} set {1} where {2};", tableName,
                                       vals, where));
            }
            catch (Exception ex)
            {
                returnCode = false;
                //ServiceLogWriter.LogError(ex);  
            }
            return returnCode;
        }

        /// <summary>  
        ///     Allows the programmer to easily delete rows from the DB.  
        /// </summary>  
        /// <param name="tableName">The table from which to delete.</param>  
        /// <param name="where">The where clause for the delete.</param>  
        /// <returns>A boolean true or false to signify success or failure.</returns>  
        public static  bool Delete(String tableName, String where)
        {
            Boolean returnCode = true;
            try
            {
                db.ExecuteNonQuery(String.Format("delete from {0} where {1};", tableName, where));
            }
            catch (Exception ex)
            {
                returnCode = false;
            }
            return returnCode;
        }

        /// <summary>  
        ///     Allows the programmer to easily insert into the DB  
        /// </summary>  
        /// <param name="tableName">The table into which we insert the data.</param>  
        /// <param name="data">A dictionary containing the column names and data for the insert.</param>  
        /// <returns>A boolean true or false to signify success or failure.</returns>  
        public static  bool Insert(String tableName, Dictionary<String, String> data)
        {
            String columns = "";
            String values = "";
            Boolean returnCode = true;
            foreach (KeyValuePair<String, String> val in data)
            {
                columns += String.Format(" {0},", val.Key.ToString());
                values += String.Format(" '{0}',", val.Value);
            }
            columns = columns.Substring(0, columns.Length - 1);
            values = values.Substring(0, values.Length - 1);
            try
            {
                db.ExecuteNonQuery(String.Format("insert into {0}({1}) values({2});", tableName, columns, values));
            }
            catch (Exception ex)
            {
                returnCode = false;
            }
            return returnCode;
        }

        /// <summary>  
        ///     Allows the programmer to easily delete all data from the DB.  
        /// </summary>  
        /// <returns>A boolean true or false to signify success or failure.</returns>  
        public static  bool ClearDB()
        {
            DataTable tables;
            try
            {
                tables = db.GetDataTable("select NAME from SQLITE_MASTER where type= 'table' order by NAME;");
                foreach (DataRow table in tables.Rows)
                {
                    db.ClearTable(table["NAME"].ToString());
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>  
        ///     Allows the user to easily clear all data from a specific table.  
        /// </summary>  
        /// <param name="table">The name of the table to clear.</param>  
        /// <returns>A boolean true or false to signify success or failure.</returns>  
        public static  bool ClearTable(String table)
        {
            try
            {

                db.ExecuteNonQuery(String.Format("delete from {0};", table));
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>  
        ///     Allows the programmer to easily test connect to the DB.  
        /// </summary>  
        /// <returns>A boolean true or false to signify success or failure.</returns>  
        public static bool TestConnection()
        {
            using (SQLiteConnection cnn = new SQLiteConnection(dbConnection))
            {
                try
                {
                    cnn.Open();
                    return true;
                }
                catch
                {
                    return false;
                }
                finally
                {
                    // Close the database connection  
                    if ((cnn != null) && (cnn.State != ConnectionState.Open))
                        cnn.Close();
                }
            }
        }

        /// <summary>  
        ///     Allows the programmer to easily test if table exists in the DB.  
        /// </summary>  
        /// <returns>A boolean true or false to signify success or failure.</returns>  
        public static bool IsTableExists(String tableName)
        {
            string count = "0";
            if (dbConnection == default(string))
                return false;
            using (SQLiteConnection cnn = new SQLiteConnection(dbConnection))
            {
                try
                {
                    cnn.Open();
                    if (tableName == null || cnn.State != ConnectionState.Open)
                    {
                        return false;
                    }
                    String sql = string.Format("SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name ='{0}'", tableName);
                    count = ExecuteScalar(sql);
                }
                finally
                {
                    // Close the database connection  
                    if ((cnn != null) && (cnn.State != ConnectionState.Open))
                        cnn.Close();
                }
            }
            return Convert.ToInt32(count) > 0;
        }
    }
}