using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;
using System.Net;
using System.Net.Sockets;
using System.Windows;
using System.Data;
using System.Security.Cryptography;
using System.Threading;
using System.IO;
using Newtonsoft.Json;
using System.Timers;

namespace SynchBox_Client
{
    public class SynchClient
    {
        public class remoteFileInfos
        {
            public string hash = "";
            public int fid = -1;
            public bool delete = true;

        }
        public Dictionary<string, remoteFileInfos> remoteFiles = new Dictionary<string, remoteFileInfos>();

        public Dictionary<string, string> editedFiles = new Dictionary<string, string>();

        NetworkStream netStream;
        MainWindow.SessionVars sessionVars;

        FileSystemWatcher watcher;

        private static System.Timers.Timer aTimer;

        public async Task StartSyncAsync(NetworkStream netStream, MainWindow.SessionVars sessionVars)
        {
            this.sessionVars = sessionVars;
            this.netStream = netStream;

            int syncIdServer = proto_client.GetSynchIdWrapper(netStream);
            if (sessionVars.lastSyncId == -1 //se è la prima volta che sincronizzo questa cartella
                || sessionVars.lastSyncId < syncIdServer) // oppure se la revisione che ho in locale non è la più nuova
            {

                sessionVars.lastSyncId = syncIdServer; //imposto come ultima sincronizzazione quella del server
                remoteFiles = populate_dictionary(netStream); //scarico la struttura del server

                findDifference(netStream, sessionVars.path); //

                if (sessionVars.lastSyncId != -1) // se ho modificato qualcosa chiudo e aggiorno il lastSyncId
                {
                    proto_client.EndSessionWrapper(netStream, sessionVars.lastSyncId);
                }

            }

            watch(); // inizio il monitoraggio delle cartelle
            aTimer = new System.Timers.Timer(30000); //30 secs interval
            aTimer.Elapsed += new ElapsedEventHandler(Syncronize);
            GC.KeepAlive(aTimer); 
        }

        private void Syncronize(object sender, ElapsedEventArgs e)
        {
            if(editedFiles.Count == 0){
                return;
            }
            
            remoteFiles = populate_dictionary(netStream);

            foreach (KeyValuePair<string, string> entry in editedFiles)
            {
                if (entry.Value.CompareTo("DELETE") == 0)
                {
                    syncDeletefile(netStream, entry.Key);
                }
                else if (entry.Value.CompareTo("CREATE") == 0)
                {
                    syncFile(netStream, entry.Key, "CREATE");
                }
                else if (entry.Value.CompareTo("UPDATE") == 0)
                {
                    syncFile(netStream, entry.Key, "UPDATE");
                }
            }

            editedFiles.Clear();

            proto_client.EndSessionWrapper(netStream, sessionVars.lastSyncId);
            writeChanges();
        }

        private void writeChanges()
        {
            int i = 0;
            string json;

            string filePath = ".\\conf.ini";
            if (File.Exists(sessionVars.path + filePath))
            {
                using (StreamReader r = new StreamReader(sessionVars.path + filePath))
                {
                    json = r.ReadToEnd();
                    List<Item> items = JsonConvert.DeserializeObject<List<Item>>(json);
                    for (i = 0; i < items.Count;i++)
                    {
                        if (sessionVars.uid_str.CompareTo(items[i].uid) == 0)
                        {
                            items[i].syncId = sessionVars.lastSyncId.ToString();

                            json = JsonConvert.SerializeObject(items.ToArray());
                            System.IO.File.WriteAllText(sessionVars.path + filePath, json);
                            return;
                        }

                    }
                }
            }
            else
            {
                List<Item> _data = new List<Item>();
                _data.Add(new Item()
                {
                    uid = sessionVars.uid_str,
                    path = sessionVars.path,
                    syncId = sessionVars.lastSyncId.ToString()
                });
                json = JsonConvert.SerializeObject(_data.ToArray());

                //write string to file
                System.IO.File.WriteAllText(sessionVars.path + filePath, json);
            }
        }

        private void watch()
        {
            watcher = new FileSystemWatcher();
            watcher.Path = sessionVars.path;
            watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
            watcher.Filter = "*.*";
            watcher.Changed += new FileSystemEventHandler(handlerChanged);
            watcher.Created += new FileSystemEventHandler(handlerChanged);
            watcher.Deleted += new FileSystemEventHandler(handlerChanged);
            watcher.EnableRaisingEvents = true;
        }

        private void handlerChanged(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType.Equals(WatcherChangeTypes.Deleted))
            {
                editedFiles.Add(e.FullPath, "DELETE");
                //syncDeletefile(netStream, e.FullPath);
            }
            else if (e.ChangeType.Equals(WatcherChangeTypes.Created))
            {
                editedFiles.Add(e.FullPath, "CREATE");
                //syncFile(netStream, e.FullPath, "CREATE");
            }
            else if (e.ChangeType.Equals(WatcherChangeTypes.Changed) )
            {
                editedFiles.Add(e.FullPath, "UPDATE");
                // syncFile(netStream, e.FullPath, "UPDATE");
            }

            proto_client.EndSessionWrapper(netStream, sessionVars.lastSyncId);

        }

        private void syncFile(NetworkStream netStream, string path, string action)
        {
            string hash = computeFileHash(path);
            if (action.CompareTo("UPDATE")==0)
            {
                syncUpdatefile(netStream, path, hash);
            }
            else if (action.CompareTo("CREATE")==0)
            {
                syncNewfile(netStream, path, hash);
            }
        }

        public class Item
        {
            public string uid;
            public string path;
            public string syncId;
        }

        /*
         * 0 --> directory corrente uguale a quella precedentemente sincronizzata
         * 1 --> nuova directory di sincronizzazione 
         */
        public int getInitInformation(MainWindow.SessionVars sessionVars)
        {
            string filePath = ".\\conf.ini";
            if (File.Exists(sessionVars.path + filePath))
            {
                using (StreamReader r = new StreamReader(sessionVars.path + filePath))
                {
                    string json = r.ReadToEnd();
                    List<Item> items = JsonConvert.DeserializeObject<List<Item>>(json);
                    foreach (var item in items)
                    {
                        if (sessionVars.uid_str.CompareTo(item.uid) == 0)
                        {
                            sessionVars.lastSyncId = Int32.Parse(item.syncId); // copio le informazioni sull'ultima sincronizzazione contenute nel file conf.ini
                            if (item.path.CompareTo(sessionVars.path) != 0)
                            {
                                return 1;
                            }
                            return 0;
                        }
                    }
                }
            }
            return 1;
        }

        private Dictionary<string, remoteFileInfos> populate_dictionary(NetworkStream netStream)
        {
            Dictionary<string, remoteFileInfos> remoteFiles = new Dictionary<string, remoteFileInfos>();

            proto_client.ListResponse remoteFileList;
            remoteFileList = proto_client.ListRequestAllWrapper(netStream);
            foreach (proto_client.FileListItem fileInfo in remoteFileList.fileList)
            {
                remoteFileInfos tmp = new remoteFileInfos();
                tmp.hash = fileInfo.md5;
                tmp.fid = fileInfo.fid;
                tmp.delete = false;
                remoteFiles.Add(sessionVars.path + fileInfo.folder + fileInfo.filename, tmp);
            }

            return remoteFiles;

        }

        private void findDifference(NetworkStream netStream, string path)
        {
            try
            {
                var md5 = MD5.Create();
                foreach (string d in Directory.GetDirectories(path))
                {
                    foreach (string f in Directory.GetFiles(d))
                    {
                        string localHash = computeFileHash(f);
                        if (!remoteFiles.ContainsKey(f))
                        {
                            //nuovo file da aggiungere
                            syncNewfile(netStream, f, localHash);
                        }
                        else
                        {
                            if (localHash.CompareTo(remoteFiles[f].hash) != 0)
                            {
                                //il file è stato modificato, inviare al server la nuova versione
                                syncUpdatefile(netStream, f, localHash);
                            }
                            else
                            {
                                remoteFiles[f].delete = false;
                            }
                            //non fare niente, file ok
                        }
                    }
                    findDifference(netStream, d);
                }

                foreach (KeyValuePair<string, remoteFileInfos> entry in remoteFiles)
                {
                    if (entry.Value.delete == true)
                    {
                        syncDeletefile(netStream, entry.Key);
                    }
                }

            }
            catch (System.Exception excpt)
            {
                Console.WriteLine(excpt.Message);
            }
        }

        private void syncDeletefile(NetworkStream netStream, string path)
        {
            checkBeginSession(netStream);

            proto_client.Delete delete = new proto_client.Delete();
            proto_client.DeleteOk deleteOk = new proto_client.DeleteOk();

            delete.fid = remoteFiles[path].fid;
            deleteOk = proto_client.DeleteWrapper(netStream, ref delete);

            remoteFiles.Remove(path);
        }

        private void syncUpdatefile(NetworkStream netStream, string path, string hash)
        {
            checkBeginSession(netStream);

            proto_client.Update Update = new proto_client.Update();
            proto_client.UpdateOk UpdateOk = new proto_client.UpdateOk();
            Update.fid = remoteFiles[path].fid;
            Update.fileDump = File.ReadAllBytes(path);

            UpdateOk = proto_client.UpdateWrapper(netStream, ref Update);

            remoteFiles[path].hash = hash;
            remoteFiles[path].delete = false;
        }

        private void syncNewfile(NetworkStream netStream, string path, string hash)
        {
            checkBeginSession(netStream);

            proto_client.Add add = new proto_client.Add();
            proto_client.AddOk addOk = new proto_client.AddOk();
            remoteFileInfos fileInfo = new remoteFileInfos();
            add.filename = Path.GetFileName(path);
            add.folder = Path.GetDirectoryName(path).Replace(sessionVars.path, "") + "\\";
            add.fileDump = File.ReadAllBytes(path);

            addOk = proto_client.AddWrapper(netStream, ref add);

            fileInfo.hash = hash;
            fileInfo.fid = addOk.fid;
            fileInfo.delete = false;
            remoteFiles.Add(path, fileInfo);

        }


        private void checkBeginSession(NetworkStream netStream)
        {
            if (sessionVars.lastSyncId == -1)
            {
                sessionVars.lastSyncId = proto_client.BeginSessionWrapper(netStream);
            }
        }
        private string computeFileHash(string file)
        {
            var md5 = MD5.Create();
            var stream = File.OpenRead(file);
            string hash = System.Convert.ToBase64String(md5.ComputeHash(stream));
            return hash;
        }

        //funzione usata sul server.. controllare che torni lo stesso output
        public static string CalculateMD5Hash(byte[] byteArray)
        {
            MD5 md5 = System.Security.Cryptography.MD5.Create();
            byte[] hash = md5.ComputeHash(byteArray);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("X2"));
            }
            return sb.ToString();
        }
    }
}
