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
using GreenQloud.Model;
using System.Collections;

 

namespace GreenQloud {
    public class Controller : AbstractApplicationController {

        public override void Initialize ()
        {

        }
        public override void CreateMenuItem ()
        {

        }
        public override void CheckForUpdates()
        {

        }
        public override void Alert(string message)
        {

        }

        public override void FirstRunAction ()
        {
            CreateStartupItem ();
        }

        public void CreateStartupItem ()
        {
            // There aren't any bindings in MonoMac to support this yet, so
            // we call out to an applescript to do the job
            System.Diagnostics.Process process = new System.Diagnostics.Process ();
            process.StartInfo.FileName               = "defaults";
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute        = false;

            process.StartInfo.Arguments = "write loginwindow AutoLaunchedApplicationDictionary -array-add '{Path=\""+ MonoMac.Foundation.NSBundle.MainBundle.BundlePath + "\";}'";


            process.Start ();
            process.WaitForExit ();

            Logger.LogInfo ("Controller", "Added " + MonoMac.Foundation.NSBundle.MainBundle.BundlePath + " to startup items");
        }

        public override void Quit ()
        {
            Process.GetProcessesByName("QloudSync")[0].Kill();
            Environment.Exit (0);
        }
        public override void OpenFolder (string path)
        {
            NSWorkspace.SharedWorkspace.OpenFile (path);
        }
        public override void OpenWebsite (string url)
        {
            NSWorkspace.SharedWorkspace.OpenUrl (new NSUrl (url));
        }


        /*
        public int Contador{
            set; get;
        }
        public new event ProgressChangedEventHandler ProgressChanged = delegate { };
        public new delegate void ProgressChangedEventHandler (double percentage,double time);

        public IconController StatusIcon;

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
        public event Action OnPaused = delegate { };

        protected List<string> warnings = new List<string> ();
        protected List<string> errors   = new List<string> ();
        
        private System.Timers.Timer timer;

        private bool disconected = false;
        private bool loadedSynchronizers = false;
        private bool isPaused = false;

        Thread checkConnection;

        bool firstRun = RuntimeSettings.FirstRun;

        public bool DatabaseLoaded(){
            if (File.Exists (RuntimeSettings.DatabaseFile) && File.Exists (RuntimeSettings.DatabaseInfoFile)) {
                return true;
            } else {
                return false;
            }
        }

        public Controller () : base ()
        {
            UpdateConfigFile ();
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
         }

        public void UIHasLoaded ()
        {
            checkConnection.Start ();
            if (!File.Exists (RuntimeSettings.DatabaseFile)){
                if (!Directory.Exists(RuntimeSettings.DatabaseFolder))
                    Directory.CreateDirectory (RuntimeSettings.DatabaseFolder);
                    SQLiteDatabase.Instance().CreateDataBase();
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
                    SQLiteDatabase.Instance().CreateDataBase();
                    File.WriteAllText(RuntimeSettings.DatabaseInfoFile, RuntimeSettings.DatabaseVersion);
                }
            }

            CalcTimeDiff ();
            if (firstRun) {
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
                ConfigFile.GetInstance().Read("InstanceID");
            }catch {
                string id = Crypto.Getbase64(ConfigFile.GetInstance().Read("ApplicationName") + Credential.Username + GlobalDateTime.NowUniversalString);
                ConfigFile.GetInstance().Write ("InstanceID", id); 
                ConfigFile.GetInstance().Read("InstanceID");
                Logger.LogInfo ("INFO", "Generated InstanceID: " + id);
            }
        }
        
        [System.Runtime.InteropServices.DllImport("/System/Library/Frameworks/CoreServices.framework/CoreServices")]
        internal static extern short Gestalt(int selector, ref int response);
        //static string m_OSInfoString = null;
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

        public void StopSynchronizers (LocalRepository repo) 
        {
            SynchronizerUnit unit = SynchronizerUnit.GetByRepo(repo);
            if (unit != null) {
                unit.StopAll ();
                Logger.LogInfo ("INFO", "Synchronizers Stoped!");
            } else {
                Logger.LogInfo ("INFO", "Cannot stop synchronizers! [repository not found]");
            }
        }
        public void InitializeSynchronizers ( bool initRecovery = false){
            SQLiteRepositoryDAO repoDAO = new SQLiteRepositoryDAO ();
            Hashtable ht = SelectedFoldersConfig.GetInstance().Read ();
            foreach (string folder in ht.Keys) {
                LocalRepository repo = repoDAO.FindOrCreate (folder, ht [folder].ToString ());
                InitializeSynchronizers (repo, initRecovery);
            }
        }
        public void InitializeSynchronizers (LocalRepository repo, bool initRecovery = false)
        {
            SQLiteRepositoryDAO repoDAO = new SQLiteRepositoryDAO ();
            if (initRecovery || repo.Recovering) {
                initRecovery = true;
                repo.Recovering = true;
                repoDAO.Update (repo);
            }
            Thread.Sleep (5000);
            Thread startSync;
            startSync = new Thread (delegate() {
                try {
                    Logger.LogInfo ("INFO", "Initializing Synchronizers!");
                    SynchronizerUnit unit = SynchronizerUnit.GetByRepo(repo);
                    if(unit == null){
                        unit = new SynchronizerUnit(repo);
                        SynchronizerUnit.Add(repo, unit);
                    }
                    unit.InitializeSynchronizers(initRecovery);
                    loadedSynchronizers = true;
                    Logger.LogInfo ("INFO", "Synchronizers Ready!");
                    if (initRecovery || repo.Recovering) {
                        repo.Recovering = false;
                        repoDAO.Update (repo);
                    }
                    ErrorType = ERROR_TYPE.NULL;
                    OnIdle ();
                } catch (Exception e) {
                    Console.WriteLine (e.Message);
                }
            });
            startSync.Start ();
        }

        public void HandleSyncStatusChanged ()
        {
            UpdateState ();
        }

        public bool IsDownloading ()
        {
            return SynchronizerUnit.AnyDownloading ();
        }

        public bool IsUploading ()
        {
            return SynchronizerUnit.AnyUploading ();
        }

        private void UpdateState ()
        {
            if (SynchronizerUnit.AnyWorking()) {
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

        public void FirstLoad()
        {
            try {
                InitializeSynchronizers(true);
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
            process.StartInfo.FileName               = "defaults";
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute        = false;

            process.StartInfo.Arguments = "write loginwindow AutoLaunchedApplicationDictionary -array-add '{Path=\""+ MonoMac.Foundation.NSBundle.MainBundle.BundlePath + "\";}'";

            
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
            //OpenFolder (RuntimeSettings.HomePath);
        }

        public void OpenRepositoryitemFolder (string path)
        {
            OpenFolder (path);
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

        public void UpdateConfigFile ()
        {
            ConfigFile.GetInstance().UpdateConfigFile ();
            SelectedFoldersConfig.GetInstance().UpdateConfigFile ();
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

        public void OpenStorageQloudWebSite(){
            string hash = Crypto.GetHMACbase64 (Credential.SecretKey, Credential.PublicKey, true);
            Program.Controller.OpenWebsite (string.Format ("https://my.greenqloud.com/qloudsync?username={0}&hashValue={1}&returnUrl=/storageQloud", Credential.Username, hash));
        }

        public void HandleReconnection(){
            OnIdle();
            SynchronizerUnit.ReconnectAll ();
        }

        public void HandleDisconnection(){
            ErrorType = ERROR_TYPE.DISCONNECTION;
            OnError ();
            SynchronizerUnit.DisconnectAll ();
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

        public void PauseSync(){
            if (isPaused) {
                isPaused = false;
                SynchronizerUnit.ReconnectResolver ();
                OnSyncing ();
                Console.Out.WriteLine("Resolver Synchronizer Resumed!");
            } else {
                isPaused = true;
                SynchronizerUnit.DisconnectResolver ();
                OnPaused ();
                Console.Out.WriteLine("Resolver Synchronizer Paused!");
            }
        }

        public void HandleError(LocalRepository repo){
            ErrorType = ERROR_TYPE.FATAL_ERROR;
            OnError ();
            StopSynchronizers(repo);
            Thread.Sleep (5000);
            InitializeSynchronizers (repo, true);
        }

        public ERROR_TYPE ErrorType {
            set;
            get;
        }
        */
   	}
}
