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
        public class Item
        {
            public string uid;
            public string path;
            public string syncId;
        }

        public Dictionary<string, proto_client.FileListItem> remoteFiles = new Dictionary<string, proto_client.FileListItem>();
        public Dictionary<string, string> editedFiles = new Dictionary<string, string>();

        NetworkStream netStream;
        MainWindow.SessionVars sessionVars;
        int syncIdServer; // TODO: da finire di valorizzare
        int syncSessionId = -1;
        bool flagSession = false;

        FileSystemWatcher watcher;
        private static System.Timers.Timer aTimer;

        public async Task StartSyncAsync(NetworkStream netStream, MainWindow.SessionVars sessionVars)
        {   
            try { 
                this.sessionVars = sessionVars;
                this.netStream = netStream;

                syncIdServer = proto_client.GetSynchIdWrapper(netStream);
                if (sessionVars.lastSyncId == -1  || sessionVars.lastSyncId < syncIdServer)
                {
                    clientServerAlignment();
                }

                watch(); // inizio il monitoraggio delle cartelle
                
            }catch (Exception e)
            {
                Logging.WriteToLog(e.ToString());
            }
        }

        private void clientServerAlignment()
        {
            if (syncIdServer == -1) //il server non ha dati, è la prima volta che lo chiamo
            {
                sessionVars.lastSyncId = 1;
                syncIdServer = 1;
                return;
            }
            if (sessionVars.lastSyncId == syncIdServer) //se sono già allineato non faccio niente
            {
                return;
            }

            sessionVars.lastSyncId = syncIdServer;
            populate_dictionary(netStream);

            int syncSessionIdTemporaneo = syncSessionId;

            findDifference(sessionVars.path);

            if (syncSessionId != syncSessionIdTemporaneo) // se ho modificato qualcosa chiudo e aggiorno il lastSyncId
            {
                proto_client.EndSessionWrapper(netStream, syncSessionId);
                flagSession = false;
            }
            syncIdServer = proto_client.GetSynchIdWrapper(netStream);
            sessionVars.lastSyncId = syncIdServer;
            writeChanges();
        }

        private void SyncronizeChanges(object sender, ElapsedEventArgs e)
        {
            
            if(editedFiles.Count == 0){
                return;
            }
            aTimer.Enabled = false;

            clientServerAlignment();

            foreach (KeyValuePair<string, string> entry in editedFiles)
            {
                if(Directory.Exists(entry.Key)){
                    foreach (string d in Directory.GetDirectories(entry.Key))
                    {
                        foreach (string f in Directory.GetFiles(d))
                        {
                            selectSyncAction(f);
                        }
                    }
                    foreach (string f in Directory.GetFiles(entry.Key))
                    {
                        selectSyncAction(f);
                    }
                }
                else
                {
                    selectSyncAction(entry.Key);
                }
            }

            editedFiles.Clear();

            //potrebbe non essere aperta la sessione
            proto_client.EndSessionWrapper(netStream, syncSessionId);
            flagSession = false;

            syncIdServer = proto_client.GetSynchIdWrapper(netStream);
            sessionVars.lastSyncId = syncIdServer;

            writeChanges();
            aTimer.Enabled = true;
        }

        private void selectSyncAction(string path)
        {
            if (remoteFiles.ContainsKey(path)) // se contiene la chiave non è un nuovo file
            {
                if (File.Exists(path)) // se esiste nel fs, allora vuol dire che è stato modificato
                {
                    //TODO: controllare md5
                    syncFile(netStream, path, "UPDATE");
                }
                else // file eliminato
                {
                    syncDeletefile(netStream, path);
                }
            }
            else
            {
                syncFile(netStream, path, "CREATE");
            }
        }

        private void handlerChanged(object sender, FileSystemEventArgs e)
        {
            if (e.FullPath.CompareTo(sessionVars.path + "\\conf.ini") == 0)
            {
                return;
            }
            if (!editedFiles.ContainsKey(e.FullPath)){
                editedFiles.Add(e.FullPath, "CHANGE");
            }
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

        /*
         * 0 --> directory corrente uguale a quella precedentemente sincronizzata
         * 1 --> nuova directory di sincronizzazione 
         */
        public int getInitInformation(MainWindow.SessionVars sessionVars)
        {
            string filePath = "\\conf.ini";
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

        private void populate_dictionary(NetworkStream netStream)
        {

            proto_client.ListResponse remoteFileList;

            remoteFileList = proto_client.ListRequestLastWrapper(netStream);
            //Potrebbe bastare una listRequestLast??

            //remoteFileList = proto_client.ListRequestAllWrapper(netStream);
            if (remoteFileList.fileList != null) { 
                foreach (proto_client.FileListItem fileInfo in remoteFileList.fileList)
                {
                    remoteFiles.Add(sessionVars.path + fileInfo.folder + fileInfo.filename, fileInfo);
                }
            }

        }

        private void findDifference(string path)  
        {
            try
            {
                foreach (string d in Directory.GetDirectories(path))
                {
                    foreach (string f in Directory.GetFiles(d))
                    {
                        chooseAction(f);
                    }
                    findDifference(d);
                }

                if (sessionVars.path.CompareTo(path) == 0)
                {
                    foreach (string f in Directory.GetFiles(path))
                    {
                        if (f.CompareTo(path + "\\conf.ini") == 0)
                        {
                            continue;
                        }
                        chooseAction(f);
                    }

                    proto_client.GetList getList = new proto_client.GetList();
                    getList.fileList = new List<proto_client.FileToGet>();
                    getList.n = 0;

                    foreach (KeyValuePair<string, proto_client.FileListItem> entry in remoteFiles)
                    {
                        if(entry.Value.deleted==false && !File.Exists(entry.Key)){
                            proto_client.FileToGet fileToGet = new proto_client.FileToGet();
                            getList.n++;
                            fileToGet.fid = entry.Value.fid;
                            fileToGet.rev = entry.Value.rev;
                            getList.fileList.Add(fileToGet);
                        }
                    }

                    proto_client.GetListWrapper(netStream, ref getList);
                    proto_client.GetResponse getResponse = new proto_client.GetResponse();

                    for (int i = 0; i < getList.n; i++)
                    {
                        proto_client.GetResponseWrapper(netStream, ref getResponse);

                        string fileName = sessionVars.path + getResponse.fileInfo.folder + getResponse.fileInfo.filename;

                        Directory.CreateDirectory(Path.GetDirectoryName(sessionVars.path + getResponse.fileInfo.folder)); // creo le directory

                        System.IO.File.WriteAllText(fileName, System.Text.Encoding.UTF8.GetString(getResponse.fileDump));
                    }
                }

            }
            catch (System.Exception excpt)
            {
                Console.WriteLine(excpt.Message);
            }
        }

        private void chooseAction(string f)
        {
            string localHash = computeFileHash(f);
            if (!remoteFiles.ContainsKey(f))
            {
                syncNewfile(netStream, f, localHash); //nuovo file da aggiungere
            }
            else
            {
                if (remoteFiles[f].deleted == true)
                {
                    File.Delete(f); //elimino il file locale
                }
                else if (localHash.CompareTo(remoteFiles[f].md5) != 0)
                {
                    string newName = MakeUnique(f);
                    System.IO.File.Move(f, newName);
                    syncNewfile(netStream, newName, localHash);

                    proto_client.GetList getList = new proto_client.GetList();
                    getList.fileList = new List<proto_client.FileToGet>();
                    proto_client.FileToGet fileToGet = new proto_client.FileToGet();

                    fileToGet.fid = remoteFiles[f].fid;
                    fileToGet.rev = remoteFiles[f].rev;
                    getList.fileList.Add(fileToGet);
                    getList.n = 1;

                    proto_client.GetListWrapper(netStream, ref getList);
                    proto_client.GetResponse getResponse = new proto_client.GetResponse();

                    proto_client.GetResponseWrapper(netStream, ref getResponse);

                    string fileName = sessionVars.path + getResponse.fileInfo.folder + getResponse.fileInfo.filename;

                    Directory.CreateDirectory(Path.GetDirectoryName(sessionVars.path + getResponse.fileInfo.folder)); // creo le directory

                    System.IO.File.WriteAllText(fileName, System.Text.Encoding.UTF8.GetString(getResponse.fileDump));
                }
                //non fare niente, file ok
            }
        }

        private string MakeUnique(string path)
        {
            string dir = Path.GetDirectoryName(path);
            string fileName = Path.GetFileNameWithoutExtension(path);
            string fileExt = Path.GetExtension(path);

            for (int i = 1; ; ++i)
            {
                path = Path.Combine(dir, fileName + " Copia in conflitto - " + i + fileExt);

                if (!File.Exists(path))
                    return path;
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

            remoteFiles[path].md5 = hash;
        }

        private void syncNewfile(NetworkStream netStream, string path, string hash)
        {
            checkBeginSession(netStream);

            proto_client.Add add = new proto_client.Add();
            proto_client.AddOk addOk = new proto_client.AddOk();
            proto_client.FileListItem fileInfo = new proto_client.FileListItem();


            add.filename = Path.GetFileName(path);
            add.folder = Path.GetDirectoryName(path).Replace(sessionVars.path, "") + "\\";
            add.fileDump = File.ReadAllBytes(path);

            addOk = proto_client.AddWrapper(netStream, ref add);

            fileInfo.fid = addOk.fid;
            fileInfo.rev = addOk.rev;
            fileInfo.filename = Path.GetFileName(path);
            fileInfo.folder = Path.GetDirectoryName(path).Replace(sessionVars.path, "") + "\\";
            fileInfo.md5 = hash;
            fileInfo.deleted = false;

            remoteFiles.Add(path, fileInfo);
        }

        private void writeChanges()
        {
            int i = 0;
            string json;
            bool add = false;

            string filePath = "\\conf.ini";
            string fileName = sessionVars.path + filePath;
            if (File.Exists(sessionVars.path + filePath))
            {
                List<Item> items;
                using (StreamReader r = new StreamReader(sessionVars.path + filePath))
                {
                    json = r.ReadToEnd();
                    items = JsonConvert.DeserializeObject<List<Item>>(json);
                    for (i = 0; i < items.Count && !add; i++)
                    {
                        if (sessionVars.uid_str.CompareTo(items[i].uid) == 0)
                        {
                            items[i].syncId = sessionVars.lastSyncId.ToString();
                            add = true;
                        }
                    }
                    if(!add){
                        items[i].syncId = sessionVars.lastSyncId.ToString();
                    }
                }
                
                json = JsonConvert.SerializeObject(items.ToArray());
                System.IO.File.WriteAllText(sessionVars.path + filePath, json);
                return;
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

            aTimer = new System.Timers.Timer(5000); //5 secs interval
            aTimer.AutoReset = true;
            aTimer.Enabled = true;
            aTimer.Elapsed += new ElapsedEventHandler(SyncronizeChanges);
            GC.KeepAlive(aTimer);
        }


        private void checkBeginSession(NetworkStream netStream)
        {
            if (flagSession == false && (sessionVars.lastSyncId == -1 || sessionVars.lastSyncId == syncIdServer))
            {
                syncSessionId = proto_client.BeginSessionWrapper(netStream);
                flagSession = true;
            }
        }
        private string computeFileHash(string file)
        {
            var md5 = MD5.Create();
            var stream = File.OpenRead(file);
            string hash = System.Convert.ToBase64String(md5.ComputeHash(stream));
            stream.Close();
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
