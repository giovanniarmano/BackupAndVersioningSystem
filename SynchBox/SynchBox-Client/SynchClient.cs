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
        public Dictionary<string, string> editedDirectory = new Dictionary<string, string>();
        public Dictionary<string, string> deletedFiles = new Dictionary<string, string>();

        NetworkStream netStream;
        MainWindow.SessionVars sessionVars;
        int syncIdServer;
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
                if (proto_client.LockAcquireWrapper(netStream))
                {
                    clientServerAlignment();
                    proto_client.LockReleaseWrapper(netStream);
                }

                watch(); // inizio il monitoraggio delle cartelle
                
            }catch (Exception e)
            {
                proto_client.LockReleaseWrapper(netStream);
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
            if (remoteFiles.Count != 0 && sessionVars.lastSyncId == syncIdServer)
            {
                return;
            }
            populate_dictionary(netStream);

            sessionVars.lastSyncId = syncIdServer;

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
            aTimer.Enabled = false;

            if (editedFiles.Count == 0 && editedDirectory.Count == 0 && deletedFiles.Count == 0)
            {
                aTimer.Enabled = true;
                return;
            }
            
            if (!proto_client.LockAcquireWrapper(netStream))
            {
                return;
            }
            try
            {
                clientServerAlignment();

                if (editedDirectory.Count > 0)
                {
                    foreach (KeyValuePair<string, string> entry in editedDirectory)
                    {
                        selectSyncAction(entry.Key);
                        //editedDirectory.Remove(entry.Key);  // sarebbe meglio così in questo modo impedisco problemi si sync con l'handle degli eventi, ma scassa per la modifica del dizionario
                    }
                    editedDirectory.Clear();
                }

                if (editedFiles.Count > 0)
                {
                    foreach (KeyValuePair<string, string> entry in editedFiles)
                    {
                        selectSyncAction(entry.Key);
                        //editedFiles.Remove(entry.Key);
                    }
                    editedFiles.Clear();
                }

                if (deletedFiles.Count > 0)
                {
                    foreach (KeyValuePair<string, string> entry in deletedFiles)
                    {
                        syncDeletefile(entry.Key);
                    }
                    deletedFiles.Clear();
                }


                //potrebbe non essere aperta la sessione
                proto_client.EndSessionWrapper(netStream, syncSessionId);
                flagSession = false;

                syncIdServer = proto_client.GetSynchIdWrapper(netStream);
                sessionVars.lastSyncId = syncIdServer;
                writeChanges();

                proto_client.LockReleaseWrapper(netStream);
                aTimer.Enabled = true;
            }
            catch (Exception ex)
            {
                proto_client.LockReleaseWrapper(netStream);
                Logging.WriteToLog(ex.ToString());
            }
            
        }

        private void selectActionFolder(string p)
        {
            selectSyncAction(p);

            foreach (string d in Directory.GetDirectories(p))
            {
                selectActionFolder(d);
                foreach (string f in Directory.GetFiles(d))
                {
                    selectSyncAction(f);
                }
            }
        }

        private void selectSyncAction(string path)
        {
            if (remoteFiles.ContainsKey(path)) // se contiene la chiave non è un nuovo file
            {
                if (File.Exists(path)) // se esiste nel fs, allora vuol dire che è stato modificato
                {
                    //TODO: controllare md5
                    syncFile(path, "UPDATE");
                }
                else if (Directory.Exists(path))
                {
                    //Non fare niente, non devo aggiornare le cartelle, nel caso cambino nome, me la gestisco con il rinomina
                }
                else // file eliminato
                {
                    syncDeletefile(path);
                }
            }
            else
            {
                if (File.Exists(path))
                {
                    syncFile(path, "CREATE");
                }
                else if (!remoteFiles.ContainsKey(path+"\\"))
                {
                    if (Directory.Exists(path))
                    {
                        syncNewFolder(path);
                    }
                    else
                    {
                        syncDeletefile(path);
                    }
                }
            }
        }

        private void handlerChanged(object sender, FileSystemEventArgs e)
        {
            if (e.FullPath.CompareTo(sessionVars.path + "\\conf.ini") == 0)
            {
                return;
            }

            if (e.ChangeType.Equals(WatcherChangeTypes.Deleted))
            {
                if (editedDirectory.ContainsKey(e.FullPath))
                {
                    editedDirectory.Remove(e.FullPath);
                }
                if (!deletedFiles.ContainsKey(e.FullPath))
                {
                    deletedFiles.Add(e.FullPath, "CHANGE");
                }
            }
            else
            {
                if (deletedFiles.ContainsKey(e.FullPath))
                {
                    deletedFiles.Remove(e.FullPath);
                }
                if (Directory.Exists(e.FullPath))
                {
                    if (!editedDirectory.ContainsKey(e.FullPath))
                    {
                        editedDirectory.Add(e.FullPath, "CHANGE");
                    }
                }
                else if (File.Exists(e.FullPath))
                {
                    if (!editedFiles.ContainsKey(e.FullPath))
                    {
                        editedFiles.Add(e.FullPath, "CHANGE");
                    }
                }
            }
            
        }

        private void handlerRename(object sender, RenamedEventArgs e)
        {
            //throw new NotImplementedException();
        }

        private void syncFile(string path, string action)
        {
            string hash = CalculateMD5Hash(File.ReadAllBytes(path));
            if (action.CompareTo("UPDATE")==0)
            {
                syncUpdatefile(path, hash);
            }
            else if (action.CompareTo("CREATE")==0)
            {
                syncNewfile(path, hash);
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
                    if (!remoteFiles.ContainsKey(sessionVars.path + fileInfo.folder + fileInfo.filename))
                    {
                        remoteFiles.Add(sessionVars.path + fileInfo.folder + fileInfo.filename, fileInfo);
                    }
                    else
                    {
                        //TODO: panico
                    }
                    
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

                    proto_client.GetListWrapper(netStream, ref getList); // TODO: problema sulla sincronizzazione delle cartelle (le chiede anche se ci sono per via dello slash
                    proto_client.GetResponse getResponse = new proto_client.GetResponse();

                    for (int i = 0; i < getList.n; i++)
                    {
                        proto_client.GetResponseWrapper(netStream, ref getResponse);

                        string fileName = sessionVars.path + getResponse.fileInfo.folder + getResponse.fileInfo.filename;
                        
                        if (getResponse.fileInfo.dir)
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(fileName));
                        }
                        else
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(sessionVars.path + getResponse.fileInfo.folder));
                            System.IO.File.WriteAllText(fileName, System.Text.Encoding.UTF8.GetString(getResponse.fileDump));
                        }
                    }
                }

            }
            catch (System.Exception excpt)
            {
                proto_client.LockReleaseWrapper(netStream); //TODO: controllare che si possa fare sempre
                Console.WriteLine(excpt.Message);
            }
        }

        private void chooseAction(string f)
        {
            string localHash = CalculateMD5Hash(File.ReadAllBytes(f));
            //string localHash = computeFileHash(f);
            if (!remoteFiles.ContainsKey(f))
            {
                syncNewfile(f, localHash); //nuovo file da aggiungere
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
                    syncNewfile(newName, localHash);

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

        private void syncDeletefile(string path)
        {
            checkBeginSession(netStream);

            proto_client.Delete delete = new proto_client.Delete();
            proto_client.DeleteOk deleteOk = new proto_client.DeleteOk();

            if (remoteFiles.ContainsKey(path))
            {
                delete.fid = remoteFiles[path].fid;
            }
            else if (remoteFiles.ContainsKey(path+"\\"))
            {
                delete.fid = remoteFiles[path + "\\"].fid;
            }
            
            deleteOk = proto_client.DeleteWrapper(netStream, ref delete);

            remoteFiles.Remove(path);
        }

        private void syncUpdatefile(string path, string hash)
        {
            checkBeginSession(netStream);

            proto_client.Update Update = new proto_client.Update();
            proto_client.UpdateOk UpdateOk = new proto_client.UpdateOk();
            Update.fid = remoteFiles[path].fid;
            Update.fileDump = File.ReadAllBytes(path);

            UpdateOk = proto_client.UpdateWrapper(netStream, ref Update);

            remoteFiles[path].md5 = hash;
        }

        private void syncNewfile(string path, string hash)
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


        //ancora da ricontrollare
        private void syncNewFolder(string path)
        {
            checkBeginSession(netStream);

            proto_client.Add add = new proto_client.Add();
            proto_client.AddOk addOk = new proto_client.AddOk();
            proto_client.FileListItem fileInfo = new proto_client.FileListItem();


            add.filename = Path.GetFileName(path);
            add.folder = Path.GetDirectoryName(path).Replace(sessionVars.path, "") + "\\";
            add.dir = true;

            addOk = proto_client.AddWrapper(netStream, ref add);

            fileInfo.fid = addOk.fid;
            fileInfo.rev = addOk.rev;
            fileInfo.filename = Path.GetFileName(path);
            fileInfo.folder = Path.GetDirectoryName(path).Replace(sessionVars.path, "") + "\\";
            fileInfo.deleted = false;
            fileInfo.md5 = null;
            fileInfo.dir = true;

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
            watcher.IncludeSubdirectories = true;
            watcher.Path = sessionVars.path;
            watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.CreationTime | NotifyFilters.Attributes;
            watcher.Filter = "*.*";
            watcher.Changed += new FileSystemEventHandler(handlerChanged);
            watcher.Created += new FileSystemEventHandler(handlerChanged);
            watcher.Deleted += new FileSystemEventHandler(handlerChanged);
            watcher.Renamed += new RenamedEventHandler(handlerRename);
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
