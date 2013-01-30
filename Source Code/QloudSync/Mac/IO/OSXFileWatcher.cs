using System;
using System.Collections.Generic;
using System.Linq;
using QloudSync.Repository;
using QloudSync.Synchrony;




namespace  QloudSync.IO
{
    public class OSXFileWatcher
    {
        
        public List<System.IO.FileSystemWatcher> watchers = new List<System.IO.FileSystemWatcher>();
        List <string> triggers = new List<string>();
        List<string> eventshandled = new List<string>(); 
        List<File> updatedFilesInDownloadController = new List<File>();
        public DateTime LastTimeChanges {
            get;
            set;
        }
        
        DateTime LastTimeCatch;
        
        public OSXFileWatcher (string repo_address)
        {
            CreateWatcher (repo_address);
            new System.Threading.Thread(ListenEvents).Start();

        }
        
        public void ListenEvents ()
        {
            while (true) {
                if (LastTimeCatch == new DateTime())
                    continue;
                if(Catching)
                    continue;
                if (triggers.Count != 0){
                    string path = triggers [0];
                    if (!eventshandled.Where (eh => eh == triggers[0]).Any ())
                    {
                        if(HandleCreates (path)){
                            eventshandled.Add (triggers[0]);
                            
                            LastTimeChanges = DateTime.Now;
                            triggers.RemoveAt (0);
                        }
                    }
                }
                try{
                    foreach(Change c in UploadController.GetInstance().PendingChanges){
                        if (eventshandled.Where(eh => eh == c.File.FullLocalName && (c.Event == System.IO.WatcherChangeTypes.Created || c.Event == System.IO.WatcherChangeTypes.Renamed)).Any())
                            eventshandled.Remove(c.File.FullLocalName);
                    }
                }
                catch{
                    continue;
                }
            }
        }
        
        public void CreateWatcher (string folder_path)
        {

            if (folder_path.Contains (".app/") || folder_path.EndsWith (".app"))
                return;
            Console.WriteLine (DateTime.Now.ToUniversalTime () + " - Creating a watcher to " + folder_path + "\n");
            System.IO.DirectoryInfo d = new System.IO.DirectoryInfo (folder_path);
            
            System.IO.FileSystemWatcher f = new System.IO.FileSystemWatcher (d.FullName, "*.*");
            f.NotifyFilter = System.IO.NotifyFilters.DirectoryName | System.IO.NotifyFilters.FileName | System.IO.NotifyFilters.LastWrite;
            f.Changed += HandleChanges; 
            f.Deleted += HandleChanges;
            f.Created += HandleChanges;
            f.EnableRaisingEvents = true;
            
            foreach (System.IO.DirectoryInfo dir in d.GetDirectories())
                CreateWatcher (dir.FullName);

            watchers.Add (f);
        }
        
        #region Events
        void HandleChanges (object sender, System.IO.FileSystemEventArgs e)
        {
            if (e==null)
                return;
            LocalFile localFile = new LocalFile (e.FullPath);
            if (localFile.IsIgnoreFile)
            {   
                return;
            }
            Logger.LogInfo("Watcher",string.Format("{0}{1}", e.ChangeType, e.FullPath));

            switch (e.ChangeType) {
            case System.IO.WatcherChangeTypes.Created:
                triggers.Add (e.FullPath);

                LastTimeCatch = DateTime.Now;
                break;
            case System.IO.WatcherChangeTypes.Deleted:
                HandleDelete();
                break;
            case System.IO.WatcherChangeTypes.Changed:
                UploadController.GetInstance().PendingChanges.Add 
                    (new Change(new LocalFile(e.FullPath), e.ChangeType));
                break;
            }
        }
        
        bool HandleCreates (string path)
        { 

            if (path == null) {
                return false;
            }
            //TODO lock
            updatedFilesInDownloadController = DownloadSynchronizer.GetInstance().FilesInLastSync; 
            if (updatedFilesInDownloadController.Count > 0) {
                if (updatedFilesInDownloadController.Where (df => df.FullLocalName == path).Any ())
                    return true;
            }
            
            if (System.IO.Directory.Exists (path)) {
                CreateFolder (path);
            }
            else {  
                LocalFile lf = new LocalFile (path); 
                HandleDelete ();
                CreateFile (lf);
            }
            return true;
        }
        

        void HandleDelete ()
        {
            try {
                foreach (QloudSync.Repository.File lf in LocalRepo.Files) {
                    
                    if (updatedFilesInDownloadController.Where (f => f.AbsolutePath == lf.AbsolutePath).Any ())
                        continue;
                    
                    if (!lf.ExistsInLocalRepo && !lf.Deleted) {
                        //se tiver algum pendingchange antes do delete,apaga
                        List<Change> deprecatedChanges = UploadController.GetInstance().PendingChanges.Where (c => c.File.AbsolutePath == lf.AbsolutePath && c.Event != System.IO.WatcherChangeTypes.Deleted).ToList<Change> ();
                        foreach (Change ch in deprecatedChanges)
                            UploadController.GetInstance().PendingChanges.Remove (ch);
                        lf.Deleted = true;
                        lf.TimeOfLastChange = DateTime.Now;
                        UploadController.GetInstance().PendingChanges.Add 
                            (new Change (lf, System.IO.WatcherChangeTypes.Deleted));
                    }
                }
                UpdateWatchers ();
            } catch (InvalidOperationException) {
                Logger.LogInfo ("Debug", "Collection was modified; HandleDelete");
                HandleDelete();
            }
        }
        
#endregion
        
        void CreateFolder (string folder_path)
        {
            Logger.LogInfo ("Watcher", string.Format("Creating folder {0}", folder_path));
            if (folder_path.Contains (".app/") || folder_path.EndsWith (".app"))
                return;
            CreateWatcher (folder_path);
            LocalFile folder = new LocalFile (folder_path);
            if (System.IO.Directory.GetFiles (folder_path).Count () == 0 && System.IO.Directory.GetDirectories (folder_path).Count () == 0)
                UploadController.GetInstance().PendingChanges.Add 
                    (new Change (folder, System.IO.WatcherChangeTypes.Created));
            else {
                foreach (string fileName in System.IO.Directory.GetFiles(folder_path)) {
                    LocalFile lf = new LocalFile (fileName);
                    if (!lf.IsIgnoreFile)
                        CreateFile (lf);
                }
                foreach (string folderName in System.IO.Directory.GetDirectories(folder_path)) {
                    CreateFolder (folderName);
                }            
            }
            
            LocalRepo.Files.Add (folder); 
        }
        
        void CreateFile (LocalFile file)
        {
            Logger.LogInfo ("Watcher", string.Format("Create {0}",file.FullLocalName));
            UploadController.GetInstance().PendingChanges.Add 
                (new Change (file, System.IO.WatcherChangeTypes.Created));
            LocalRepo.Files.Add (file);
        }
        
        void UpdateWatchers ()
        {
            int c = 0;
            while (c < watchers.Count)
            {
                System.IO.FileSystemWatcher w = watchers[c];
                if(!new System.IO.DirectoryInfo(w.Path).Exists){
                    Console.WriteLine (DateTime.Now.ToUniversalTime ()+" - Remove watcher: "+w.Path);
                    w.Dispose();
                    watchers.Remove (w);
                    w = null;
                    UploadController.GetInstance().PendingChanges.Add(new Change(new Folder(w.Path), System.IO.WatcherChangeTypes.Deleted));
                }
                else c++;
            }
            Console.WriteLine ();
        }

        public bool Catching {
            get {
                return DateTime.Now.Subtract (LastTimeCatch).TotalSeconds < 1;
            }
        }
        
        public bool IsIdle {
            get {
                return DateTime.Now.Subtract (LastTimeChanges).TotalSeconds > 1 && !Catching;
            }
        }

        public void Dispose ()
        {
                while (watchers.Count!=0) {
                    System.IO.FileSystemWatcher w = watchers [0];
                    w.Dispose ();
                    watchers.Remove (w);
                    w = null;
                }
                   }
    }
}

