using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows;

namespace SyncBox_Server
{
    class db
    {
        private string db_path = "";

        //ctor
        public db() { 
            //set globals default here
            db_path = "c:\\db.db";
        }


        public void set_path(string path)
        {
            this.db_path = path;
        }

        //return false if no db exist or is succesfully created
        public bool start()
        {
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
                }
            }
            //if db, open it
            //open db

            return true;
        }

       
    }

    
}
