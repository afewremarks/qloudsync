//   SparkleShare, a collaboration and sharing tool.
//   Copyright (C) 2010  Hylke Bons <hylkebons@gmail.com>
//
//   This program is free software: you can redistribute it and/or modify
//   it under the terms of the GNU General Public License as published by
//   the Free Software Foundation, either version 3 of the License, or
//   (at your option) any later version.
//
//   This program is distributed in the hope that it will be useful,
//   but WITHOUT ANY WARRANTY; without even the implied warranty of
//   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//   GNU General Public License for more details.
//
//   You should have received a copy of the GNU General Public License
//   along with this program. If not, see <http://www.gnu.org/licenses/>.


using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Threading;
using System.Security.Cryptography;


namespace QloudSync {

    public abstract class SparkleControllerBase {

        public SparkleRepoBase [] Repositories {
            get {
                lock (this.repo_lock)
                    return this.repositories.GetRange (0, this.repositories.Count).ToArray ();
            }
        }

        public bool RepositoriesLoaded { get; private set;}

        private List<SparkleRepoBase> repositories = new List<SparkleRepoBase> ();
        public string FoldersPath { get; private set; }

        public double ProgressPercentage = 0.0;
        public string ProgressSpeed      = "";


        public event ShowSetupWindowEventHandler ShowSetupWindowEvent = delegate { };
        public delegate void ShowSetupWindowEventHandler (PageType page_type);

        public event Action ShowAboutWindowEvent = delegate { };
        public event Action ShowEventLogWindowEvent = delegate { };

        public event FolderFetchedEventHandler FolderFetched = delegate { };
        public delegate void FolderFetchedEventHandler (string remote_url, string [] warnings);
        
        public event FolderFetchErrorHandler FolderFetchError = delegate { };
        public delegate void FolderFetchErrorHandler (string remote_url, string [] errors);
        
        public event FolderFetchingHandler FolderFetching = delegate { };
        public delegate void FolderFetchingHandler (double percentage);
        
        public event Action FolderListChanged = delegate { };


        public event Action OnIdle = delegate { };
        public event Action OnSyncing = delegate { };
        public event Action OnError = delegate { };



        public event NotificationRaisedEventHandler NotificationRaised = delegate { };
        public delegate void NotificationRaisedEventHandler (SparkleChangeSet change_set);

        public event AlertNotificationRaisedEventHandler AlertNotificationRaised = delegate { };
        public delegate void AlertNotificationRaisedEventHandler (string title, string message);


        public bool FirstRun {
            get {
                return this.config.User.Email.Equals ("Unknown");
            }
        }

        public List<string> Folders {
            get {
                List<string> folders = this.config.Folders;
                return folders;
            }
        }

        public string ConfigPath {
            get {
                return this.config.LogFilePath;
            }
        }

        public SparkleUser CurrentUser {
            get {
                return this.config.User;
            }

            set {
                this.config.User = value;
            }
        }

        public bool NotificationsEnabled {
            get {
                string notifications_enabled = this.config.GetConfigOption ("notifications");

                if (string.IsNullOrEmpty (notifications_enabled)) {
                    this.config.SetConfigOption ("notifications", bool.TrueString);
                    return true;

                } else {
                    return notifications_enabled.Equals (bool.TrueString);
                }
            }
        }


        public abstract string EventLogHTML { get; }
        public abstract string DayEntryHTML { get; }
        public abstract string EventEntryHTML { get; }

        // Path where the plugins are kept
        public abstract string PluginsPath { get; }

        // Enables SparkleShare to start automatically at login
        public abstract void CreateStartupItem ();

        // Installs the sparkleshare:// protocol handler
        public abstract void InstallProtocolHandler ();

        // Adds the SparkleShare folder to the user's
        // list of bookmarked places
        public abstract void AddToBookmarks ();

        // Creates the SparkleShare folder in the user's home folder
        public abstract bool CreateSparkleShareFolder ();

        // Opens the SparkleShare folder or an (optional) subfolder
        public abstract void OpenFolder (string path);
        
        // Opens a file with the appropriate application
        public abstract void OpenFile (string path);
        
        // Opens a file with the appropriate application
        public abstract void OpenWebsite (string url);


        private Config config;
        private SparkleFetcher fetcher;
        private FileSystemWatcher watcher;
        private Object repo_lock        = new Object ();
        private Object check_repos_lock = new Object ();


        public SparkleControllerBase ()
        {
            string app_data_path = Environment.GetFolderPath (Environment.SpecialFolder.ApplicationData);
            string config_path   = Path.Combine (app_data_path, "qloudsync");
            
            this.config                 = new Config (config_path, "config.xml");

            Config.DefaultConfig = this.config;
            FoldersPath                 = this.config.FoldersPath;
        }


        public virtual void Initialize ()
        {
           
            SparklePlugin.PluginsPath = PluginsPath;
            InstallProtocolHandler ();

            // Create the SparkleShare folder and add it to the bookmarks
            if (CreateSparkleShareFolder ())
                AddToBookmarks ();
            Console.WriteLine (this.config.FullPath);

            if (FirstRun) {
                this.config.SetConfigOption ("notifications", bool.TrueString);
            }
        }


        public void UIHasLoaded ()
        {
            if (FirstRun) {
                ShowSetupWindow (PageType.Login);

                /*new Thread (() => {
                    string keys_path     = Path.GetDirectoryName (SparkleConfig.DefaultConfig.FullPath);
                    string key_file_name = DateTime.Now.ToString ("yyyy-MM-dd HH\\hmm");
                    
                    string [] key_pair = SparkleKeys.GenerateKeyPair (keys_path, key_file_name);
                    SparkleKeys.ImportPrivateKey (key_pair [0]);
                    
                    //string link_code_file_path = Path.Combine (Program.Controller.FoldersPath, "Your link code.txt");
                    
                    // Create an easily accessible copy of the public
                    // key in the user's SparkleShare folder
                   // File.Copy (key_pair [1], link_code_file_path, true);
                    
                }).Start ();*/

            } else {
                new Thread (() => {
                    CheckRepositories ();
                    RepositoriesLoaded = true;
                    FolderListChanged ();
                    UpdateState ();

                }).Start ();
            }
        }


        private void AddRepository (string folder_path)
        {

            SparkleRepoBase repo = null;
            string folder_name   = Path.GetFileName (folder_path);
            string backend       = this.config.GetBackendForFolder (folder_name);
            backend = "SQ";
            if (repositories.Where (r => r.Name == "QloudSync").Any())
                return;
            try {

                Type t = Type.GetType ("SparkleLib." + backend + ".SparkleRepo, SparkleLib." + backend); 
                repo = (SparkleRepoBase) Activator.CreateInstance ( t,
                                                                   new object [] { folder_path, this.config }

                    );
            } catch (Exception e ){
                Console.WriteLine (e.InnerException);
                     SparkleLogger.LogInfo ("Controller",
                    "Failed to load '" + backend + "' backend for '" + folder_name + "': " + e.Message);

                return;
            }

            repo.ChangesDetected += delegate {
                UpdateState ();
            };

            repo.SyncStatusChanged += delegate (SyncStatus status) {
                if (status == SyncStatus.Idle) {
                    ProgressPercentage = 0.0;
                    ProgressSpeed      = "";
                }

                UpdateState ();
            };

            repo.ProgressChanged += delegate (double percentage, string speed) {
                ProgressPercentage = percentage;
                ProgressSpeed      = speed;

                UpdateState ();
            };
         
            repo.NewChangeSet += delegate (SparkleChangeSet change_set) {
                if (NotificationsEnabled)
                    NotificationRaised (change_set);
            };

            repo.ConflictResolved += delegate {
                if (NotificationsEnabled)
                    AlertNotificationRaised ("Conflict detected",
                        "Don't worry, SparkleShare made a copy of each conflicting file.");
            };
          
            this.repositories.Add (repo);

            repo.Initialize ();
        }


        private void RemoveRepository (string folder_path)
        {
            for (int i = 0; i < this.repositories.Count; i++) {
                SparkleRepoBase repo = this.repositories [i];

                if (repo.LocalPath.Equals (folder_path)) {
                    repo.Dispose ();
                    this.repositories.Remove (repo);
                    repo = null;

                    return;
                }
            }
        }


        private void CheckRepositories ()
        {
            lock (this.check_repos_lock) {
                string path = this.config.FoldersPath;
                if (config.User.Name == "empty")
                    AddRepository (path);


                FolderListChanged ();

            }
        }


        // Fires events for the current syncing state
        private void UpdateState ()
        {
            bool has_unsynced_repos = false;

            foreach (SparkleRepoBase repo in Repositories) {
                if (repo.Status == SyncStatus.SyncDown || repo.Status == SyncStatus.SyncUp || repo.IsBuffering) {
                    OnSyncing ();
                    return;

                } else if (repo.HasUnsyncedChanges) {
                    has_unsynced_repos = true;
                }
            }

            if (has_unsynced_repos)
               OnError ();
            else
                OnIdle ();
        }


        private void ClearFolderAttributes (string path)
        {
            if (!Directory.Exists (path))
                return;

            string [] folders = Directory.GetDirectories (path);

            foreach (string folder in folders)
                ClearFolderAttributes (folder);

            string [] files = Directory.GetFiles(path);

            foreach (string file in files)
                if (!IsSymlink (file))
                    File.SetAttributes (file, FileAttributes.Normal);
        }


        private bool IsSymlink (string file)
        {
            FileAttributes attributes = File.GetAttributes (file);
            return ((attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint);
        }


        public void OnFolderActivity (object o, FileSystemEventArgs args)
        {
            if (args != null && args.FullPath.EndsWith (".xml") &&
                args.ChangeType == WatcherChangeTypes.Created) {
                return;

            } else {
                if (Directory.Exists (args.FullPath) && args.ChangeType == WatcherChangeTypes.Created)
                    return;
                
                CheckRepositories ();
            }
        }

        public void StartFetcher (string address, string remote_path, bool fetch_prior_history)
        {
            string tmp_path = this.config.TmpPath;

			if (!Directory.Exists (tmp_path)) {
                Directory.CreateDirectory (tmp_path);
                File.SetAttributes (tmp_path, File.GetAttributes (tmp_path) | FileAttributes.Hidden);
            }
            this.fetcher = new SparkleFetcher (address, remote_path);

            this.fetcher.Failed += delegate {
                FolderFetchError (this.fetcher.RemoteUrl.ToString (), this.fetcher.Errors);
                StopFetcher ();
            };
            
            this.fetcher.ProgressChanged += delegate (double percentage) {
                FolderFetching (percentage);
            };

            this.fetcher.Start ();
        }


        public void StopFetcher ()
        {
            this.fetcher.Stop ();

            if (Directory.Exists (this.fetcher.TargetFolder)) {
                try {
                    Directory.Delete (this.fetcher.TargetFolder, true);
                    SparkleLogger.LogInfo ("Controller", "Deleted " + this.fetcher.TargetFolder);

                } catch (Exception e) {
                    SparkleLogger.LogInfo ("Controller",
                        "Failed to delete '" + this.fetcher.TargetFolder + "': " + e.Message);
                }
            }

            this.fetcher.Dispose ();
            this.fetcher = null;

            this.watcher.EnableRaisingEvents = true;
        }

        public void FinishFetcher ()
        {
            this.watcher.EnableRaisingEvents = false;

            this.fetcher.Complete ();
            string canonical_name = Path.GetFileNameWithoutExtension (this.fetcher.RemoteUrl.AbsolutePath);
            canonical_name = canonical_name.Replace ("-crypto", "");

            bool target_folder_exists = Directory.Exists (
                Path.Combine (this.config.FoldersPath, canonical_name));

            // Add a numbered suffix to the name if a folder with the same name
            // already exists. Example: "Folder (2)"
            int suffix = 1;
            while (target_folder_exists) {
                suffix++;
                target_folder_exists = Directory.Exists (
                    Path.Combine (this.config.FoldersPath, canonical_name + " (" + suffix + ")"));
            }

            string target_folder_name = canonical_name;

            if (suffix > 1)
                target_folder_name += " (" + suffix + ")";

            string target_folder_path = Path.Combine (this.config.FoldersPath, target_folder_name);

            try {
                ClearFolderAttributes (this.fetcher.TargetFolder);
                Directory.Move (this.fetcher.TargetFolder, target_folder_path);

            } catch (Exception e) {
                SparkleLogger.LogInfo ("Controller", "Error moving directory: " + e.Message);
                this.watcher.EnableRaisingEvents = true;
                return;
            }

            this.config.AddFolder (target_folder_name, this.fetcher.Identifier,
                this.fetcher.RemoteUrl.ToString ());

            FolderFetched (this.fetcher.RemoteUrl.ToString (), this.fetcher.Warnings.ToArray ());
            //while (!QloudSync.QloudSyncPlugin.InitRepo) {
            //}
            AddRepository (target_folder_path);
            FolderListChanged ();

            this.fetcher.Dispose ();
            this.fetcher = null;

            this.watcher.EnableRaisingEvents = true;
        }

        public void ShowSetupWindow (PageType page_type)
        {
            ShowSetupWindowEvent (page_type);
        }


        public void ShowAboutWindow ()
        {
            ShowAboutWindowEvent ();
        }


        public void ShowEventLogWindow ()
        {
            ShowEventLogWindowEvent ();
        }


        public void OpenSparkleShareFolder ()
        {
            OpenFolder (this.config.FoldersPath);
        }


        public void OpenSparkleShareFolder (string name)
        {
            OpenFolder (new SparkleFolder (name).FullPath);
        }


        public void ToggleNotifications () {
            bool notifications_enabled = this.config.GetConfigOption ("notifications").Equals (bool.TrueString);
            this.config.SetConfigOption ("notifications", (!notifications_enabled).ToString ());
        }


        // Format a file size nicely with small caps.
        // Example: 1048576 becomes "1 ᴍʙ"
        public string FormatSize (double byte_count)
        {
            if (byte_count >= 1099511627776)
                return String.Format ("{0:##.##} ᴛʙ", Math.Round (byte_count / 1099511627776, 1));
            else if (byte_count >= 1073741824)
                return String.Format ("{0:##.##} ɢʙ", Math.Round (byte_count / 1073741824, 1));
            else if (byte_count >= 1048576)
                return String.Format ("{0:##.##} ᴍʙ", Math.Round (byte_count / 1048576, 0));
            else if (byte_count >= 1024)
                return String.Format ("{0:##.##} ᴋʙ", Math.Round (byte_count / 1024, 0));
            else
                return byte_count.ToString () + " bytes";
        }


        public virtual void Quit ()
        {
            foreach (SparkleRepoBase repo in Repositories)
                repo.Dispose ();

            Environment.Exit (0);
        }
    }
}
