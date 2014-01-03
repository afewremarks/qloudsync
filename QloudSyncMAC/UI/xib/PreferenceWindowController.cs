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

namespace QloudSync
{
    public partial class PreferenceWindowController : MonoMac.AppKit.NSWindowController
    {
        private List<NSButton> remoteFoldersCheckboxes;
        private SQLiteRepositoryDAO repoDao;
        private SQLiteRepositoryIgnoreDAO repoIgnore;
        private RemoteRepositoryController remoteRepositoryController;
        //private NetworkTraffic netTraffic;
        private List<RepositoryItem> items;
        private List<RepositoryIgnore> ignoreFolders;
        private Timer timer;
        private float lastAmountOfBytesReceived;
        private float lastAmountOfBytesSent;
        private bool makeStep;
        private bool isUpload;
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

            Program.Controller.ShowEventPreferenceWindow += delegate {
                using (var a = new NSAutoreleasePool ())
                {
                    NSRunLoop.Main.BeginInvokeOnMainThread (delegate {
                        base.LoadWindow ();
                        loadFolders();
                        //will render for generic 
                        Window.OpenWindow();
                    });

                }
            };
        }
        // Shared initialization code
        void Initialize ()
        {   
            count = 0;
            items = new List<RepositoryItem>();
            makeStep = false;
            isUpload = false;
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
            //Initial Timers
            timer = new Timer (1000);
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
                            Logger.LogInfo("ERROR", ex);
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
            foreach (NSView view in foldersView.Subviews) {
                view.RemoveFromSuperview ();
            }

           
            remoteFoldersCheckboxes = new List<NSButton> ();
            List<RepositoryItem> remoteItems = new RemoteRepositoryController (null).RootFolders;
            ignoreFolders = repoIgnore.All (repoDao.RootRepo ());

            for (int i = 0; i <remoteItems.Count; i++) {
                NSButton chk = new NSButton () {
                    Frame = new RectangleF (5,  256 - ((remoteFoldersCheckboxes.Count + 1) * 17), 300, 18),
                    Title = remoteItems[i].Key,
                    StringValue = remoteItems[i].Key
                };
                chk.SetButtonType(NSButtonType.Switch);

                if(ignoreFolders.Any(j => j.Path.Equals(remoteItems[i].Key)))
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
            Event e = Program.Controller.GetCurrentEvent ();
            ResetItemList ();
            if (e != null) {

                EventType eventType = e.EventType;

                if (e.Item != null && eventType != EventType.DELETE) {



                    if(!items.Contains(e.Item)){
                        count++;



                        if(!(items.Count == 0))
                            itemsProcessedLabel.StringValue += "done!" + Environment.NewLine;

                        if (count == 6) {
                            itemsProcessedLabel.StringValue = "";
                            count = 0;
                        }
                            

                        itemsLabel.StringValue = "1";
                        makeStep = true;

                        if (e.RepositoryType == RepositoryType.LOCAL) {
                            try {
                              
                                if (e.EventType == EventType.MOVE) {

                                    items.Add (e.Item.ResultItem);
                                } else {
                                    items.Add (e.Item);
                                }
                                isUpload = true;


                                itemsProcessedLabel.StringValue += " ↑ " + e.Item.Name + " ... ";
                              

          
                            } catch(Exception ex){
                                Logger.LogInfo ("ERROR", "NWManger " + ex.ToString());
                            }
                        } else {
                            try{
                                isUpload = false;
                                items.Add(e.Item);
                                remoteRepositoryController = new RemoteRepositoryController(e.Item.Repository);


                              
                                itemsProcessedLabel.StringValue += " ↓ " + e.Item.Name + " ... ";
                              
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

    }
}

