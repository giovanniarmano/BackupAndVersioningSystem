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
using System.Web.Script.Serialization;

namespace SynchBox_Client
{
    public class SynchClient
    {
        private class Item
        {
            public string uid;
            public string path;
            public string syncId;
        }

        private int intervallo = 10;

        private Dictionary<string, proto_client.FileListItem> remoteFiles = new Dictionary<string, proto_client.FileListItem>();
        private Dictionary<string, proto_client.FileListItem> oldRemoteFiles = new Dictionary<string, proto_client.FileListItem>();


        private Dictionary<string, string> editedFiles = new Dictionary<string, string>();
        private Dictionary<string, string> editedDirectory = new Dictionary<string, string>();
        private Dictionary<string, string> renamedDirectory = new Dictionary<string, string>();
        private Dictionary<string, string> deletedFiles = new Dictionary<string, string>();
        private Dictionary<string, string> tmpFiles = new Dictionary<string, string>();


        //------------- HANDLER ------------------


        private Dictionary<string, string> handlerEditedFiles = new Dictionary<string, string>();
        private Dictionary<string, string> handlerEditedDirectory = new Dictionary<string, string>();
        private Dictionary<string, string> handlerRenamedDirectory = new Dictionary<string, string>();
        private Dictionary<string, string> handlerDeletedFiles = new Dictionary<string, string>();


        //------------- END ------------------

        private string[] fileInfoContainer;

        private NetworkStream netStream;
        private MainWindow.SessionVars sessionVars;
        private int syncIdServer;
        private int syncSessionId = -1;
        private bool flagSession = false;

        private FileSystemWatcher watcher;
        private static System.Timers.Timer aTimer;

        private Mutex SyncMutex = new Mutex();


        public void StartSyncAsync()
        {
            try { 
                syncIdServer = proto_client.GetSynchIdWrapper(netStream);
                if (proto_client.LockAcquireWrapper(netStream))
                {
                    clientServerAlignment();
                    proto_client.LockReleaseWrapper(netStream);

                    Logging.WriteToLog("Sinc completo, inizio monitoraggio cartelle");
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

            readRemoteFile();

            findDifference(sessionVars.path);

            if (syncSessionId != syncSessionIdTemporaneo) // se ho modificato qualcosa chiudo e aggiorno il lastSyncId
            {
                if (flagSession)
                {
                    proto_client.EndSessionWrapper(netStream, syncSessionId);
                    flagSession = false;
                }
            }
            syncIdServer = proto_client.GetSynchIdWrapper(netStream);
            sessionVars.lastSyncId = syncIdServer;
            writeChanges();
        }

        private void SyncronizeChanges(object sender, ElapsedEventArgs e)
        {
            if (!sessionVars.isSynchronizationActive)
            {
                fuggite();
                return;
            }
            try
            {
                aTimer.Enabled = false;

                syncIdServer = proto_client.GetSynchIdWrapper(netStream);

                if (handlerEditedFiles.Count == 0 && handlerEditedDirectory.Count == 0 && handlerRenamedDirectory.Count == 0 && handlerDeletedFiles.Count == 0
                        && sessionVars.lastSyncId == syncIdServer)
                {
                    return;
                }
            
                if (!proto_client.LockAcquireWrapper(netStream)) //TODO pernsare se fare merge di questa if con quella sopra
                {
                    return;
                }


                SyncMutex.WaitOne();

                editedFiles = handlerEditedFiles.ToDictionary(entry => entry.Key, entry => entry.Value);
                editedDirectory = handlerEditedDirectory.ToDictionary(entry => entry.Key, entry => entry.Value);
                renamedDirectory = handlerRenamedDirectory.ToDictionary(entry => entry.Key, entry => entry.Value);
                deletedFiles = handlerDeletedFiles.ToDictionary(entry => entry.Key, entry => entry.Value);

                handlerEditedFiles.Clear();
                handlerEditedDirectory.Clear();
                handlerRenamedDirectory.Clear();
                handlerDeletedFiles.Clear();

                SyncMutex.ReleaseMutex();

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
                    tmpFiles = editedFiles.ToDictionary(entry => entry.Key, entry => entry.Value);
                    foreach (KeyValuePair<string, string> entry in editedFiles)
                    {
                        selectSyncAction(entry.Key);
                    }
                    clearEditedFile();
                }

                if (renamedDirectory.Count > 0)
                {
                    foreach (KeyValuePair<string, string> entry in renamedDirectory)
                    {
                        findRenamedDifference(entry.Key);
                    }
                    renamedDirectory.Clear();
                }

                if (deletedFiles.Count > 0)
                {
                    foreach (KeyValuePair<string, string> entry in deletedFiles)
                    {
                        if (remoteFiles.ContainsKey(entry.Key) && remoteFiles[entry.Key].dir && !remoteFiles[entry.Key].deleted)// TODO: non dovrebbe passarci mai!
                        {
                            //syncDeleteFolder(entry.Key);
                            syncDeletefile(entry.Key);
                            deleteOldFiles();
                        }
                        else if (remoteFiles.ContainsKey(entry.Key + "\\") && remoteFiles[entry.Key + "\\"].dir && !remoteFiles[entry.Key + "\\"].deleted) 
                        {
                            //syncDeleteFolder(entry.Key + "\\");
                            syncDeletefile(entry.Key + "\\");
                            deleteOldFiles();
                        }
                        else if (remoteFiles.ContainsKey(entry.Key) && !remoteFiles[entry.Key].deleted)
                        {
                            syncDeletefile(entry.Key);
                        }
                        else if (remoteFiles.ContainsKey(entry.Key) && remoteFiles[entry.Key].deleted)
                        {
                            //selectSyncAction(entry.Key);
                        }
                        
                    }
                    deletedFiles.Clear();
                }


                //potrebbe non essere aperta la sessioneif (flagSession)
                if(flagSession){
                    proto_client.EndSessionWrapper(netStream, syncSessionId);
                    flagSession = false;
                }

                syncIdServer = proto_client.GetSynchIdWrapper(netStream);
                sessionVars.lastSyncId = syncIdServer;
                writeChanges();

                proto_client.LockReleaseWrapper(netStream);
            }
            catch (Exception ex)
            {
                proto_client.LockReleaseWrapper(netStream);
                Logging.WriteToLog(ex.ToString());
            }
            finally
            {
                aTimer.Enabled = true;
            }
            
        }

        private void clearEditedFile()
        {
            foreach (KeyValuePair<string, string> entry in tmpFiles)
            {
                if (entry.Value == null)
                {
                    editedFiles.Remove(entry.Key);
                }
            }
            tmpFiles.Clear();
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
            bool edited = true;
            try
            {
                if (remoteFiles.ContainsKey(path)) // se contiene la chiave non è un nuovo file
                {
                    if (File.Exists(path)) // se esiste nel fs, allora vuol dire che è stato modificato
                    {
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
                    else if (!remoteFiles.ContainsKey(path + "\\"))
                    {
                        if (Directory.Exists(path))
                        {
                            syncNewFolder(path); /// problem!
                        }
                        else
                        {
                            tmpFiles[path] = null;
                            //System.Windows.Forms.MessageBox.Show("Non so se dovrei eliminarlo.. sul server non c'è");
                            //syncDeletefile(path); //TODO: è giusta questa linea????
                        }
                    }
                    else if (remoteFiles.ContainsKey(path + "\\") && remoteFiles[path + "\\"].deleted && remoteFiles[path + "\\"].dir)
                    {
                        syncUpdateFolder(path);
                    }
                }
            }
            catch (System.IO.IOException syncEx)
            {
                edited = false;
            }
            catch (Exception syncEx)
            {
                Console.WriteLine(syncEx.Message);
            }
            finally
            {
                if (tmpFiles.ContainsKey(path) && edited)
                {
                    tmpFiles[path] = null;
                }
            }
            
        }

        private void handlerChanged(object sender, FileSystemEventArgs e)
        {
            if (e.FullPath.CompareTo(sessionVars.path + "\\conf.ini") == 0 || Path.GetFileName(e.FullPath)[0] == '~')
            {
                return;
            }
            try
            {
                SyncMutex.WaitOne();
                
                if (e.ChangeType.Equals(WatcherChangeTypes.Deleted))
                {
                    if (handlerEditedDirectory.ContainsKey(e.FullPath))
                    {
                        handlerEditedDirectory.Remove(e.FullPath);
                    }
                    if (!handlerDeletedFiles.ContainsKey(e.FullPath))
                    {
                        handlerDeletedFiles.Add(e.FullPath, "CHANGE");
                    }
                }
                else
                {
                    if (handlerDeletedFiles.ContainsKey(e.FullPath))
                    {
                        handlerDeletedFiles.Remove(e.FullPath);
                    }
                    if (Directory.Exists(e.FullPath))
                    {
                        if (!handlerEditedDirectory.ContainsKey(e.FullPath))
                        {
                            handlerEditedDirectory.Add(e.FullPath, "CHANGE");
                        }
                    }
                    else if (File.Exists(e.FullPath))
                    {
                        if (!handlerEditedFiles.ContainsKey(e.FullPath))
                        {
                            handlerEditedFiles.Add(e.FullPath, "CHANGE");
                        }
                    }
                }
            }
            catch (Exception exe)
            {
                Console.WriteLine(exe.Message);
            }
            finally
            {
                SyncMutex.ReleaseMutex();
            }
        }

        private void handlerRename(object sender, RenamedEventArgs e)
        {
            try
            {
                SyncMutex.WaitOne();
                handlerDeletedFiles.Add(e.OldFullPath, "CHANGE");

                if (Directory.Exists(e.FullPath))
                {
                    handlerRenamedDirectory.Add(e.FullPath, "CHANGE");
                    handlerEditedDirectory.Add(e.FullPath, "CHANGE");
                    if (handlerEditedDirectory.ContainsKey(e.OldFullPath + "\\"))
                    {
                        handlerEditedDirectory.Remove(e.OldFullPath + "\\");
                    }
                }
                else if (File.Exists(e.FullPath))
                {
                    handlerEditedFiles.Add(e.FullPath, "CHANGE");
                    if (handlerEditedFiles.ContainsKey(e.OldFullPath))
                    {
                        handlerEditedFiles.Remove(e.OldFullPath);
                    }
                }
            }
            catch (Exception exe)
            {
                Console.WriteLine(exe.Message);
            }
            finally
            {
                SyncMutex.ReleaseMutex();
            }
        }

        private void syncFile(string path, string action)
        {
            string hash = CalculateMD5Hash(File.ReadAllBytes(path));
            if (action.CompareTo("UPDATE") == 0 && (hash.CompareTo(remoteFiles[path].md5) != 0 || remoteFiles[path].deleted))
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
            remoteFiles.Clear();

            // -----------DA CONTROLLARE !!!!--------------- 
            /*editedFiles.Clear();
            editedDirectory.Clear();
            deletedFiles.Clear();
            */ // ----------------------------------------------

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
                        System.Windows.Forms.MessageBox.Show("Sul server ci sono due entry con lo stesso path.. non è consistente!!");
                        //essendo che le cartelle hanno lo \ alla fine e che i file non posso chiamarmi uguali nella stessa dir
                        // il fatto che passi di qui è un bel casino
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
                    chooseActionFolder(d);
                    if (Directory.Exists(d))
                    {
                        foreach (string f in Directory.GetFiles(d))
                        {
                            chooseAction(f);
                        }
                        findDifference(d);
                    }
                }

                if (sessionVars.path.CompareTo(path) == 0)
                {
                    foreach (string f in Directory.GetFiles(path))
                    {
                        if (f.CompareTo(path + "\\conf.ini") == 0 || f.CompareTo(path + "\\~remoteFiles") == 0 )
                        {
                            continue;
                        }
                        chooseAction(f);
                    }

                    if (syncIdServer > syncSessionId) // se sono indietro scarico i file che mi mancano
                    {
                        downloadNewFiles();
                    }
                    else // altrimenti elimino i file di troppo dal server
                    {
                        deleteOldFiles();
                    }
                }

            }
            catch (System.Exception excpt)
            {
                proto_client.LockReleaseWrapper(netStream); //TODO: controllare che si possa fare sempre
                Console.WriteLine(excpt.Message);
            }
        }

        private void findRenamedDifference(string path)
        {
            foreach (string d in Directory.GetDirectories(path))
            {
                chooseActionFolder(d);
                if (Directory.Exists(d))
                {
                    foreach (string f in Directory.GetFiles(d))
                    {
                        chooseAction(f);
                    }
                    findDifference(d);
                }
            }
            foreach (string f in Directory.GetFiles(path))
            {
                chooseAction(f);
            }
        }

        private void chooseActionFolder(string f)  // da fare prima dei file nella cartella
        {
            if (!remoteFiles.ContainsKey(f+"\\"))
            {
                syncNewFolder(f);
            }
            else
            {
                if (remoteFiles[f + "\\"].deleted == true)
                {
                    DeleteDirectory(f);
                }
            }
        }

        public static void DeleteDirectory(string target_dir)
        {
            string[] files = Directory.GetFiles(target_dir);
            string[] dirs = Directory.GetDirectories(target_dir);

            foreach (string file in files)
            {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }

            foreach (string dir in dirs)
            {
                DeleteDirectory(dir);
            }

            Directory.Delete(target_dir, false);
        }

        private void deleteOldFiles()
        {
            Dictionary<string, proto_client.FileListItem> tmpDict = new Dictionary<string, proto_client.FileListItem>();
            tmpDict = remoteFiles.ToDictionary(entry => entry.Key, entry => entry.Value);
            foreach (KeyValuePair<string, proto_client.FileListItem> entry in tmpDict)
            {
                if (entry.Value.deleted == false && ((!File.Exists(entry.Key) && !entry.Value.dir) || (!Directory.Exists(entry.Key) && entry.Value.dir)))
                {
                    syncDeletefile(entry.Key);
                }
            }
        }

        private void downloadNewFiles()
        {
            proto_client.GetList getList = new proto_client.GetList();
            getList.fileList = new List<proto_client.FileToGet>();
            getList.n = 0;

            foreach (KeyValuePair<string, proto_client.FileListItem> entry in remoteFiles)
            {
                if (entry.Value.deleted == false && ((!File.Exists(entry.Key) && !entry.Value.dir) || (!Directory.Exists(entry.Key) && entry.Value.dir)))
                {
                    proto_client.FileToGet fileToGet = new proto_client.FileToGet();
                    getList.n++;
                    fileToGet.fid = entry.Value.fid;
                    fileToGet.rev = entry.Value.rev;
                    getList.fileList.Add(fileToGet);
                }
            }

            if (getList.n == 0)
            {
                return;
            }

            proto_client.GetListWrapper(netStream, ref getList); // TODO: problema sulla sincronizzazione delle cartelle (le chiede anche se ci sono per via dello slash --> risolto (?)
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
                    //Directory.CreateDirectory(Path.GetDirectoryName(sessionVars.path + getResponse.fileInfo.folder));
                    System.IO.File.WriteAllBytes(fileName, getResponse.fileDump);
                }
            }
        }

        private void chooseAction(string f)
        {
            string localHash;
            try
            {
                localHash = CalculateMD5Hash(File.ReadAllBytes(f));
            }
            catch (System.IO.IOException ioEx)
            {
                if (!editedFiles.ContainsKey(f))
                {
                    editedFiles.Add(f, "CHANGE");
                }
                return;
            }
            if (!remoteFiles.ContainsKey(f))
            {
                syncNewfile(f, localHash); //nuovo file da aggiungere
            }
            else
            {
                if (remoteFiles[f].deleted == true)
                {
                    if (remoteFiles[f].dir)
                    {
                        Directory.Delete(f);
                    }
                    else
                    {
                        File.Delete(f); //elimino il file locale
                    }
                }
                else if (localHash.CompareTo(remoteFiles[f].md5) != 0 || localHash.CompareTo(oldRemoteFiles[f].md5) != 0)
                {

                    proto_client.GetList getList = new proto_client.GetList();
                    getList.fileList = new List<proto_client.FileToGet>();
                    proto_client.FileToGet fileToGet = new proto_client.FileToGet();
                    proto_client.GetResponse getResponse = new proto_client.GetResponse();

                    fileToGet.fid = remoteFiles[f].fid;
                    fileToGet.rev = remoteFiles[f].rev;
                    getList.fileList.Add(fileToGet);
                    getList.n = 1;


                    if (localHash.CompareTo(oldRemoteFiles[f].md5) != 0 && oldRemoteFiles[f].md5.CompareTo(remoteFiles[f].md5) == 0 )
                    {
                        syncUpdatefile(f, localHash);
                    }
                    else if (localHash.CompareTo(oldRemoteFiles[f].md5) == 0 && oldRemoteFiles[f].md5.CompareTo(remoteFiles[f].md5) != 0)
                    {
                        File.Delete(f);

                        proto_client.GetListWrapper(netStream, ref getList);
                        proto_client.GetResponseWrapper(netStream, ref getResponse);
                        string fileName = sessionVars.path + getResponse.fileInfo.folder + getResponse.fileInfo.filename;

                        try
                        {
                            System.IO.File.WriteAllBytes(f, getResponse.fileDump);
                        }
                        catch (Exception wEcx)
                        {
                            Console.WriteLine(wEcx.Message);
                        }
                    }
                    else if (localHash.CompareTo(oldRemoteFiles[f].md5) != 0 && oldRemoteFiles[f].md5.CompareTo(remoteFiles[f].md5) != 0)
                    {
                        string newName = MakeUnique(f);
                        System.IO.File.Move(f, newName);
                        syncNewfile(newName, localHash);

                        proto_client.GetListWrapper(netStream, ref getList);
                        proto_client.GetResponseWrapper(netStream, ref getResponse);

                        string fileName = sessionVars.path + getResponse.fileInfo.folder + getResponse.fileInfo.filename;

                        try
                        {
                            System.IO.File.WriteAllBytes(f, getResponse.fileDump);
                        }
                        catch (Exception wEcx)
                        {
                            Console.WriteLine(wEcx.Message);
                        }
                    }

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
        private string MakeUnique(string path, string rev)
        {
            string dir = System.IO.Path.GetDirectoryName(path);
            string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
            string fileExt = System.IO.Path.GetExtension(path);

            for (int i = 1; ; ++i)
            {
                path = System.IO.Path.Combine(dir, fileName + " Rev:" + rev + " - " + i + fileExt);

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

            //remoteFiles.Remove(path); //TODO :: quale delle due??
            
            if (remoteFiles.ContainsKey(path))
            {
                remoteFiles[path].deleted = true;
                remoteFiles[path].rev = remoteFiles[path].rev + 1;
                remoteFiles[path].md5 = null;
            }
            else if (remoteFiles.ContainsKey(path + "\\"))
            {
                remoteFiles[path + "\\"].deleted = true;
                remoteFiles[path].rev = remoteFiles[path].rev + 1;
            }
        }

        private void syncDeleteFolder(string path)
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

            deleteOk = proto_client.DeleteFolderWrapper(netStream, ref delete);

            //remoteFiles.Remove(path);

            if (remoteFiles.ContainsKey(path))
            {
                remoteFiles[path].deleted = true;
                remoteFiles[path].rev = remoteFiles[path].rev + 1;
            }
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
            remoteFiles[path].deleted = false;

            remoteFiles[path].rev = remoteFiles[path].rev + 1;
        }

        private void syncUpdateFolder(string path)
        {
            checkBeginSession(netStream);

            proto_client.Update Update = new proto_client.Update();
            proto_client.UpdateOk UpdateOk = new proto_client.UpdateOk();
            Update.fid = remoteFiles[path + "\\"].fid;

            UpdateOk = proto_client.UpdateWrapper(netStream, ref Update);

            remoteFiles[path + "\\"].deleted = false;
            remoteFiles[path + "\\"].rev = remoteFiles[path + "\\"].rev + 1;
        }

        public void syncNewfile(string path, string hash)
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
            fileInfo.filename = Path.GetFileName(path) + "\\";
            fileInfo.folder = Path.GetDirectoryName(path).Replace(sessionVars.path, "") + "\\";
            fileInfo.deleted = false;
            fileInfo.md5 = null;
            fileInfo.dir = true;

            remoteFiles.Add(path + "\\", fileInfo);
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
                //File.SetAttributes(sessionVars.path + filePath, File.GetAttributes(sessionVars.path + filePath) | FileAttributes.Hidden);
            }
            writeRemoteFile();
        }

        private void writeRemoteFile()
        {
            if(File.Exists(sessionVars.path + "\\~remoteFiles")){
                File.Delete(sessionVars.path + "\\~remoteFiles");
            }
            File.WriteAllText(sessionVars.path + "\\~remoteFiles", new JavaScriptSerializer().Serialize(remoteFiles));
        }

        private void readRemoteFile()
        {
            oldRemoteFiles.Clear();
            if (File.Exists(sessionVars.path + "\\~remoteFiles"))
            {
                oldRemoteFiles = new JavaScriptSerializer().Deserialize<Dictionary<string, proto_client.FileListItem>>(File.ReadAllText(sessionVars.path + "\\~remoteFiles"));
            }
            else
            {
                oldRemoteFiles = remoteFiles.ToDictionary(entry => entry.Key, entry => entry.Value);
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

            watcher.InternalBufferSize = 60000;

            aTimer = new System.Timers.Timer(intervallo); //5 secs interval
            aTimer.AutoReset = true;
            aTimer.Enabled = true;
            aTimer.Elapsed += new ElapsedEventHandler(SyncronizeChanges);
            GC.KeepAlive(aTimer);
        }

        private void checkBeginSession(NetworkStream netStream)
        {
            //if (flagSession == false && (sessionVars.lastSyncId == -1 || sessionVars.lastSyncId == syncIdServer))
            if (flagSession == false)
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

        private void fuggite()
        {
            Logging.WriteToLog("Syncronization cancellation requested ---> in progress");

            aTimer.Dispose();

            watcher.EnableRaisingEvents = false;
            watcher.Changed -= new FileSystemEventHandler(handlerChanged);
            watcher.Created -= new FileSystemEventHandler(handlerChanged);
            watcher.Deleted -= new FileSystemEventHandler(handlerChanged);
            watcher.Renamed -= new RenamedEventHandler(handlerRename);
            watcher.Dispose(); 

            editedFiles.Clear();
            editedDirectory.Clear();
            renamedDirectory.Clear();
            deletedFiles.Clear();
            tmpFiles.Clear();
            remoteFiles.Clear();


            handlerEditedFiles.Clear();
            handlerEditedDirectory.Clear();
            handlerRenamedDirectory.Clear();
            handlerDeletedFiles.Clear();

            Logging.WriteToLog("Syncronization cancellation requested ---> done");
        }

        public void setEnvironment(NetworkStream networkStream, MainWindow.SessionVars sessionVars)
        {
            this.sessionVars = sessionVars;
            this.netStream = networkStream;
        }

        public void overrideLocalCopy()
        {

            proto_client.GetList getList = new proto_client.GetList();
            getList.fileList = new List<proto_client.FileToGet>();
            proto_client.FileToGet fileToGet = new proto_client.FileToGet();
            proto_client.GetResponse getResponse = new proto_client.GetResponse();

            fileToGet.fid = Int32.Parse(fileInfoContainer[0]);
            fileToGet.rev = Int32.Parse(fileInfoContainer[1]);
            getList.fileList.Add(fileToGet);
            getList.n = 1;

            proto_client.GetListWrapper(sessionVars.socketClient.getStream(), ref getList);
            proto_client.GetResponseWrapper(sessionVars.socketClient.getStream(), ref getResponse);

            string fileName = sessionVars.path + getResponse.fileInfo.folder + getResponse.fileInfo.filename;

            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }
            try
            {
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(sessionVars.path + getResponse.fileInfo.folder));
                System.IO.File.WriteAllBytes(fileName, getResponse.fileDump);
            }
            catch (Exception wEcx)
            {
                Logging.WriteToLog(wEcx.Message);

            }
            return;
        }
        public void saveNewCopy()
        {

            proto_client.GetList getList = new proto_client.GetList();
            getList.fileList = new List<proto_client.FileToGet>();
            proto_client.FileToGet fileToGet = new proto_client.FileToGet();
            proto_client.GetResponse getResponse = new proto_client.GetResponse();

            fileToGet.fid = Int32.Parse(fileInfoContainer[0]);
            fileToGet.rev = Int32.Parse(fileInfoContainer[1]);
            getList.fileList.Add(fileToGet);
            getList.n = 1;

            proto_client.GetListWrapper(sessionVars.socketClient.getStream(), ref getList);
            proto_client.GetResponseWrapper(sessionVars.socketClient.getStream(), ref getResponse);

            string fileName = sessionVars.path + getResponse.fileInfo.folder + getResponse.fileInfo.filename;
            fileName = MakeUnique(fileName, fileInfoContainer[1]);

            try
            {
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(sessionVars.path + getResponse.fileInfo.folder));
                File.WriteAllBytes(fileName, getResponse.fileDump);
                syncNewfile(fileName, getResponse.fileInfo.md5);
            }
            catch (Exception wEcx)
            {
                Console.WriteLine(wEcx.Message);
            }
            return;
        }

        internal void setFileInfoContainer(string[] fileInfo)
        {
            fileInfoContainer = fileInfo;
        }
    }
}
