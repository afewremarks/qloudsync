using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using IWshRuntimeLibrary;
using GreenQloud.Synchrony;

 

namespace GreenQloud {

	public class Controller{

        LocalSynchronizer localSynchronizer;
        RemoteSynchronizer remoteSynchronizer;
        BacklogSynchronizer backlogSynchronizer;


        public double ProgressPercentage = 0.0;
        public string ProgressSpeed      = "";


        public event ShowSetupWindowEventHandler ShowSetupWindowEvent = delegate { };
        public delegate void ShowSetupWindowEventHandler();
        
        public event Action ShowAboutWindowEvent = delegate { };
        public event Action ShowEventLogWindowEvent = delegate { };
        
        public event FolderFetchedEventHandler FolderFetched = delegate { };
        public delegate void FolderFetchedEventHandler (string [] warnings);
        
        public event FolderFetchErrorHandler FolderFetchError = delegate { };
        public delegate void FolderFetchErrorHandler (string [] errors);
        
        public event FolderFetchingHandler FolderFetching = delegate { };
        public delegate void FolderFetchingHandler (double percentage, double time);

        public event Action OnIdle = delegate { };
        public event Action OnSyncing = delegate { };
        public event Action OnError = delegate { };

        bool FirstRun = RuntimeSettings.FirstRun;
        private Thread firstRunBacklogSynchronizerThread;


        public void Initialize ()
        {
            if (this.CreateHomeFolder())
            {
                this.AddToBookmarks();
                this.SetIconFolder();
            }

            this.CreateConfigFolder();
            localSynchronizer = LocalSynchronizer.GetInstance();
            remoteSynchronizer = RemoteSynchronizer.GetInstance();
            backlogSynchronizer = BacklogSynchronizer.GetInstance();
            localSynchronizer.SyncStatusChanged += HandleSyncStatusChanged;
            remoteSynchronizer.SyncStatusChanged += HandleSyncStatusChanged;
            backlogSynchronizer.SyncStatusChanged += HandleSyncStatusChanged;
        }

        public void UIHasLoaded ()
        {
            if (FirstRun) {
                this.ShowSetupWindow();
                foreach (string f in Directory.GetFiles(RuntimeSettings.ConfigPath))
                    System.IO.File.Delete (f);

            } else {
                InitializeSynchronizers();
            }
        }

        private void InitializeSynchronizers ()
        {
            if (!FirstRun)
                GreenQloud.Synchrony.BacklogSynchronizer.GetInstance().Start();

            localSynchronizer.Start();
            remoteSynchronizer.Start();
        }

        void HandleSyncStatusChanged (SyncStatus status)
        {
            if (status == SyncStatus.IDLE)
            {
                ProgressPercentage = 0.0;
                ProgressSpeed = "";
            }
            UpdateState ();
        }
        
        
        // Fires events for the current syncing state
        private void UpdateState ()
        {
           if (localSynchronizer.Status == SyncStatus.DOWNLOADING || localSynchronizer.Status == SyncStatus.UPLOADING ||
                remoteSynchronizer.Status == SyncStatus.DOWNLOADING || remoteSynchronizer.Status == SyncStatus.UPLOADING  
                ) 
                OnSyncing ();
             else
                OnIdle ();
        }
        


        public void SyncStart ()
        {
            localSynchronizer.Finished += () => FinishFetcher();
            localSynchronizer.Failed += delegate
            {
                FolderFetchError(localSynchronizer.Errors);
                SyncStop();
            };

            localSynchronizer.ProgressChanged += delegate(double percentage, double time)
            {
                FolderFetching(percentage, time);
            };
            localSynchronizer.FirstLoad();
            FinishFetcher();
        }

        public void SyncStop ()
        {
            localSynchronizer.Stop ();
        }        
        

        public void FinishFetcher ()
        {  
            Logger.LogInfo ("Controller", "First load sucessfully");
            FolderFetched (localSynchronizer.Warnings);
            InitializeSynchronizers ();
        }
        
        public void ShowSetupWindow ()
        {
            ShowSetupWindowEvent();
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
            OpenFolder (RuntimeSettings.HomePath);
        }
        
        public void ToggleNotifications () {
            Prefferences.NotificationsEnabled = !Prefferences.NotificationsEnabled;
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
            Environment.Exit (0);
        }

		public void AddToBookmarks ()
        {
            string favoritesFolder = string.Format(@"{0}\Links\{1}.lnk", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), GlobalSettings.ApplicationName);
            IWshShell wsh = new WshShellClass();
            IWshShortcut shortcut = (IWshShortcut) wsh.CreateShortcut (favoritesFolder);
            shortcut.TargetPath = RuntimeSettings.HomePath;
            shortcut.Save();
		}

        public void SetIconFolder()
        {

        }

		public bool CreateHomeFolder ()
		{
            if (!Directory.Exists (RuntimeSettings.HomePath)) {
                Directory.CreateDirectory (RuntimeSettings.HomePath);
                return true;

            } else {
                return false;
            }
		}

        void CreateConfigFolder ()
        {

            if (!Directory.Exists (RuntimeSettings.ConfigPath)) 
                Directory.CreateDirectory (RuntimeSettings.ConfigPath);
                
        }

		public void OpenFolder (string path)
		{
			
		}
		
        
        public void OpenWebsite (string url)
        {
            
        }
   	}
}