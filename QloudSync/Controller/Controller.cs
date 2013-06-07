using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

using MonoMac.Foundation;
using MonoMac.AppKit;
using MonoMac.ObjCRuntime;
using GreenQloud.Synchrony;
using System.Collections.Generic;

 

namespace GreenQloud {

    
    public enum ERROR_TYPE{
        DISCONNECTION,
        ACCESS_DENIED
    }

	public class Controller{

        public static int Contador{
            set; get;
        }

        public new event ProgressChangedEventHandler ProgressChanged = delegate { };
        public new delegate void ProgressChangedEventHandler (double percentage, double time);


        public IconController StatusIcon;
        private StorageQloudLocalEventsSynchronizer localSynchronizer;
        private StorageQloudRemoteEventsSynchronizer remoteSynchronizer;
        private StorageQloudBacklogSynchronizer backlogSynchronizer;
        private SynchronizerResolver synchronizerResolver;

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

        protected List<string> warnings = new List<string> ();
        protected List<string> errors   = new List<string> ();
        
        private System.Timers.Timer timer;

        bool FirstRun = RuntimeSettings.FirstRun;

        public Controller () : base ()
        {
            NSApplication.Init ();
        }


        public void Initialize ()
        {
            localSynchronizer = StorageQloudLocalEventsSynchronizer.GetInstance();
            remoteSynchronizer = StorageQloudRemoteEventsSynchronizer.GetInstance();
            backlogSynchronizer = StorageQloudBacklogSynchronizer.GetInstance();
            synchronizerResolver = SynchronizerResolver.GetInstance();

            synchronizerResolver.SyncStatusChanged +=HandleSyncStatusChanged;
            
            this.timer = new System.Timers.Timer (){
                Interval = 10000
            };
            
            timer.Elapsed += (object sender, System.Timers.ElapsedEventArgs e)=>{
                try{
                    InitializeSynchronizers();
                }catch{
                    
                }
            };

            CreateConfigFolder();
            UpdateConfigFile ();

            if (CreateHomeFolder ())
                AddToBookmarks ();
         }

        public void UIHasLoaded ()
        {
            if (!File.Exists (RuntimeSettings.DatabaseFile)){
                if (!Directory.Exists(RuntimeSettings.DatabaseFolder))
                    Directory.CreateDirectory (RuntimeSettings.DatabaseFolder);
                new Persistence.SQLite.SQLiteDatabase().CreateDataBase();
                File.WriteAllText(RuntimeSettings.DatabaseInfoFile, RuntimeSettings.DatabaseVersion);
            } else {
                if (!File.Exists (RuntimeSettings.DatabaseInfoFile)){
                    File.Delete(RuntimeSettings.DatabaseFile);
                }else{
                    string version = File.OpenText(RuntimeSettings.DatabaseInfoFile).ReadLine();
                    if(Double.Parse(version) <  Double.Parse(RuntimeSettings.DatabaseVersion)){
                        //TODO run migrations
                        File.Delete(RuntimeSettings.DatabaseInfoFile);
                        File.Delete(RuntimeSettings.DatabaseFile);
                    } 
                }
                if(!File.Exists (RuntimeSettings.DatabaseFile)){
                    new Persistence.SQLite.SQLiteDatabase().CreateDataBase();
                    File.WriteAllText(RuntimeSettings.DatabaseInfoFile, RuntimeSettings.DatabaseVersion);
                }
            }
            
            if (File.Exists (RuntimeSettings.BacklogFile))
                File.Delete(RuntimeSettings.BacklogFile);
            
            bool available = double.Parse(InitOSInfoString()) >= double.Parse(GlobalSettings.AvailableOSXVersion);
            if(available){
                if (FirstRun) {
                    ShowSetupWindow (PageType.Login);
                    foreach (string f in Directory.GetFiles(RuntimeSettings.ConfigPath))
                        File.Delete (f);

                    UpdateConfigFile ();
                } else {
                    InitializeSynchronizers();
                }
            }
            else{
                var alert = new NSAlert {
                    MessageText = "QloudSync is only available to OSX Mountain Lion.",
                    AlertStyle = NSAlertStyle.Informational
                };
                
                alert.AddButton ("OK");
                
                var returnValue = alert.RunModal();
                Quit ();
            }
            verifyConfigRequirements ();
        }

        void verifyConfigRequirements ()
        {
            try{
                ConfigFile.Read("InstanceID");
            }catch(ConfigurationException e){
                string id = Crypto.Getbase64(ConfigFile.Read("ApplicationName") + Credential.Username + GlobalDateTime.NowUniversalString);
                ConfigFile.Write ("InstanceID", id); 
                ConfigFile.Read("InstanceID");
                Logger.LogInfo ("INFO", "Generated InstanceID: " + id);
            }
        }
        
        [System.Runtime.InteropServices.DllImport("/System/Library/Frameworks/CoreServices.framework/CoreServices")]
        internal static extern short Gestalt(int selector, ref int response);
        static string m_OSInfoString = null;
        static string InitOSInfoString()
        {
            try{
                //const int gestaltSystemVersion = 0x73797376;
                const int gestaltSystemVersionMajor = 0x73797331;
                const int gestaltSystemVersionMinor = 0x73797332;
                const int gestaltSystemVersionBugFix = 0x73797333;
                
                int major = 0;
                int minor = 0;
                int bugFix = 0;
                
                Gestalt(gestaltSystemVersionMajor, ref major);
                Gestalt(gestaltSystemVersionMinor, ref minor);
                Gestalt(gestaltSystemVersionBugFix, ref bugFix);
                
                
                
                Console.WriteLine("Mac OS X/{0}.{1}.{2}", major, minor, bugFix);
                return string.Format ("{0}.{1}", major, minor);}
            catch (Exception e){
                Console.WriteLine (e.Message);
            }
            return "";
        }


        private void InitializeSynchronizers ()
        {
            localSynchronizer.Start();
            remoteSynchronizer.Start();
            synchronizerResolver.Start();
                        
            ProgressChanged += delegate (double percentage, double speed) {
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
            if (synchronizerResolver.SyncStatus == SyncStatus.DOWNLOADING || synchronizerResolver.SyncStatus == SyncStatus.UPLOADING) {
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
            
            ProgressChanged += delegate (double percentage, double time) {
                FolderFetching (percentage, time);
            };
            FirstLoad();
            FinishFetcher();
        }

        public void FirstLoad()
        {
            try {
                backlogSynchronizer.Start();
                while(!backlogSynchronizer.FinishLoad)
                    Thread.Sleep (1000);
                backlogSynchronizer.Stop();


                InitializeSynchronizers();
                Thread.Sleep (1000);

                int eventsToSync = synchronizerResolver.EventsToSync;
                int totalEventsToSync = eventsToSync;

                while(eventsToSync > 0){

                    double percent = 100 - (100*eventsToSync/totalEventsToSync);

                    ProgressChanged (percent , 0.0);
                    eventsToSync = synchronizerResolver.EventsToSync;
                    Thread.Sleep (1000);
                }
            }catch (Exception e) {                
                Logger.LogInfo ("Initial Sync Error", e.Message+"\n "+e.StackTrace);
            }
        }

        public void SyncStop ()
        {
            localSynchronizer.Stop ();
        }        
        

        public void FinishFetcher ()
        {  
            Logger.LogInfo ("Controller", "First load sucessfully");
            FolderFetched (localSynchronizer.Warnings);
            new Persistence.SQLite.SQLiteRepositoryDAO().Create (new GreenQloud.Model.LocalRepository(RuntimeSettings.HomePath));
            new Thread (() => CreateStartupItem ()).Start ();
            //InitializeSynchronizers ();
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
            
            Logger.LogInfo ("Controller", "Added " + MonoMac.Foundation.NSBundle.MainBundle.BundlePath + " to startup items");
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
            Preferences.NotificationsEnabled = !Preferences.NotificationsEnabled;
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

        void UpdateConfigFile ()
        {
            ConfigFile.UpdateConfigFile ();
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


        public void HandleDisconnection(){
            ErrorType = ERROR_TYPE.DISCONNECTION;
            OnError ();
            localSynchronizer.Stop ();
            remoteSynchronizer.Stop ();
            timer.Start ();
        }

        public void HandleAccessDenied ()
        {
            ErrorType = ERROR_TYPE.ACCESS_DENIED;
            OnError ();
            localSynchronizer.Stop ();
            remoteSynchronizer.Stop ();
        }

        public ERROR_TYPE ErrorType {
            set;
            get;
        }
   	}
}
