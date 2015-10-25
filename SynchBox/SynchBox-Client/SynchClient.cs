﻿using System;
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
        public string monitoredPath = "";
        public int sessionId = -1;

        public async Task StartSyncAsync(NetworkStream netStream, MainWindow.SessionVars sessionVars)
        {
            if (monitoredPath == "")
            {
                getInitInformation(sessionVars);
            }
            else
            {
                //se il monitoredPath e sessionVars.path sono diversi si cambiata la cartella ce si sta monitorando
                //da vedere come gestire
            }
            int syncIdServer = proto_client.GetSynchIdWrapper(netStream);
            if (sessionVars.lastSyncId == -1 || sessionVars.lastSyncId < syncIdServer)
            {
                sessionVars.lastSyncId = syncIdServer;
                remoteFiles = this.populate_dictionary(netStream);
            }
            else
            {
                foreach (KeyValuePair<string, remoteFileInfos> entry in remoteFiles)
                {
                    entry.Value.delete = true;
                }
            }

            sessionId = -1;
            
            findDifference(netStream, sessionVars.path);

            if (sessionId != -1)
            {
                proto_client.EndSessionWrapper(netStream, sessionVars.lastSyncId);
                sessionVars.lastSyncId = sessionId;
            }

        }

        private void getInitInformation(MainWindow.SessionVars sessionVars)
        {
            string path = ".\\conf.ini";
            Dictionary<string, string> information = new Dictionary<string, string>();
            if (!File.Exists(path))
            {
                using (StreamReader sr = File.OpenText(path))
                {
                    string buffer = "";
                    while ((buffer = sr.ReadLine()) != null)
                    {
                        var line = buffer.Split(':');
                        if (line[0].CompareTo(sessionVars.uid_str) != 0)
                        {
                            var info = line[1].Split(' ');
                            information[info[0]] = info[1];
                        }
                    }
                }
                if (information.ContainsKey("monitoredPath"))
                {
                    monitoredPath = information["monitoredPath"];
                }
            }
            else
            {
                using (StreamWriter sw = File.CreateText(path))
                {
                    sw.WriteLine(sessionVars.uid_str+":monitoredPath " + sessionVars.path);
                }
            }
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
                remoteFiles.Add(monitoredPath+fileInfo.folder + fileInfo.filename, tmp);
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
            add.folder = Path.GetDirectoryName(path).Replace(monitoredPath, "")+"\\";
            add.fileDump = File.ReadAllBytes(path);

            addOk = proto_client.AddWrapper(netStream, ref add);

            fileInfo.hash = hash;
            fileInfo.fid = addOk.fid;
            fileInfo.delete = false;
            remoteFiles.Add(path, fileInfo);

        }
        private void checkBeginSession(NetworkStream netStream)
        {
            if (sessionId == -1)
            {
                sessionId = proto_client.BeginSessionWrapper(netStream);
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

        /*
       public static void do_sync(NetworkStream netStream, SessionVars vars, CancellationToken ct)
       {

           //controllo se vars.uid ha già effettuato almeno una sincronizzazione!

           //list last -> server

           //list last response

           //HO filesystem_server

           //se no 
           {
               //foreach item in list
               { 
                   //get

                   //get response

                   //write on disk
               }
           }

           while (!ct.IsCancellationRequested) { 
               //update/populate filesystem struct
               //HO filesystem_client

               //compare_filesystems(server,client);

               //sleep (timesleep)
           }

       }
       */


        /*
        private static DataTable populateFS(ref DataTable dt, string rootFolder){
                dt = new DataTable();
                dt.Clear();
                dt.Columns.Add("uid");   //user_id
                dt.Columns.Add("fid");   //file id
                dt.Columns.Add("cid");   //changeset id 
                dt.Columns.Add("rev");   //revision
                dt.Columns.Add("name");  //filename
                dt.Columns.Add("folder_path");//path
                dt.Columns.Add("md5");   //md5
                
                List<string> search = Directory.GetFiles(rootFolder, "*.*").ToList();

                foreach(string item in search)
                {
                
                        //calculate md5 in bytearray
                        byte[] filehash;

                        using (var md5 = MD5.Create())
                        {
                            using (var stream = File.OpenRead(item))
                            {
                                filehash =  md5.ComputeHash(stream);
                            }
                        }


                }   
    
        }
        */

        /*
        //compare filesystems
        {   
            //ADD A FAST METHOD TO RECOGNISE nochange CASE- for example id sync

            //request changeset #
            //begin new changeset state = not finished!

            //foreach client file
            {
                //if present in the server
                    //scegli il più recente!

                    //se client + recente, update

                    //se server + recente, get last version

                //if not present in the server add

                //if non ho dei file che sono sul server
                    //get di tutti quei file
             
                //SEMPRE
                //fai tipo un update del numero di modifiche apportate al changeset!
                
            }
            
        }
         */
    }
}
