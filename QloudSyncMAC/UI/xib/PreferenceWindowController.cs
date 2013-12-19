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

namespace QloudSync
{
    public partial class PreferenceWindowController : MonoMac.AppKit.NSWindowController
    {
        List<NSButton> remoteFoldersCheckboxes;
        SQLiteRepositoryDAO repoDao;
        SQLiteRepositoryIgnoreDAO repoIgnore;

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
                    InvokeOnMainThread (delegate {
                        base.LoadWindow ();
                        //will render for generic 
                        base.ShowWindow (new NSObject ());
                    });
                }
            };
        }
        // Shared initialization code
        void Initialize ()
        {
            repoDao = new SQLiteRepositoryDAO();
            repoIgnore = new SQLiteRepositoryIgnoreDAO();
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
    }
}

