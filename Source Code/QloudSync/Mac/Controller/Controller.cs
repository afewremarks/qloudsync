//   SparkleShare, a collaboration and sharing tool.
//   Copyright (C) 2010  Hylke Bons <hylkebons@gmail.com>
//
//   This program is free software: you can redistribute it and/or modify
//   it under the terms of the GNU General Public License as published by
//   the Free Software Foundation, either version 3 of the License, or
//   (at your option) any later version.
//
//   This program is distributed in the hope that it will be useful,
//   but WITHOUT ANY WARRANTY; without even the implied warranty of
//   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//   GNU General Public License for more details.
//
//   You should have received a copy of the GNU General Public License
//   along with this program. If not, see <http://www.gnu.org/licenses/>.


using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

using MonoMac.Foundation;
using MonoMac.AppKit;
using MonoMac.ObjCRuntime;

 

namespace QloudSync {

	public class Controller{

        public IconController StatusIcon;
        DownloadController downloadController;
        UploadController uploadController;
        public double ProgressPercentage = 0.0;
        public string ProgressSpeed      = "";
        
        
        public event ShowSetupWindowEventHandler ShowSetupWindowEvent = delegate { };
        public delegate void ShowSetupWindowEventHandler (PageType page_type);
        
        public event Action ShowAboutWindowEvent = delegate { };
        public event Action ShowEventLogWindowEvent = delegate { };
        
        public event FolderFetchedEventHandler FolderFetched = delegate { };
        public delegate void FolderFetchedEventHandler (string [] warnings);
        
        public event FolderFetchErrorHandler FolderFetchError = delegate { };
        public delegate void FolderFetchErrorHandler (string [] errors);
        
        public event FolderFetchingHandler FolderFetching = delegate { };
        public delegate void FolderFetchingHandler (double percentage);

        public event Action OnIdle = delegate { };
        public event Action OnSyncing = delegate { };
        public event Action OnError = delegate { };


        public Controller () : base ()
        {
            NSApplication.Init ();
        }


        public void Initialize ()
        {
            CreateConfigFolder();

            if (CreateHomeFolder ())
                AddToBookmarks ();
        }

        public void UIHasLoaded ()
        {
            if (RuntimeSettings.FirstRun) {
                ShowSetupWindow (PageType.Login);
                foreach (string f in Directory.GetFiles(RuntimeSettings.ConfigPath))
                    File.Delete (f);

            } else {
                InitializeSynchronizers();
            }
        }

        private void InitializeSynchronizers ()
        {
           // StatusIcon = new IconController ();
            QloudSync.Synchrony.BacklogSynchronizer.GetInstance ().Synchronize ();

            downloadController = DownloadController.GetInstance();
            //downloadController.Synchronize ();
            uploadController = UploadController.GetInstance();


            /*repo.ChangesDetected += delegate {
                UpdateState ();
            };
            
            repo.SyncStatusChanged += delegate (SyncStatus status) {
                if (status == SyncStatus.Idle) {
                    ProgressPercentage = 0.0;
                    ProgressSpeed      = "";
                }
                
                UpdateState ();
            };
            
            repo.ProgressChanged += delegate (double percentage, string speed) {
                ProgressPercentage = percentage;
                ProgressSpeed      = speed;
                
                UpdateState ();
            };
            
            repo.Initialize ();*/
        }
        
        
        // Fires events for the current syncing state
        private void UpdateState ()
        {
            /*bool has_unsynced_repos = false;
            if (repo.Status == SyncStatus.SyncDown || repo.Status == SyncStatus.SyncUp || repo.IsBuffering) {
                OnSyncing ();
                return;
                
            } else if (repo.HasUnsyncedChanges) {
                has_unsynced_repos = true;
            }

            if (has_unsynced_repos)
                OnError ();
            else
                OnIdle ();*/
        }
        


        public void SyncStart ()
        {
            downloadController = DownloadController.GetInstance();

            downloadController.Finished += () => FinishFetcher();
            downloadController.Failed += delegate {
                FolderFetchError (downloadController.Errors);
                SyncStop();
            };
            
            downloadController.ProgressChanged += delegate (double percentage) {
                FolderFetching (percentage);
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
            NSMutableDictionary sidebar_plist = NSMutableDictionary.FromDictionary (
                NSUserDefaults.StandardUserDefaults.PersistentDomainForName ("com.apple.sidebarlists"));

            // Go through the sidebar categories
            foreach (NSString sidebar_category in sidebar_plist.Keys) {

                // Find the favorites
                if (sidebar_category.ToString ().Equals ("favorites")) {

                    // Get the favorites
                    NSMutableDictionary favorites = NSMutableDictionary.FromDictionary(
                        (NSDictionary) sidebar_plist.ValueForKey (sidebar_category));

                    // Go through the favorites
                    foreach (NSString favorite in favorites.Keys) {

                        // Find the custom favorites
                        if (favorite.ToString ().Equals ("VolumesList")) {

                            // Get the custom favorites
                            NSMutableArray custom_favorites = (NSMutableArray) favorites.ValueForKey (favorite);

                            NSMutableDictionary properties = new NSMutableDictionary ();
                            properties.SetValueForKey (new NSString ("1935819892"), new NSString ("com.apple.LSSharedFileList.TemplateSystemSelector"));

                            NSMutableDictionary new_favorite = new NSMutableDictionary ();
                            new_favorite.SetValueForKey (new NSString (GlobalSettings.ApplicationName),  new NSString ("Name"));

                            new_favorite.SetValueForKey (NSData.FromString ("ImgR SYSL fldr"),  new NSString ("Icon"));

                            new_favorite.SetValueForKey (NSData.FromString (RuntimeSettings.HomePath),
                                new NSString ("Alias"));

                            new_favorite.SetValueForKey (properties, new NSString ("CustomItemProperties"));

                            // Add to the favorites
                            custom_favorites.Add (new_favorite);
                            favorites.SetValueForKey ((NSArray) custom_favorites, new NSString (favorite.ToString ()));
                            sidebar_plist.SetValueForKey (favorites, new NSString (sidebar_category.ToString ()));
                        }
                    }

                }
            }

            NSUserDefaults.StandardUserDefaults.SetPersistentDomain (sidebar_plist, "com.apple.sidebarlists");
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


   	}
}
