using SparkleLib;



using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using System.Threading;
using SQ.IO;
using SQ;
using SQ.Synchrony;
using SQ.Security;
using SQ.Util;
using SQ.Repository;

namespace SparkleLib.SQ
{
    public class SparkleRepo : SparkleRepoBase
    {
        
        
        private OSXFileWatcher fileWatcher;
        private bool enableHasChanges =false;
        private int controlador = 0;

        private new SyncStatus Status
        {
            get; set;
        }

        DateTime LastTimeRepo {
            get;
            set;
        }
        
        public SparkleRepo (string path, SparkleConfig config) : base (path, config)
        {

            MoveToQloudSyncFolder();
            Initialize();
            EnableHasChanges = false;
            fileWatcher = new OSXFileWatcher(LocalRepo.LocalFolder);
            BacklogSynchronizer.GetInstance().Synchronize();

            EnableHasChanges = true;
        }
        
        new void Initialize(){
            DownloadSynchronizer.GetInstance().Synchronized = true;
            UploadSynchronizer.GetInstance().Synchronized = true;
            Credential.URLConnection = base.RemoteUrl.Host;
            QloudSyncPlugin.Connect();
        }

        void MoveToQloudSyncFolder ()
        {
            DirectoryInfo oldFolder = new DirectoryInfo (base.LocalPath);
            string QloudsyncFolder = base.LocalPath.Substring (0, base.LocalPath.IndexOf ("QloudSync") + 9);
            if (oldFolder.Exists) 
            {
                 foreach (DirectoryInfo d in oldFolder.GetDirectories())
                 {
                    if(d.Name != ".tmp" && d.FullName != Path.Combine (QloudsyncFolder, d.Name))
                        Directory.Move (d.FullName, Path.Combine (QloudsyncFolder, d.Name));
                 }
    
                foreach (FileInfo f in oldFolder.GetFiles())
                {
                    if (f.FullName != Path.Combine (QloudsyncFolder, f.Name))
                    System.IO.File.Move (f.FullName, Path.Combine (QloudsyncFolder, f.Name));
                }
                if(oldFolder.Name!="QloudSync")
                oldFolder.Delete ();
            }

            LocalRepo.LocalFolder = QloudsyncFolder;
            DirectoryInfo temp = new DirectoryInfo (Path.Combine (QloudsyncFolder, ".tmp"));
            if (temp.Exists) {
                temp.Delete ();
            }
        }

        public override bool HasUnsyncedChanges {
            get {

                if (EnableHasChanges){
                    ++controlador;

                    if (controlador == 1
                        && DownloadSynchronizer.GetInstance().Synchronized 
                        && UploadSynchronizer.GetInstance().Synchronized 
                        && BacklogSynchronizer.GetInstance().Synchronized 
                        && Status == SyncStatus.Idle 
                        && fileWatcher.IsIdle)
                    {

                        bool hasChanges = HasLocalChanges;
                        if (hasChanges)
                            SyncUp();

                        
                        if (DateTime.Now.Subtract(Repo.LastSyncTime).TotalSeconds >= 30 && Status == SyncStatus.Idle)
                            SyncDown();
                        
                        controlador = 0;
                        return hasChanges;
                    }
                    else {
                        if(!(DownloadSynchronizer.GetInstance().Synchronized && UploadSynchronizer.GetInstance().Synchronized))
                            controlador = 0;
                        return false;
                    }
                }
                else return false;
            }
            
            set {
                
            }
        }

        static bool finished = false;
        
        public override bool SyncDown ()
        {
            try
            {
                if (Status == SyncStatus.Idle)
                {
                    Thread downThread = new Thread (DownThreadMethod);
                    Status = SyncStatus.SyncDown;
                    
                    double percentage = 0;
                    finished = false;
                    downThread.Start();
                    double repo_InitialSize = LocalRepo.Size;

                    while (percentage < 100)
                    {
                        if (finished)
                            break;
                        double downSize = DownloadSynchronizer.SyncSize;
                        double repoSize = LocalRepo.Size-repo_InitialSize;
                        
                        if (downSize != 0)
                            percentage = (repoSize / downSize)*100;
                        base.OnProgressChanged (percentage, "0");
                        Thread.Sleep (1000);
                    }
                    BacklogSynchronizer.GetInstance().Write();
                    LastTimeRepo = DateTime.Now;
                    Status = SyncStatus.Idle;
                }
            }
            catch (Exception e)
            {
                Status = SyncStatus.Idle;
                Logger.LogInfo ("Repo", e);
                return false;
            }
            return true;
        }

        void DownThreadMethod(){
            finished = false;
            finished = DownloadSynchronizer.GetInstance().Synchronize();
            
        }
        
        public override bool SyncUp ()
        {
            try
            {
                if (Status == SyncStatus.Idle)
                {

                    Status = SyncStatus.SyncUp;
                   
                    UpThreadMethod();
                    controlador = 0;
                    Status = SyncStatus.Idle;
                }
            }
            catch(Exception e)
            {
                Logger.LogInfo ("Sync", e);
                return false;
            }
            return true;
        }

        void UpThreadMethod ()
        {
            finished = false;
            if (fileWatcher.IsIdle) {
                finished = UploadSynchronizer.GetInstance ().Synchronize ();
                BacklogSynchronizer.GetInstance ().Write ();
            }
            finished = true;
        }
      
        public override bool HasRemoteChanges {
            get {
                if (EnableHasChanges)
                    return UploadSynchronizer.GetInstance().HasRemoteChanges;
                else 
                    return false;
            }
        }
        
        public override bool HasLocalChanges {
            get {
                if (LocalRepo.PendingChanges == null)
                    return true;
                return LocalRepo.PendingChanges.Count != 0;
            }
        }


        protected List<SparkleChangeSet> GetChangesSetsInternal (string path, int count)
        {
            SparkleUser user = new SparkleUser(local_config.User.Name, local_config.User.Email);
            List<SparkleChangeSet> sparkleChangeSets = new List<SparkleChangeSet> ();
            SparkleChangeSet schange = new SparkleChangeSet(){
                FirstTimestamp = DateTime.Now,
                Timestamp = DateTime.Now,
                Revision = "Fetch",
                Folder = new SparkleFolder("folder"),
                RemoteUrl = new Uri("http://s.greenqloud.com"),
                User = user
            };
            schange.Changes.Add(new SparkleChange(){
                Path        = "remote",
                MovedToPath = "Local",
                Timestamp   = DateTime.Now,
                Type        = SparkleChangeType.Moved
            });
            sparkleChangeSets.Add(schange);
            
            base.ChangeSets = sparkleChangeSets;
            return sparkleChangeSets;
            
        }
        
 
        
        
        public override List<string> ExcludePaths 
        {
            get {
                return new List<string>();
            }
        }
        


        //changed back from restorefile
        public override void RestoreFile (string path, string revision, string target_file_path)
        {

        }
        
        public new string Identifier { get { return ""; } }
        public override string CurrentRevision {get { return ""; } }
        public override double Size {  get { return 0; } }
        public override double HistorySize {  get { return 0; } }
        
        bool EnableHasChanges {
            set {
                this.enableHasChanges = value;
            }get{
                return this.enableHasChanges;
            }
        }

        public override List<SparkleChangeSet> GetChangeSets ()
        {
            return null;
        }

        public override List<SparkleChangeSet> GetChangeSets (string path)
        {
            return GetChangesSetsInternal (path, 1);
        }
    }
}