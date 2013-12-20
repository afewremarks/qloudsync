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
using QloudSyncCore.Core.Util;
using System.Diagnostics;
using System.Timers;

namespace QloudSync
{
    public partial class PreferenceWindowController : MonoMac.AppKit.NSWindowController
    {
        List<NSButton> remoteFoldersCheckboxes;
        SQLiteRepositoryDAO repoDao;
        SQLiteRepositoryIgnoreDAO repoIgnore;
        RemoteRepositoryController remoteRepositoryController;
        NetworkTraffic netTraffic;
        List<RepositoryItem> items;
        Timer timer;
        float lastAmountOfBytesReceived;
        float lastAmountOfBytesSent;
        bool makeStep;
        bool isUpload;

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

            Program.Controller.ShowEventPreferenceWindow += delegate {
                using (var a = new NSAutoreleasePool ())
                {

                    NSRunLoop.Main.BeginInvokeOnMainThread (delegate {
                        base.LoadWindow ();
                        //will render for generic 
                        base.ShowWindow (this);
                    });
                }
            };
        }
        // Shared initialization code
        void Initialize ()
        {   
            items = new List<RepositoryItem>();
            makeStep = false;
            isUpload = false;
            repoDao = new SQLiteRepositoryDAO();
            repoIgnore = new SQLiteRepositoryIgnoreDAO();
            netTraffic = new NetworkTraffic (Process.GetCurrentProcess().Id);

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
            loadFolders ();
            //Initial Timers
            timer = new Timer (1000);
            timer.Elapsed += delegate {
                float currentAmountOfBytesReceived = netTraffic.GetBytesReceived ();
                float currentAmountOfBytesSent = netTraffic.GetBytesSent ();
                using (NSAutoreleasePool pool = new NSAutoreleasePool()) {
                    totalBandwidthLabel.StringValue = string.Format("{0} Kb/s", (currentAmountOfBytesReceived / 1024).ToString ("0.00"));
                    downloadBandwidthLabel.StringValue = string.Format("{0} Kb/s", ((currentAmountOfBytesReceived - lastAmountOfBytesReceived) / 1024).ToString("0.00"));
                    uploadBandwidthLabel.StringValue = string.Format("{0} Kb/s" ,Math.Abs(((currentAmountOfBytesReceived - lastAmountOfBytesReceived) / 1024)).ToString("0.00"));
                    lastAmountOfBytesReceived = currentAmountOfBytesReceived;
                    lastAmountOfBytesSent = currentAmountOfBytesSent;
                    OnItemEvent();
                    UpdateProgressBar();
                }
            };
            timer.Start ();

            Window.WillClose += delegate {
                timer.Stop();
            };

            //Selective Sync Tab
            changeFoldersButton.Activated += delegate {

                int size = this.remoteFoldersCheckboxes.Count;
                LocalRepository repo = repoDao.RootRepo();
                Program.Controller.KillSynchronizers();
                for(int i = 0; i < size; i++)
                {
                    if(this.remoteFoldersCheckboxes.ElementAt(i).State == NSCellStateValue.On)
                    {
                        repoIgnore.Create(repo, remoteFoldersCheckboxes.ElementAt(i).ToString());
                    }
                    else
                    {
                        repoIgnore.Remove(repo, remoteFoldersCheckboxes.ElementAt(i).ToString());
                    }

                }
                Program.Controller.InitializeSynchronizers(true);
            };

            moveSQFolderButton.Activated += delegate {
                string path = ChangeSQFolder();
                if(path != RuntimeSettings.SelectedHomePath)
                {
                    if(Program.Controller.Confirm("Are you sure of this? All your files will be moved"))
                    {
                        try
                        {
                            Program.Controller.MoveSQFolder(path);
                        }
                        catch(Exception ex)
                        {
                            Logger.LogInfo("ERROR", ex);
                            Program.Controller.Alert("Cannot move StorageQloud Folder");
                        }
                    }
                }
            };



            //Network Status tab


            //Account Tab
            pathLabel.StringValue = RuntimeSettings.SelectedHomePath;
            usernameLabel.StringValue = Credential.Username;
            versionLabel.StringValue = GlobalSettings.RunningVersion;

            unlinkAccountButton.Activated += delegate {
                try{
                    if( Program.Controller.Confirm("Are you sure you want to continue? You are unlinking your account to" +
                                                   "this computer.") ){
                        Program.Controller.UnlinkAccount ();
                    }
                }catch (Exception ex){
                    Logger.LogInfo("ERROR" , ex);
                    Program.Controller.Alert("Cannot unlink accounts, please check your " +
                        "internet connection and try again.");
                }
            };
        }

        //Helpers
        private void loadFolders()
        {
            remoteFoldersCheckboxes = new List<NSButton> ();
            List<RepositoryItem> remoteItems = new RemoteRepositoryController (null).RootFolders;

            foreach (RepositoryItem item in remoteItems) 
            {
                NSButton chk = new NSButton () {
                    Frame = new RectangleF (5,  -12 + ((remoteFoldersCheckboxes.Count + 1) * 17), 300, 18),
                    Title = item.Key
                };
                chk.SetButtonType(NSButtonType.Switch);
                chk.State = NSCellStateValue.On;
                remoteFoldersCheckboxes.Add (chk);
            }
            foreach (NSButton chk in remoteFoldersCheckboxes) 
            {
                foldersScrollView.AddSubview (chk);
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
            Event e = Program.Controller.GetCurrentEvent ();
            ResetStatusBar ();
            ResetItemList ();
            if (e != null) {

                EventType eventType = e.EventType;

                if (e.Item != null && eventType != EventType.DELETE) {

                    if(!items.Contains(e.Item)){

                        itemsLabel.StringValue = "1";
                        makeStep = true;

                        if (e.RepositoryType == RepositoryType.LOCAL) {
                            try {
                                FileInfo fi = new FileInfo (e.Item.LocalAbsolutePath);
                                if (e.EventType == EventType.MOVE) {
                                    fi = new FileInfo (e.Item.ResultItem.LocalAbsolutePath);
                                    items.Add (e.Item.ResultItem);
                                } else {
                                    items.Add (e.Item);
                                }
                                isUpload = true;
                                statusProgressIndicator.MaxValue = (int)fi.Length;
                            } catch(Exception ex){
                                Logger.LogInfo ("ERROR", "NWManger " + ex.ToString());
                            }
                        } else {
                            try{
                                isUpload = false;
                                items.Add(e.Item);
                                remoteRepositoryController = new RemoteRepositoryController(e.Item.Repository);
                                statusProgressIndicator.MaxValue = (int)remoteRepositoryController.GetContentLength(e.Item.Key);
                            }catch(Exception ex){
                                Logger.LogInfo ("ERROR", "NWManger " + ex.ToString());
                            }
                        }
                    }
                }
            }
        }

        private void ResetItemList()
        {
            if (items.Count == 50) {
                items.Clear ();
            }
        }

        private void ResetStatusBar()
        {
            if (statusProgressIndicator.DoubleValue == statusProgressIndicator.MaxValue) {
                statusProgressIndicator.DoubleValue = 0;
                makeStep = false;
                itemsLabel.StringValue = "0";
            }
        }

        private void UpdateProgressBar()
        {
            if (makeStep) {
                int step = (int)(netTraffic.GetBytesReceived () - lastAmountOfBytesReceived);

                if (isUpload) {
                    step = (int)(netTraffic.GetBytesSent () - lastAmountOfBytesSent);
                }

                statusProgressIndicator.IncrementBy (step);
            }
        }

       

    

    }
}

