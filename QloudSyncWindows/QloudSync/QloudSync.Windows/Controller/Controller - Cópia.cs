using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using IWshRuntimeLibrary;

 

namespace GreenQloud {

	public class Controller{

        DownloadController downloadController;
        UploadController uploadController;
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


        public void Initialize ()
        {
            this.CreateConfigFolder();

            if (this.CreateHomeFolder())
            { 
                this.AddToBookmarks ();
                this.SetIconFolder();
            }
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
            if(!FirstRun)
              GreenQloud.Synchrony.BacklogSynchronizer.GetInstance ().Synchronize ();

            downloadController = DownloadController.GetInstance();
            if(!FirstRun)
                downloadController.Synchronize();

            uploadController = UploadController.GetInstance();

            downloadController.SyncStatusChanged += HandleSyncStatusChanged;
            uploadController.SyncStatusChanged += HandleSyncStatusChanged;
        }

        void HandleSyncStatusChanged (SyncStatus status)
        {
            if (status == SyncStatus.Idle)
            {
                ProgressPercentage = 0.0;
                ProgressSpeed = "";
            }
            UpdateState ();
        }
        
        
        // Fires events for the current syncing state
        private void UpdateState ()
        {
            if (downloadController.Status == SyncStatus.Sync || uploadController.Status == SyncStatus.Sync)// repo.IsBuffering
                OnSyncing ();
             else
                OnIdle ();
        }
        


        public void SyncStart ()
        {
            downloadController = DownloadController.GetInstance();

            downloadController.Finished += () => FinishFetcher();
            downloadController.Failed += delegate {
                FolderFetchError (downloadController.Errors);
                SyncStop();
            };
            
            downloadController.ProgressChanged += delegate (double percentage, double time) {
                FolderFetching (percentage, time);
            };

            downloadController.FirstLoad();
            FinishFetcher();
        }

        public void SyncStop ()
        {
            downloadController.Stop ();
        }        
        

        public void FinishFetcher ()
        {  
            Logger.LogInfo ("Controller", "First load sucessfully");
            FolderFetched (downloadController.Warnings);
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
            string favoritesFolder = string.Format(@"{0}\Links\QloudSync.lnk", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
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