using GreenQloud;
using GreenQloud.Core;
using GreenQloud.Model;
using GreenQloud.Persistence.SQLite;
using GreenQloud.Repository;
using GreenQloud.Synchrony;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;

namespace GreenQloud
{
    public abstract class AbstractApplicationController : ApplicationController
    {

        public abstract void Initialize();
        public abstract void Quit();
        public abstract void OpenFolder(string path);
        public abstract void OpenWebsite(string url);
        public abstract void CreateMenuItem();
        public abstract void CheckForUpdates();
        public abstract void Alert(string message);

        public event Action ShowAboutWindowEvent = delegate { };
        public event Action ShowEventLogWindowEvent = delegate { };
        public event Action ShowTransferWindowEvent = delegate { };
        public event FolderFetchedEventHandler FolderFetched = delegate { };
        public delegate void FolderFetchedEventHandler();
        public event ShowSetupWindowEventHandler ShowSetupWindowEvent = delegate { };
        public delegate void ShowSetupWindowEventHandler(PageType page_type);

        public event Action OnIdle = delegate { };
        public event Action OnSyncing = delegate { };
        public event Action OnError = delegate { };
        public event Action OnPaused = delegate { };
      
        private Event currentEventDownloading;

        Thread checkConnection;
        bool firstRun = RuntimeSettings.FirstRun;
        private bool disconected = false;
        private bool isPaused = false;

        public enum ERROR_TYPE
        {
            NULL,
            DISCONNECTION,
            ACCESS_DENIED,
            FATAL_ERROR
        }

        public enum PageType
        {
            None,
            Setup,
            Add,
            Invite,
            Syncing,
            Error,
            Finished,
            Tutorial,
            CryptoSetup,
            CryptoPassword,
            Login,
            ConfigureFolders
        }

        public ERROR_TYPE ErrorType
        {
            set;
            get;
        }

        public AbstractApplicationController() {
            if (firstRun)
            {
                foreach (string f in Directory.GetFiles(RuntimeSettings.ConfigPath))
                    File.Delete(f);
                UpdateConfigFile();

            }
            ErrorType = ERROR_TYPE.NULL;
            checkConnection = new Thread(delegate()
                {
                while(true){
                    try{
                        bool hasCon = CheckConnection();
                        if(hasCon){
                            if (disconected || ErrorType == ERROR_TYPE.DISCONNECTION)
                            {
                                disconected = false;
                                ErrorType = ERROR_TYPE.NULL;
                                HandleReconnection();
                            }
                        } else {
                            if (!disconected)
                            {
                                disconected = true;
                                HandleDisconnection();
                            }
                        }
                        Thread.Sleep (5000);
                    } catch { Logger.LogInfo("ERROR", "Failed to check connection"); };
                }
            });
        }

        public void UnlinkAccount()
        {
            KillSynchronizers();
            PhysicalRepositoryController controller = new PhysicalRepositoryController(null);
            controller.DeleteFile(RuntimeSettings.CredentialPath);
            Alert("Your account are now unlinked, you can run QloudSync again to login with your accout or a new one.");
            Quit();
        }

        public void MoveSQFolder(string pathTo)
        {
            SQLiteRepositoryDAO repoDao = new SQLiteRepositoryDAO();
            LocalRepository repo = repoDao.RootRepo();
            PhysicalRepositoryController controller = new PhysicalRepositoryController(repo);
            KillSynchronizers();
            controller.MoveDir(repo.Path, pathTo);
            repo.Path = pathTo;
            repoDao.Update(repo);
            InitializeSynchronizers(false);
            Program.Controller.Alert("SQFolder moved to "+ pathTo);
        }

        void CalcTimeDiff()
        {
            bool success = false;
            while (!success)
            {
                try
                {
                    GlobalDateTime.CalcTimeDiff();
                    success = true;
                }
                catch
                {
                    SQLiteTimeDiffDAO dao = new SQLiteTimeDiffDAO();
                    if (dao.Count == 0)
                    {
                        Logger.LogInfo("ERROR", "Failed to load server time... attempt to try again.");
                        Thread.Sleep(2000);
                    }
                    else
                    {
                        Logger.LogInfo("WARNING", "Failed to load server time... using previous information.");
                        success = true;
                    }
                }
            }
        }

        public void InitializeSynchronizers(LocalRepository repo, bool recovery)
        {
            SQLiteRepositoryDAO repoDAO = new SQLiteRepositoryDAO();
            Thread startSync;
            startSync = new Thread(delegate()
            {
                try
                {
                    Logger.LogInfo("INFO", "Initializing Synchronizers!");
                    SynchronizerUnit unit = SynchronizerUnit.GetByRepo(repo);
                    if (unit == null)
                    {
                        unit = new SynchronizerUnit(repo);
                        SynchronizerUnit.Add(repo, unit);
                    }
                    unit.InitializeSynchronizers(recovery);
                    Logger.LogInfo("INFO", "Synchronizers Ready!");
                    ErrorType = ERROR_TYPE.NULL;
                    OnIdle();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            });
            startSync.Start();
        }

        public void KillSynchronizers()
        {
            SQLiteRepositoryDAO repoRaven = new SQLiteRepositoryDAO();
            List<LocalRepository> repos = repoRaven.AllActived;
            foreach (LocalRepository repo in repos)
            {
                KillSynchronizers(repo);
            }
        }

        public void StopSynchronizers()
        {
            SQLiteRepositoryDAO repoRaven = new SQLiteRepositoryDAO();
            List<LocalRepository> repos = repoRaven.AllActived;
            foreach (LocalRepository repo in repos)
            {
                StopSynchronizers(repo);
            }
        }

        public void StopSynchronizers(LocalRepository repo)
        {
            SynchronizerUnit unit = SynchronizerUnit.GetByRepo(repo);
            if (unit != null)
            {
                unit.StopAll();
                Logger.LogInfo("INFO", "Synchronizers Stoped!");
            }
            else
            {
                Logger.LogInfo("INFO", "Cannot stop synchronizers! [repository not found]");
            }
        }

        public void KillSynchronizers(LocalRepository repo)
        {
            SynchronizerUnit unit = SynchronizerUnit.GetByRepo(repo);
            if (unit != null)
            {
                unit.KillAll();
                Logger.LogInfo("INFO", "Synchronizers Killed nicely!");
            }
            else
            {
                Logger.LogInfo("INFO", "Cannot stop synchronizers! [repository not found]");
            }
        }

        public void InitializeSynchronizers(bool recovery)
        {
            SQLiteRepositoryDAO repoRaven = new SQLiteRepositoryDAO();
            List<LocalRepository> repos = repoRaven.AllActived;
            foreach (LocalRepository repo in repos)
            {
                CreateRepoFolder(repo);
                InitializeSynchronizers(repo, recovery);
            }
        }

        public void UIHasLoaded()
        {
            new Thread(() => CreateMenuItem()).Start();
            checkConnection.Start();

            //TODO REFATORAR URGENTEs
            if (!File.Exists(RuntimeSettings.DatabaseFile))
            {
                if (!Directory.Exists(RuntimeSettings.DatabaseFolder))
                    Directory.CreateDirectory(RuntimeSettings.DatabaseFolder);
                SQLiteDatabase.Instance().CreateDataBase();
                File.WriteAllText(RuntimeSettings.DatabaseInfoFile, RuntimeSettings.DatabaseVersion);
            }
            else
            {
                if (!File.Exists(RuntimeSettings.DatabaseInfoFile))
                {
                    File.Delete(RuntimeSettings.DatabaseFile);
                }
                else
                {
                    string version = File.OpenText(RuntimeSettings.DatabaseInfoFile).ReadLine();
                    if (Double.Parse(version) < Double.Parse(RuntimeSettings.DatabaseVersion))
                    {
                        File.Delete(RuntimeSettings.DatabaseInfoFile);
                        File.Delete(RuntimeSettings.DatabaseFile);
                    }
                }
                if (!File.Exists(RuntimeSettings.DatabaseFile))
                {
                    SQLiteDatabase.Instance().CreateDataBase();
                    File.WriteAllText(RuntimeSettings.DatabaseInfoFile, RuntimeSettings.DatabaseVersion);
                }
            }

            CalcTimeDiff();
            if (firstRun)
            {
                ShowSetupWindow(PageType.Login);
            }
            else
            {
                SQLiteRepositoryDAO rpoDAO = new SQLiteRepositoryDAO();
                if (rpoDAO.AllActived.Count == 0)
                {
                    ShowSetupWindow(PageType.ConfigureFolders);
                } else {
                    InitializeSynchronizers(false);
                }
            }
            verifyConfigRequirements();
        }

        void verifyConfigRequirements()
        {
            try
            {
                ConfigFile.GetInstance().Read("InstanceID");
            }
            catch
            {
                string id = Crypto.Getbase64(ConfigFile.GetInstance().Read("ApplicationName") + Credential.Username + GlobalDateTime.NowUniversalString);
                ConfigFile.GetInstance().Write("InstanceID", id);
                ConfigFile.GetInstance().Read("InstanceID");
                Logger.LogInfo("INFO", "Generated InstanceID: " + id);
            }
        }

        private void UpdateState()
        {
            if (!isPaused)
            {
                if (SynchronizerUnit.AnyWorking())
                {
                    OnSyncing();
                }
                else
                {
                    OnIdle();
                }
            }
        }

        public void PauseSync()
        {
            if (isPaused)
            {
                isPaused = false;
                SynchronizerUnit.ReconnectResolver();
                OnSyncing();
                Console.Out.WriteLine("Resolver Synchronizer Resumed!");
            }
            else
            {
                isPaused = true;
                SynchronizerUnit.DisconnectResolver();
                OnPaused();
                Console.Out.WriteLine("Resolver Synchronizer Paused!");
            }
        }

        public void HandleSyncStatusChanged()
        {
            UpdateState();
        }

        public void SyncStart()
        {
            FirstLoad();
            FinishFetcher();
        }

        public void FinishFetcher()
        {
            Logger.LogInfo("Controller", "First load sucessfully");
            FolderFetched();
        }
        
        public void FirstLoad()
        {
            try
            {
                InitializeSynchronizers(true);
            }
            catch (Exception e)
            {
                Logger.LogInfo("Initial Sync Error", e.Message + "\n " + e.StackTrace);
            }
        }

        public void ShowSetupWindow(PageType page_type)
        {
            ShowSetupWindowEvent(page_type);
        }


        public void ShowAboutWindow()
        {
            ShowAboutWindowEvent();
        }


        public void ShowEventLogWindow()
        {
            ShowEventLogWindowEvent();
        }

        protected bool CheckConnection()
        {
            bool hasConnection = false;
            try
            {
                Ping pingSender = new Ping();
                if (pingSender.Send(GlobalSettings.StorageHost).Status == IPStatus.Success)//TODO ADD ALL HOSTS TO PING
                    hasConnection = true;
            }
            catch
            {
                return false;
            }
            return hasConnection;
        }

        public void HandleReconnection()
        {
            OnIdle();
            SynchronizerUnit.ReconnectAll();
        }

        public void HandleDisconnection()
        {
            ErrorType = ERROR_TYPE.DISCONNECTION;
            OnError();
            SynchronizerUnit.DisconnectAll();
        }

        public void HandleError(LocalRepository repo)
        {
            ErrorType = ERROR_TYPE.FATAL_ERROR;
            OnError();
            KillSynchronizers(repo);
            Thread.Sleep(5000);
            InitializeSynchronizers(repo, true);
        }

        public void HandleError()
        {
            ErrorType = ERROR_TYPE.FATAL_ERROR;
            OnError();
            KillSynchronizers();
            Thread.Sleep(5000);
            InitializeSynchronizers(true);
        }

        public void CreateConfigFolder()
        {
            if (!Directory.Exists(RuntimeSettings.ConfigPath))
                Directory.CreateDirectory(RuntimeSettings.ConfigPath);

        }

        public void UpdateConfigFile()
        {
            ConfigFile.GetInstance().UpdateConfigFile();
        }

        public void CreateRepoFolder(LocalRepository repo)
        {
            if (!Directory.Exists(repo.Path))
            {
                Directory.CreateDirectory(repo.Path);
            }
        }

        public void OpenStorageFolder()
        {
            string mainPath = new SQLiteRepositoryDAO().MainActive.Path;
            OpenFolder(mainPath);
        }

        public void OpenStorageQloudWebSite()
        {
            string hash = Crypto.GetHMACbase64(Credential.SecretKey, Credential.PublicKey, true);
            OpenWebsite(string.Format("https://my.greenqloud.com/qloudsync?username={0}&hashValue={1}&returnUrl=/storageQloud", Credential.Username, hash));
        }

        public void OpenResetPasswordWebsite()
        {
            OpenWebsite(string.Format("https://my.greenqloud.com/resetPassword"));
        }

        public bool DatabaseLoaded()
        {
            if (File.Exists(RuntimeSettings.DatabaseFile) && File.Exists(RuntimeSettings.DatabaseInfoFile))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool IsPaused()
        {
            return isPaused;
        }

        public Event GetCurrentEvent()
        {
            return currentEventDownloading;
        }

        public void HandleItemEvent(Event e)
        {
            if (!e.Item.IsFolder)
                currentEventDownloading = e;
            
        }
  
    }
}
