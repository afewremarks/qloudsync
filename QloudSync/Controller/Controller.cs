using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

using MonoMac.Foundation;
using MonoMac.AppKit;
using MonoMac.ObjCRuntime;
using GreenQloud.Synchrony;

 

namespace GreenQloud {

	public class Controller{

        public IconController StatusIcon;
        LocalSynchronizer localSynchronizer;
        RemoteSynchronizer remoteSynchronizer;
        BacklogSynchronizer backlogSynchronizer;

        public double ProgressPercentage = 0.0;
        public string ProgressSpeed      = "";
        
        
        public event ShowSetupWindowEventHandler ShowSetupWindowEvent = delegate { };
        public delegate void ShowSetupWindowEventHandler (PageType page_type);
        
        public event Action ShowAboutWindowEvent = delegate { };
        public event Action ShowEventLogWindowEvent = delegate { };
        public event Action ShowTransferWindowEvent = delegate { };

        
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

        public Controller () : base ()
        {
            NSApplication.Init ();
        }


        public void Initialize ()
        {
            localSynchronizer = LocalSynchronizer.GetInstance();
            remoteSynchronizer = RemoteSynchronizer.GetInstance();
            backlogSynchronizer = BacklogSynchronizer.GetInstance();

            localSynchronizer.SyncStatusChanged += HandleSyncStatusChanged;
            remoteSynchronizer.SyncStatusChanged += HandleSyncStatusChanged;
            backlogSynchronizer.SyncStatusChanged +=HandleSyncStatusChanged;

            CreateConfigFolder();

            if (CreateHomeFolder ())
                AddToBookmarks ();
         }

        public void UIHasLoaded ()
        {
            if (FirstRun) {
                ShowSetupWindow (PageType.Login);
                foreach (string f in Directory.GetFiles(RuntimeSettings.ConfigPath))
                    File.Delete (f);

            } else {
                InitializeSynchronizers();
            }
        }
       

        private void InitializeSynchronizers ()
        {
            if(!FirstRun)
                GreenQloud.Synchrony.BacklogSynchronizer.GetInstance ().Start();
            localSynchronizer.Start();
            remoteSynchronizer.Start();
                        
            localSynchronizer.ProgressChanged += delegate (double percentage, double speed) {
                ProgressPercentage = percentage;
                ProgressSpeed      = speed.ToString();
                
                UpdateState ();
            };

        }

        void HandleSyncStatusChanged (SyncStatus status)
        {
            if (status == SyncStatus.IDLE)
            {
                ProgressPercentage = 0.0;
                ProgressSpeed = string.Empty;
            }
            UpdateState ();
        }
        
        
        // Fires events for the current syncing state
        private void UpdateState ()
        {
           if (localSynchronizer.Status == SyncStatus.DOWNLOADING || localSynchronizer.Status == SyncStatus.UPLOADING ||
                remoteSynchronizer.Status == SyncStatus.DOWNLOADING || remoteSynchronizer.Status == SyncStatus.UPLOADING  
                ) {

                OnSyncing ();
            } else {
                OnIdle ();
            }
        }
        


        public void SyncStart ()
        {

            localSynchronizer.Finished += () => FinishFetcher();
            localSynchronizer.Failed += delegate {
                FolderFetchError (localSynchronizer.Errors);
                SyncStop();
            };
            
            localSynchronizer.ProgressChanged += delegate (double percentage, double time) {
                FolderFetching (percentage, time);
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
            new Thread (() => CreateStartupItem ()).Start ();
            InitializeSynchronizers ();
        }

        public void CreateStartupItem ()
        {
            // There aren't any bindings in MonoMac to support this yet, so
            // we call out to an applescript to do the job
            System.Diagnostics.Process process = new System.Diagnostics.Process ();
            process.StartInfo.FileName               = "osascript";
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute        = false;
            
            process.StartInfo.Arguments = "-e 'tell application \"System Events\" to " +
                "make login item at end with properties {path:\"" + MonoMac.Foundation.NSBundle.MainBundle.BundlePath + "\", hidden:false}'";
            
            process.Start ();
            process.WaitForExit ();
            
            Logger.LogInfo ("Controller", "Added " + MonoMac.Foundation.NSBundle.MainBundle.BundlePath + " to login items");
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
            OpenFolder (RuntimeSettings.HomePath);
        }

        public void ShowTransferWindow ()
        {
            ShowTransferWindowEvent ();
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
            Process.GetProcessesByName("QloudSync")[0].Kill();
            Environment.Exit (0);
        }

		public void AddToBookmarks ()
        {
//            NSMutableDictionary sidebar_plist = NSMutableDictionary.FromDictionary (
//                NSUserDefaults.StandardUserDefaults.PersistentDomainForName ("com.apple.sidebarlists"));
//
//            // Go through the sidebar categories
//            foreach (NSString sidebar_category in sidebar_plist.Keys) {
//
//                // Find the favorites
//                if (sidebar_category.ToString ().Equals ("favorites")) {
//
//                    // Get the favorites
//                    NSMutableDictionary favorites = NSMutableDictionary.FromDictionary(
//                        (NSDictionary) sidebar_plist.ValueForKey (sidebar_category));
//
//                    // Go through the favorites
//                    foreach (NSString favorite in favorites.Keys) {
//
//                        // Find the custom favorites
//                        if (favorite.ToString ().Equals ("VolumesList")) {
//
//                            // Get the custom favorites
//                            NSMutableArray custom_favorites = (NSMutableArray) favorites.ValueForKey (favorite);
//
//                            NSMutableDictionary properties = new NSMutableDictionary ();
//                            properties.SetValueForKey (new NSString ("1935819892"), new NSString ("com.apple.LSSharedFileList.TemplateSystemSelector"));
//
//                            NSMutableDictionary new_favorite = new NSMutableDictionary ();
//                            new_favorite.SetValueForKey (new NSString (GlobalSettings.ApplicationName),  new NSString ("Name"));
//
//                            new_favorite.SetValueForKey (NSData.FromString ("ImgR SYSL fldr"),  new NSString ("Icon"));
//
//                            new_favorite.SetValueForKey (NSData.FromString (RuntimeSettings.HomePath),
//                                new NSString ("Alias"));
//
//                            new_favorite.SetValueForKey (properties, new NSString ("CustomItemProperties"));
//
//                            // Add to the favorites
//                            custom_favorites.Add (new_favorite);
//                            favorites.SetValueForKey ((NSArray) custom_favorites, new NSString (favorite.ToString ()));
//                            sidebar_plist.SetValueForKey (favorites, new NSString (sidebar_category.ToString ()));
//                        }
//                    }
//
//                }
//            }
//
//            NSUserDefaults.StandardUserDefaults.SetPersistentDomain (sidebar_plist, "com.apple.sidebarlists");
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
			NSWorkspace.SharedWorkspace.OpenFile (path);
		}
		
        
        public void OpenWebsite (string url)
        {
            NSWorkspace.SharedWorkspace.OpenUrl (new NSUrl (url));
        }

        private TransferResponseList<TransferResponse> recentTransfers;
        public TransferResponseList<TransferResponse> RecentsTransfers {
            get {
                if(recentTransfers == null)
                    recentTransfers = new TransferResponseList<TransferResponse>();
                return recentTransfers;
            }
        }

        public class TransferResponseList <TransferResponse> : System.Collections.Generic.List<TransferResponse>
        {
            public event EventHandler OnAdd;

            
            public new void Add (TransferResponse item)
            {
                if (null != OnAdd) {
                    OnAdd (this, null);                
                }
                base.Add (item);
            }

        } 
   	}
}
