using System;
using System.Collections.Generic;
using System.Linq;
using MonoMac.Foundation;
using MonoMac.AppKit;
using GreenQloud;
using GreenQloud.Model;
using GreenQloud.Repository;
using System.Drawing;
using System.IO;
using GreenQloud.Persistence.SQLite;
using System.Diagnostics;
using System.Timers;
using GreenQloud.Synchrony;

namespace QloudSync
{
    public partial class PreferenceWindowController : MonoMac.AppKit.NSWindowController
    {
        private List<NSButton> remoteFoldersCheckboxes;
        private SQLiteRepositoryDAO repoDao;
        private SQLiteRepositoryIgnoreDAO repoIgnore;
        private List<RepositoryIgnore> ignoreFolders;
        private Timer timer;
       
        //Used to placeholder scroll
        private int count;

        #region Constructors
        // Called when created from unmanaged code
        public PreferenceWindowController (IntPtr handle) : base (handle)
        {
            Initialize ();
        }
        // Called when created directly from a XIB file
        [Export ("initWithCoder:")]
        public PreferenceWindowController (NSCoder coder) : base (coder)
        {
            Initialize ();
        }
        // Call to load from the XIB/NIB file
        public PreferenceWindowController () : base ("PreferenceWindow")
        {
            Initialize ();

            using (var a = new NSAutoreleasePool ())
            {
                NSRunLoop.Main.BeginInvokeOnMainThread (delegate {
                    base.LoadWindow ();
                    loadFolders();
                    //will render for generic 
                    Window.OpenWindow();
                });

            }

        }
        // Shared initialization code
        void Initialize ()
        {   
            count = 0;
            repoDao = new SQLiteRepositoryDAO();
            repoIgnore = new SQLiteRepositoryIgnoreDAO();
            //netTraffic = new NetworkTraffic (Process.GetCurrentProcess().Id);

        }
        #endregion
        //strongly typed window accessor
        public new PreferenceWindow Window {
            get {
                return (PreferenceWindow)base.Window;
            }
        }

        public override void AwakeFromNib ()
        {

            base.AwakeFromNib ();
            //Network Tab
            EventsNumberLabel.StringValue = "Looking for changes...";
            timer = new Timer (5000);
            timer.Elapsed += delegate {
                using (NSAutoreleasePool pool = new NSAutoreleasePool()) {
                    OnItemEvent();
                }
            };
            timer.Start ();

            Window.WillClose += delegate {
                timer.Stop();
                Window.WindowClosed();
            };

            //Selective Sync Tab
            changeFoldersButton.Activated += delegate {

                int size = this.remoteFoldersCheckboxes.Count;
                WaitWindowController wait = new WaitWindowController();
                wait.LoadWindow();

                wait.ShouldCloseDocument = false;
                wait.ShowWindow(this);
                    
                LocalRepository repo = repoDao.RootRepo();
                Program.Controller.KillSynchronizers();
                for(int i = 0; i < size; i++)
                {
                    if(this.remoteFoldersCheckboxes.ElementAt(i).State == NSCellStateValue.Off)
                    {
                        repoIgnore.Create(repo, remoteFoldersCheckboxes.ElementAt(i).Title);
                    }
                    else
                    {
                        repoIgnore.Remove(repo, remoteFoldersCheckboxes.ElementAt(i).Title);
                    }

                }
                Program.Controller.InitializeSynchronizers(true);
                wait.Close();

            };

            moveSQFolderButton.Activated += delegate {
                string path = ChangeSQFolder();

                if(path != RuntimeSettings.SelectedHomePath)
                {
                    if(Program.Controller.Confirm("Are you sure of this? All your files will be moved"))
                    {
                        try
                        {
                            WaitWindowController wait = new WaitWindowController();
                            wait.LoadWindow();
                            wait.ShouldCloseDocument = false;
                            wait.ShowWindow(this);
                            Program.Controller.MoveSQFolder(path);
                            wait.Close();

                        }
                        catch(Exception ex)
                        {
                            Logger.LogInfo("ERROR ON MOVE STORAGEQLOUD FOLDER", ex);
                            Program.Controller.Alert("Cannot move StorageQloud Folder");
                        }
                    }
                }
            };





            //Account Tab
            pathLabel.StringValue = RuntimeSettings.SelectedHomePath;
            usernameLabel.StringValue = Credential.Username;
            versionLabel.StringValue = GlobalSettings.RunningVersion;

            unlinkAccountButton.Activated += delegate {
                try{
                    if( Program.Controller.Confirm("Are you sure you want to continue? You are unlinking your account to " +
                                                   "this computer.") ){
                        Program.Controller.UnlinkAccount ();
                    }
                }catch (Exception ex){
                    Logger.LogInfo("ERROR ON UNLINK ACCOUNT" , ex);
                    Program.Controller.Alert("Cannot unlink accounts, please check your " +
                        "internet connection and try again.");
                }
            };
        }

        //Helpers
        private void loadFolders()
        {
            foreach (NSView view in foldersView.Subviews) {
                view.RemoveFromSuperview ();
            }

           
            remoteFoldersCheckboxes = new List<NSButton> ();
            List<RepositoryItem> remoteItems = new RemoteRepositoryController (null).RootFolders;
            List<RepositoryItem> localItems = new PhysicalRepositoryController (repoDao.MainActive).RootFolders;
            List<RepositoryItem> totalItems = remoteItems;
            for (int i = 0; i < localItems.Count; i++) {
                if (!remoteItems.Contains (localItems [i])) {
                    totalItems.Add (localItems [i]);
                }
            }

            ignoreFolders = repoIgnore.All (repoDao.RootRepo ());

            for (int i = 0; i <totalItems.Count; i++) {
                NSButton chk = new NSButton () {
                    Frame = new RectangleF (5,  256 - ((remoteFoldersCheckboxes.Count + 1) * 17), 300, 18),
                    Title = remoteItems[i].Key,
                    StringValue = remoteItems[i].Key
                };
                chk.SetButtonType(NSButtonType.Switch);

                if(ignoreFolders.Any(j => j.Path.Equals(totalItems[i].Key)))
                    chk.State = NSCellStateValue.Off;
                else
                    chk.State = NSCellStateValue.On;

                remoteFoldersCheckboxes.Add (chk);
                foldersView.AddSubview (chk);
            }

        }

        public string ChangeSQFolder ()
        {
            string sqFolderPath = RuntimeSettings.SelectedHomePath;

            var openPanel = new NSOpenPanel();
            openPanel.ReleasedWhenClosed = true;
            openPanel.Prompt = "Select folder";
            openPanel.AllowsMultipleSelection = false;
            openPanel.CanCreateDirectories = true;
            openPanel.CanChooseFiles = false;
            openPanel.CanChooseDirectories = true;
            var result = openPanel.RunModal();
            if (result == 1)
            {
                sqFolderPath = Path.Combine(openPanel.Url.Path, GlobalSettings.HomeFolderName) + Path.DirectorySeparatorChar;
            }

            return sqFolderPath;
        }

        public void OnItemEvent()
        {
            if (SynchronizerUnit.UnitCount () > 0) {
                int itensToSync = SynchronizerUnit.GetTotalEventsToSync ();
                if (itensToSync > 0) {
                    EventsNumberLabel.StringValue = itensToSync + " Event" + (itensToSync > 1 ? "s" : "") + " to sync";
                } else {
                    if (SynchronizerUnit.AnyRecovering ()) {
                        EventsNumberLabel.StringValue = "Looking for changes...";
                    } else {
                        EventsNumberLabel.StringValue = "Up to date";
                    }
                }
            }

            itemsProcessedLabel.StringValue = "";
            List<TransferStatistic> unifinishedStatistics = RemoteRepositoryController.UnfinishedStatistics;
            unifinishedStatistics.Reverse ();
            foreach(TransferStatistic s in unifinishedStatistics){
                itemsProcessedLabel.StringValue += s.ToString() + "\n";
            }

            ItemsProcessedLabel2.StringValue = "";
            List<TransferStatistic> finishedStatistics = RemoteRepositoryController.FinishedStatistics;
            finishedStatistics.Reverse ();
            foreach(TransferStatistic s in finishedStatistics){
                ItemsProcessedLabel2.StringValue += s.ToString() + "\n";
            }
        }

    }
}

