using System;
using System.Linq;

using System.IO;
using System.Collections.Generic;
using GreenQloud.Repository;
using GreenQloud.Util;
using System.Collections;
using System.Threading;


namespace GreenQloud.Synchrony
{
    public class LocalSynchronizer :  Synchrony.Synchronizer
    {
        private static LocalSynchronizer instance;
        private List<StorageQloudObject> PendingFiles;
        public static bool Initialized;
        Thread threadFullLoad;
        Thread threadSynchronize;

        private System.Timers.Timer remote_timer;
        
        protected List<string> warnings = new List<string> ();
        protected List<string> errors   = new List<string> ();
        
        public event Action Failed = delegate { };
         
        public event FinishedEventHandler Finished = delegate { };
        public delegate void FinishedEventHandler ();
        public new event ProgressChangedEventHandler ProgressChanged = delegate { };
        public new delegate void ProgressChangedEventHandler (double percentage, double time);

        private LocalSynchronizer ()
        { 
            Status = SyncStatus.IDLE;
            Stoped = false;
            threadFullLoad = new Thread(FullLoad);
            threadSynchronize = new Thread(Synchronize);
            remote_timer =  new System.Timers.Timer () { Interval = GlobalSettings.IntervalBetweenChecksRemoteRepository };        

            remote_timer.Elapsed += (object sender, System.Timers.ElapsedEventArgs e)=>{
                if (BacklogSynchronizer.GetInstance().Status == SyncStatus.IDLE)
                {
                    new Thread(Synchronize).Start();
                }

            };
            remote_timer.Disposed += (object sender, EventArgs e) => Console.WriteLine("Dispose");

            Initialized = false;
            LastSyncTime = DateTime.Now;
        }
        
        public static LocalSynchronizer GetInstance()
        {
            if (instance == null)
                instance = new LocalSynchronizer();            
            
            return instance;
        }

        #region implemented abstract members of Synchronizer

        public override void Start ()
        {
            Logger.LogInfo ("LocalSynchronizer", "Start LocalSynchronizer");
            if (!remote_timer.Enabled)
                remote_timer.Start ();
        }

        public override void Pause ()
        {
            Logger.LogInfo ("LocalSynchronizer", "Start LocalSynchronizer");
            remote_timer.Stop ();
        }

        public override void Stop ()
        {
            Logger.LogInfo ("LocalSynchronizer", "Stop LocalSynchronizer");
            Stoped = true;
            if (remoteRepo.CurrentTransfer.StorageQloudObject != null) {

                string file = remoteRepo.CurrentTransfer.StorageQloudObject.FullLocalName;
            
                if (File.Exists (file)) {                
                    File.Delete (file);                
                }
            }
            controller = 0;
            Status = SyncStatus.IDLE;
            remote_timer.Stop();
        }

        #endregion

        int controller;

        protected double Percent {
            set; get;
        }
        
        protected double Speed {
            set; get;
        }
        
        protected double TimeRemaining {
            set; get;
        }
        
        protected void ClearDownloadIndexes()
        {
            Percent = 0;
            Speed = 0;
            TimeRemaining = 0;
        }

        public double Size {
            set; get;
        }
        
        
        public long BytesTransferred {
            get{
                return remoteRepo.FullSizeTransfer;
            }
        }

        public void FirstLoad ()
        {
            ++controller;
            if (Status == SyncStatus.IDLE && controller == 1) {
                ++controller;   
                try {
                    Done = false;
                    ClearDownloadIndexes ();

                    if (threadFullLoad.ThreadState == ThreadState.Stopped)
                        threadFullLoad.Join();
                    if (threadFullLoad.ThreadState != ThreadState.Running){
                        threadFullLoad = new Thread(FullLoad);
                    }
                    threadFullLoad.Start ();

                    double lastSize = 0;
                    DateTime lastTime = DateTime.Now;
                    TimeRemaining = 0;
                    while (Percent < 100) {
                        if (Done)
                            break;
                        
                        DateTime time = DateTime.Now;
                        double size = Size;
                        double transferred = BytesTransferred;
                        
                        if (size != 0) {   
                            Percent = (transferred / size) * 100;
                            double diffSeconds = time.Subtract (lastTime).TotalMilliseconds;
                            if (diffSeconds != 0) {
                                double diffSize = transferred - lastSize;
                                double sizeRemaining = size - transferred;
                                double dTimeRemaninig = (sizeRemaining / diffSize) / (diffSeconds / 1000);
                                dTimeRemaninig = Math.Round (dTimeRemaninig, 0);
                                TimeRemaining = dTimeRemaninig;
                            }
                        }
                        lastSize = transferred;
                        lastTime = time;
                        ProgressChanged (Percent, TimeRemaining);
                        Thread.Sleep (1000);
                    }
                } catch (Exception e) {

                    Logger.LogInfo ("DownloadController", e.Message+"\n "+e.StackTrace);
                }
                
                //TODO
                Status = SyncStatus.IDLE;
                controller = 0;                
            }
        }

        bool Stoped {
            get;
            set;
        }

        protected void FullLoad ()
        {
            Stoped = false;
            Status = SyncStatus.VERIFING;
            remoteRepo.FullSizeTransfer = 0;
            ChangesInLastSync = new List<Change>();
            List<StorageQloudObject> remoteFiles = remoteRepo.Files;
            List<StorageQloudObject> remoteFolders = remoteRepo.Folders;
            CalculateDownloadSize(remoteFiles);
            Logger.LogInfo("LocalSynchronizer","Init First Load");
            foreach (StorageQloudObject remoteFile in remoteFiles) {
                if(Stoped)
                    return;
                if (remoteFile.IsIgnoreFile)
                    continue;
                Status = SyncStatus.DOWNLOADING;
                ChangesInLastSync.Add(new Change(remoteFile, WatcherChangeTypes.Created));
                Logger.LogInfo ("LocalSynchronizer", string.Format("File {0} was added in list of changes of LocalSynchronizer and will be downloaded.", remoteFile.AbsolutePath));
                remoteRepo.DownloadFull (remoteFile);            
                BacklogSynchronizer.GetInstance().AddFile(remoteFile);

                Status = SyncStatus.IDLE;
            }

            foreach(StorageQloudObject folder in remoteFolders){
                if(Stoped)
                    return;
                if(!Directory.Exists (folder.FullLocalName)){
                    ChangesInLastSync.Add(new Change(folder, WatcherChangeTypes.Created));
                    Directory.CreateDirectory(folder.FullLocalName);
                    BacklogSynchronizer.GetInstance().AddFile(folder);

                    Status = SyncStatus.IDLE;
                }
            }

            LastSyncTime = DateTime.Now;
            Done = true;
        }

        public override void Synchronize ()
        {
            Done = false;    
            DateTime initTime = DateTime.Now;
            ChangesInLastSync = new List<GreenQloud.Repository.Change>();

            Initialize();
            PendingFiles = RemoteChanges;
            SynchronizeUpdates ();
            LastSyncTime = initTime;
            Done = true;            
            Status = SyncStatus.IDLE;
        }

        protected void SynchronizeUpdates ()
        {
            List<StorageQloudObject> deletedFiles = new List<StorageQloudObject> ();
            Status = SyncStatus.VERIFING;


            if (PendingFiles.Count != 0) {
                foreach (StorageQloudObject remoteFile in PendingFiles) {
                    if (RemoteSynchronizer.GetInstance ().ChangesInLastSync.Any (c => c.File.AbsolutePath == remoteFile.AbsolutePath && c.Event == WatcherChangeTypes.Created)) {
                        RemoteSynchronizer.GetInstance ().ChangesInLastSync.Remove (new Change (remoteFile, WatcherChangeTypes.Created));
                        continue;
                    }
                    if (RemoteSynchronizer.GetInstance ().PendingChanges.Any (f => f.AbsolutePath == remoteFile.AbsolutePath))
                        continue;
                    bool create = !LocalRepo.Exists (remoteFile);
                    if (!create) {
                        //update
                        if (!remoteFile.IsSync) {
                            Logger.LogInfo ("LocalSynchronizer", string.Format ("File \"{0}\" is outdated and will be dowloaded", remoteFile.FullLocalName));
                            remoteRepo.SendLocalVersionToTrash (remoteFile);
                            Program.Controller.RecentsTransfers.Add (remoteRepo.Download (remoteFile));
                            ChangesInLastSync.Add (new Change (remoteFile, WatcherChangeTypes.Created));
                            BacklogSynchronizer.GetInstance ().EditFileByName (remoteFile);
                        }
                    } else {
                        //create
                        if (LocalRepo.Files.Any(f=>f.LocalMD5Hash == remoteFile.RemoteMD5Hash))
                        {
                            StorageQloudObject localFile =  LocalRepo.Files.First(f=> f.LocalMD5Hash == remoteFile.RemoteMD5Hash);
                            if (File.Exists (localFile.FullLocalName)){                                
                                LocalRepo.CreatePath (remoteFile.FullLocalName);
                                if (!remoteFiles.Any (r => r.AbsolutePath == localFile.AbsolutePath)){
                                    Logger.LogInfo ("LocalSynchronizer", string.Format ("Moving \"{0}\" to \"{1}\" in local repo", localFile.FullLocalName, remoteFile.FullLocalName));
                                    File.Move (localFile.FullLocalName, remoteFile.FullLocalName);
                                    Program.Controller.RecentsTransfers.Add(new TransferResponse(remoteFile, GreenQloud.TransferType.REMOVE)); 
                                    ChangesInLastSync.Add (new Change (remoteFile, WatcherChangeTypes.Deleted));

                                }
                                else{        
                                    Logger.LogInfo ("LocalSynchronizer", string.Format ("Copying \"{0}\" to \"{1}\" in local repo", localFile.FullLocalName, remoteFile.FullLocalName));
                                    File.Copy (localFile.FullLocalName, remoteFile.FullLocalName);
                                }
                                Status = SyncStatus.DOWNLOADING;
                                Program.Controller.RecentsTransfers.Add (new TransferResponse(remoteFile, TransferType.DOWNLOAD));
                                BacklogSynchronizer.GetInstance ().AddFile (remoteFile);
                                LocalRepo.Files.Add (remoteFile);
                            }
                        }else{
                            Logger.LogInfo ("LocalSynchronizer", string.Format ("File \"{0}\" not exists in local repo and will be downloaded", remoteFile.FullLocalName));
                            Status = SyncStatus.DOWNLOADING;
                            Program.Controller.RecentsTransfers.Add (remoteRepo.Download (remoteFile));
                            BacklogSynchronizer.GetInstance ().AddFile (remoteFile);
                            LocalRepo.Files.Add (remoteFile);
                        }
                    }
                }
            }
            foreach (StorageQloudObject folder in remoteRepo.Folders) {               
                if (RemoteSynchronizer.GetInstance ().PendingChanges.Any (f => f.AbsolutePath+"/" == folder.AbsolutePath))
                    continue;
                if(!Directory.Exists(folder.FullLocalName)){
                    Status = SyncStatus.DOWNLOADING;
                    LocalRepo.CreateFolder(folder);
                    TransferResponse transfer = new TransferResponse(folder, GreenQloud.TransferType.DOWNLOAD);
                    transfer.Status = TransferStatus.DONE;
                    Program.Controller.RecentsTransfers.Add (transfer);
                    BacklogSynchronizer.GetInstance().AddFile(folder);
                    ChangesInLastSync.Add(new Change(folder, WatcherChangeTypes.Created));
                }
            }
            foreach (StorageQloudObject localFile in LocalRepo.Files) {
                bool deleted = false;
                if (localFile.IsAFolder) {
                    deleted = !remoteFolders.Any(rf => rf.AbsolutePath==localFile.AbsolutePath || rf.AbsolutePath.Contains(localFile.AbsolutePath));
                } else {
                    deleted = !remoteFiles.Any (rf => rf.AbsolutePath == localFile.AbsolutePath);
                }
                
                if (deleted)
                    deletedFiles.Add (localFile);
            }
            foreach (StorageQloudObject deletedFile in deletedFiles) { 
                if (deletedFile.IsIgnoreFile)
                    continue;
                if (RemoteSynchronizer.GetInstance().PendingChanges.Any (f=> f.AbsolutePath == deletedFile.AbsolutePath))
                    continue;
                if (RemoteSynchronizer.GetInstance().ChangesInLastSync.Any (c=> c.File.AbsolutePath == deletedFile.AbsolutePath && c.Event == WatcherChangeTypes.Created))
                    continue;
                if (ChangesInLastSync.Any (c => c.File.AbsolutePath == deletedFile.AbsolutePath))
                    continue;
                Logger.LogInfo ("LocalSynchronizer", string.Format ("\"{0}\" was deleted in StorageQloud", deletedFile.FullLocalName));
                Logger.LogInfo ("LocalSynchronizer", string.Format ("Deleting \"{0}\" in local repo", deletedFile.FullLocalName));
                LocalRepo.Delete (deletedFile);
                TransferResponse transfer = new TransferResponse(deletedFile, GreenQloud.TransferType.REMOVE);
                transfer.Status = TransferStatus.DONE;

                Program.Controller.RecentsTransfers.Add(transfer); 
                ChangesInLastSync.Add (new Change (deletedFile, WatcherChangeTypes.Deleted));
            }
        }


        void CalculateDownloadSize (List<StorageQloudObject> remoteFiles)
        {
            foreach (StorageQloudObject remoteFile in remoteFiles) {
                if (!remoteFile.IsIgnoreFile)
                    Size += remoteFile.AsS3Object.Size;
            }
        }



        public List<StorageQloudObject> RemoteChanges {
            get {

                TimeSpan diffClocks = remoteRepo.DiffClocks;
                DateTime referencialClock = LastSyncTime.Subtract (diffClocks);               
                List<StorageQloudObject> list = remoteRepo.Files.Where (rf => Convert.ToDateTime (rf.AsS3Object.LastModified).Subtract (referencialClock).TotalSeconds > 0).ToList<StorageQloudObject>();

                return list;
            }
        }
        
        public bool HasRemoteChanges {
            get {
                return RemoteChanges.Count != 0;
            }
        }

        
        public string [] Warnings {
            get {
                return this.warnings.ToArray ();
            }
        }
        
        public string [] Errors {
            get {
                return this.errors.ToArray ();
            }
        }
   }
}