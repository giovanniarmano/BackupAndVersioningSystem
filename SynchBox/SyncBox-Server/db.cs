using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows;
using System.Data.SQLite;

namespace SyncBox_Server
{
    class db
    {
        private string db_path = "";
        SQLiteConnection dbConnection = null;

        //ctor
        public db() { 
            /////GLOBAL VARS/////////
            db_path = "e:\\backup\\db.db";    
        }


        public void set_path(string path)
        {
            this.db_path = path;
        }

        //return false if no db exist or is succesfully created
        public bool start()
        {
            bool create_table = false;
            //if not db, create db
            if (!File.Exists(db_path)) {
                if (MessageBox.Show("Create new db ?", "File '" + this.db_path +"' Not Exist. Do you want to create a new db file?", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
                {
                    //do no stuff
                    return false;
                }
                else
                {
                    //do yes stuff
                    //create db
                    create_table = true;
                    SQLiteConnection.CreateFile(db_path);
                }
            }
            //if db, open it

            var connString = string.Format(@"Data Source={0}; Pooling=false; FailIfMissing=false;", db_path);           
            //open db
            dbConnection = new SQLiteConnection(connString);
            dbConnection.Open();

            if (create_table) { 
                //INITIALIZE TABLES OF DB
                create_db_table();
            }

           // dbConnection.Close();

            return true;
        }

        private void create_db_table(){
            string sql = @"CREATE TABLE [USERS] ( [uid] INTEGER  NOT NULL PRIMARY KEY AUTOINCREMENT, [user] TEXT  UNIQUE NOT NULL, [md5] TEXT  NOT NULL);";
                SQLiteCommand command = new SQLiteCommand(sql, dbConnection);
                command.ExecuteNonQuery();
                
        }
       
    }

    
}
