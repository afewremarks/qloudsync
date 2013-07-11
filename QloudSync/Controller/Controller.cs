using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

using MonoMac.Foundation;
using MonoMac.AppKit;
using MonoMac.ObjCRuntime;
using GreenQloud.Synchrony;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using GreenQloud.Persistence.SQLite;

 

namespace GreenQloud {

    
    public enum ERROR_TYPE{
        NULL,
        DISCONNECTION,
        ACCESS_DENIED,
        FATAL_ERROR
    }

    public class Controller {

        public static int Contador{
            set; get;
        }
        public new event ProgressChangedEventHandler ProgressChanged = delegate { };
        public new delegate void ProgressChangedEventHandler (double percentage, double time);


        public IconController StatusIcon;
        private LocalEventsSynchronizer localSynchronizer;
        private RemoteEventsSynchronizer remoteSynchronizer;
        private RecoverySynchronizer recoverySynchronizer;
        private SynchronizerResolver synchronizerResolver;

        public double ProgressPercentage = 0.0;
        public string ProgressSpeed      = "";
        
        
        public event ShowSetupWindowEventHandler ShowSetupWindowEvent = delegate { };
        public delegate void ShowSetupWindowEventHandler (PageType page_type);
        
        public event Action ShowAboutWindowEvent = delegate { };
        public event Action ShowEventLogWindowEvent = delegate { };
        public event Action ShowTransferWindowEvent = delegate { };

        
        public event FolderFetchedEventHandler FolderFetched = delegate { };
        public delegate void FolderFetchedEventHandler ();
        
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

        private bool disconected = false;
        private bool loadedSynchronizers = false;

        Thread checkConnection;

        bool FirstRun = RuntimeSettings.FirstRun;

        public Controller () : base ()
        {
            NSApplication.Init ();
        }


        public void Initialize ()
        {
            ErrorType = ERROR_TYPE.NULL;
            checkConnection = new Thread (delegate() {
                while(true){
                    try{
                        bool hasCon = CheckConnection();
                        if(hasCon){
                            if(disconected || ErrorType == ERROR_TYPE.DISCONNECTION){
                                if(loadedSynchronizers){
                                    disconected = false;
                                    HandleReconnection(); 
                                    ErrorType = ERROR_TYPE.NULL;
                                } else {
                                    disconected = false;
                                    InitializeSynchronizers();
                                    ErrorType = ERROR_TYPE.NULL;                                 

                                }
                            }
                        } else {
                            if(!disconected) {
                                disconected = true;
                                HandleDisconnection();
                            }
                        }
                        Thread.Sleep (5000);
                    } catch { Logger.LogInfo("ERROR", "Failed to check connection"); };
                }
            });

            CreateConfigFolder();
            UpdateConfigFile ();
            if (CreateHomeFolder ())
                AddToBookmarks ();
         }

        public void UIHasLoaded ()
        {
            checkConnection.Start ();
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
                        File.Delete(RuntimeSettings.DatabaseInfoFile);
                        File.Delete(RuntimeSettings.DatabaseFile);
                    } 
                }
                if(!File.Exists (RuntimeSettings.DatabaseFile)){
                    new Persistence.SQLite.SQLiteDatabase().CreateDataBase();
                    File.WriteAllText(RuntimeSettings.DatabaseInfoFile, RuntimeSettings.DatabaseVersion);
                }
            }

            CalcTimeDiff ();
            if (FirstRun) {
                ShowSetupWindow (PageType.Login);
                foreach (string f in Directory.GetFiles(RuntimeSettings.ConfigPath))
                    File.Delete (f);
                UpdateConfigFile ();
            } else {
                InitializeSynchronizers();
            }
            verifyConfigRequirements ();
        }

        void CalcTimeDiff ()
        {
            bool success = false;
            while(!success){
                try{
                    GlobalDateTime.CalcTimeDiff ();
                    success = true;
                } catch {
                    SQLiteTimeDiffDAO dao = new SQLiteTimeDiffDAO ();
                    if(dao.Count == 0){
                        Logger.LogInfo ("ERROR", "Failed to load server time... attempt to try again.");
                        Thread.Sleep(2000);
                    } else {
                        Logger.LogInfo ("WARNING", "Failed to load server time... using previous information.");
                        success = true;
                    }
                }
            }
        }

        void verifyConfigRequirements ()
        {
            try{
                ConfigFile.Read("InstanceID");
            }catch {
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

        public void StopSynchronizers () 
        {
            synchronizerResolver.Stop();
            recoverySynchronizer.Stop();
            localSynchronizer.Stop();
            remoteSynchronizer.Stop();
            Logger.LogInfo ("INFO", "Synchronizers Stoped!");
        }
        public void InitializeSynchronizers (bool initRecovery = false)
        {
            Thread startSync;
            startSync = new Thread (delegate() {
                try{
                    Logger.LogInfo ("INFO", "Initializing Synchronizers!");

                    synchronizerResolver = SynchronizerResolver.GetInstance();
                    recoverySynchronizer = RecoverySynchronizer.GetInstance();
                    remoteSynchronizer = RemoteEventsSynchronizer.GetInstance();
                    localSynchronizer = LocalEventsSynchronizer.GetInstance();

                    if(initRecovery){
                       recoverySynchronizer.Start();
                        while (!((RecoverySynchronizer)recoverySynchronizer).StartedSync)
                            Thread.Sleep(1000);
                       synchronizerResolver.Start (); 

                        while(!recoverySynchronizer.FinishedSync){
                            Thread.Sleep (1000);
                        }
                    } else {
                        synchronizerResolver.Start ();
                    }
                    localSynchronizer.Start();
                    remoteSynchronizer.Start();


                    loadedSynchronizers = true;
                    Logger.LogInfo ("INFO", "Synchronizers Ready!");
                    OnIdle ();
                }catch (Exception e){
                    Console.WriteLine (e.Message);
                }
            });
            startSync.Start ();                    
        }

        public void HandleSyncStatusChanged ()
        {
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
            FirstLoad();
            FinishFetcher();
        }

        public enum START_STATE{
            NULL = 0,
            LOAD_START = 1,
            LOAD_DONE = 2,
            CALCULATING_START = 3,
            SYNC_START = 4,
            CALCULATING_DONE = 5,
            SYNC_DONE = 6
        }

        public START_STATE StartState {
            get;
            set;
        }
        public void FirstLoad()
        {
            try {
                InitializeSynchronizers(true);
                StartState = START_STATE.LOAD_START;
                while(recoverySynchronizer == null)
                    Thread.Sleep(1000);
                StartState = START_STATE.LOAD_DONE;


                StartState = START_STATE.CALCULATING_START;
                StartState = START_STATE.SYNC_START;

                while(!((RecoverySynchronizer)recoverySynchronizer).FinishedSync)
                    Thread.Sleep(1000);
                StartState = START_STATE.CALCULATING_DONE;

                while(loadedSynchronizers == false)
                    Thread.Sleep(1000);

                Thread.Sleep(5000);

                Console.WriteLine(synchronizerResolver.EventsToSync);
                while(synchronizerResolver.EventsToSync > 0)
                    Thread.Sleep(1000);
                StartState = START_STATE.SYNC_DONE;





            }catch (Exception e) {                
                Logger.LogInfo ("Initial Sync Error", e.Message+"\n "+e.StackTrace);
            }
        }

        public void FinishFetcher ()
        {  
            Logger.LogInfo ("Controller", "First load sucessfully");
            FolderFetched ();
            new Thread (() => CreateStartupItem ()).Start ();
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


        public void HandleReconnection(){
            OnIdle();
            if(remoteSynchronizer != null)
            remoteSynchronizer.Start ();
            if(synchronizerResolver != null)
            synchronizerResolver.Start ();
        }

        public void HandleDisconnection(){
            ErrorType = ERROR_TYPE.DISCONNECTION;
            OnError ();
            if(remoteSynchronizer != null)
                recoverySynchronizer.Stop ();
            if(remoteSynchronizer != null)
                remoteSynchronizer.Stop ();
            if(synchronizerResolver != null)
                synchronizerResolver.Stop ();
        }

        private bool CheckConnection ()
        {
            bool hasConnection = false;
            try{
                Ping pingSender = new Ping ();
                if(pingSender.Send(GlobalSettings.StorageHost).Status == IPStatus.Success)//TODO ADD ALL HOSTS TO PING
                    hasConnection = true;
            } catch {
                return false;
            }
            return hasConnection;
        }

        public void HandleError(){
            ErrorType = ERROR_TYPE.FATAL_ERROR;
            OnError ();
            StopSynchronizers ();
            Thread.Sleep (5000);
            InitializeSynchronizers ();
        }

        public ERROR_TYPE ErrorType {
            set;
            get;
        }
   	}
}
