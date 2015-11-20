using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Windows;
using ProtoBuf;
using System.Threading;
using System.Data;
using System.IO;
using System.Security.Cryptography;
//using ProtoBuf.Data;

namespace SynchBox_Client
{
    public static partial class proto_client
    {
        public static void do_test(NetworkStream netStream, int n, CancellationToken ct)
        {
            try
            {
                //TODO

                //ListLast
                //ListAll
                //GetSync id

                string basepath = "C:\\backup\\temp";
                string rand = RandomString(4);

                string temp_rand = "\\" + rand + "\\";
                string temp_rand_restore = "\\" + rand + "_restore\\";

                Directory.CreateDirectory(basepath + temp_rand);
                Directory.CreateDirectory(basepath + temp_rand_restore);

                Logging.WriteToLog("AcquireLock:"+ LockAcquireWrapper(netStream).ToString());
                //folder /temp/RAND/
                int session = BeginSessionWrapper(netStream);
                //Begin Session
                //Add 15 01_RAND.txt 15_RAND.txt Files
                int i = 0;
                string filename;
                string text;
                string bff;
                //basepath
                //folder
                //filename

                //bff (basepath+folder+filename)
                AddOk addOk;
                string folder = temp_rand;

                FileListItem[] fileItemList = new FileListItem[16];
                for (i = 0; i < 16; i++)
                    fileItemList[i] = new FileListItem();

                for (i = 1; i <= 15; i++)
                {
                    //create file
                    filename = i.ToString() + "_" + rand + ".txt";
                    bff = basepath + folder + filename;
                    var fileStream = File.Create(bff);
                    fileStream.Close();

                    //write random string
                    text = RandomString(20);
                    File.WriteAllText(bff, text);
                    //create add struct & populate
                    Add add = new Add();

                    add.filename = filename;
                    add.folder = folder;
                    add.fileDump = File.ReadAllBytes(bff);

                    addOk = AddWrapper(netStream, ref add);
                    fileItemList[i].fid = addOk.fid;
                    fileItemList[i].rev = addOk.rev;
                    Logging.WriteToLog(addOk.ToString());

                }

                //end session
                EndSessionWrapper(netStream, session);
                Logging.WriteToLog("Lock:" + LockReleaseWrapper(netStream).ToString());


                Logging.WriteToLog("AcquireLock:" + LockAcquireWrapper(netStream).ToString());
                Logging.WriteToLog("AcquireLock:" + LockAcquireWrapper(netStream).ToString());

                session = BeginSessionWrapper(netStream);
                //begin session
                //update 03-07_RAND.txt
                UpdateOk updateOk;
                for (i = 3; i < 8; i++)
                {
                    //create file
                    filename = i.ToString() + "_" + rand + ".txt";
                    bff = basepath + folder + filename;
                    //write random string
                    text = RandomString(20);
                    File.WriteAllText(bff, text);
                    //create add struct & populate

                    Update update = new Update();

                    update.fid = fileItemList[i].fid;
                    update.fileDump = File.ReadAllBytes(bff);

                    updateOk = UpdateWrapper(netStream, ref update);
                    fileItemList[i].rev = updateOk.rev;
                    Logging.WriteToLog(updateOk.ToString());
                }

                //delete 11-14_RAND.txt

                DeleteOk deleteOk;
                for (i = 11; i <= 14; i++)
                {
                    //create file
                    filename = i.ToString() + "_" + rand + ".txt";
                    bff = basepath + folder + filename;
                    //write random string
                    //text = RandomString(20);
                    //File.WriteAllText(bff, text);
                    //create add struct & populate

                    Delete delete = new Delete();


                    delete.fid = fileItemList[i].fid;

                    deleteOk = DeleteWrapper(netStream, ref delete);
                    fileItemList[i].fid = -1;
                    Logging.WriteToLog(deleteOk.ToString());
                }

                EndSessionWrapper(netStream, session);
                Logging.WriteToLog("ReleaseLock:" + LockReleaseWrapper(netStream).ToString());
                Logging.WriteToLog("ReleaseLock:" + LockReleaseWrapper(netStream).ToString());

                //end session

                //folder /temp/RAND_restore/
                //getlastlist
                Logging.WriteToLog(ListRequestLastWrapper(netStream).ToString());
                Logging.WriteToLog(ListRequestAllWrapper(netStream).ToString());

                GetList getList = new GetList();
                getList.fileList = new List<FileToGet>();
                n = 0;
                for (i = 1; i <= 15; i++)
                {
                    FileToGet fileToGet = new FileToGet();
                    if (fileItemList[i].fid > 0)
                    {
                        n++;
                        fileToGet.fid = fileItemList[i].fid;
                        fileToGet.rev = fileItemList[i].rev;
                        getList.fileList.Add(fileToGet);
                    }
                }
                getList.n = n;

                GetListWrapper(netStream, ref getList);
                GetResponse getResponse = new GetResponse();
                folder = temp_rand_restore;
                for (i = 0; i < n; i++)
                {
                    GetResponseWrapper(netStream, ref getResponse);

                    filename = getResponse.fileInfo.fid + "_" + rand + ".txt";
                    bff = basepath + folder + filename;
                    var fileStream = File.Create(bff);
                    fileStream.Close();

                    File.WriteAllBytes(bff, getResponse.fileDump);
                }

            }
            catch (Exception e)
            {
                Logging.WriteToLog(e.ToString());
            }

        }

        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}