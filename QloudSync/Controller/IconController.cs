//   SparkleShare, an instant update workflow to Git.
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
using System.Drawing;
using System.IO;

using MonoMac.Foundation;
using MonoMac.AppKit;
using MonoMac.ObjCRuntime;
using System.Threading;
using GreenQloud.Synchrony;

namespace GreenQloud {

    public enum IconState {
        Idle,
        SyncingUp,
        SyncingDown,
        Syncing,
        Error
    }

    public class IconController : NSObject {

        private NSMenu menu;
        private NSMenu submenu_recents;
        private NSStatusItem status_item;
        private NSMenuItem state_item;
        private NSMenuItem folder_item;

        private NSMenuItem preferences_item;
        private NSMenuItem about_item;
        private NSMenuItem openweb_item;
        private NSMenuItem notify_item;
        private NSMenuItem recent_events_item;
        private NSMenuItem quit_item;
        private NSMenuItem co2_savings_item;
        private NSMenuItem help_item;

        private NSImage syncing_idle_image;
        private NSImage syncing_up_image;
        private NSImage syncing_down_image;
        private NSImage syncing_image;
        private NSImage syncing_error_image;

        private NSImage syncing_idle_image_active;
        private NSImage syncing_up_image_active;
        private NSImage syncing_down_image_active;
        private NSImage syncing_image_active;
        private NSImage syncing_error_image_active;

        private NSImage folder_image;
        private NSImage caution_image;
        private NSImage sparkleshare_image;

        private EventHandler [] folder_tasks;
        private EventHandler [] overflow_tasks;
        public event UpdateIconEventHandler UpdateIconEvent = delegate { };
        public delegate void UpdateIconEventHandler (IconState state);
        
        public event UpdateMenuEventHandler UpdateMenuEvent = delegate { };
        public delegate void UpdateMenuEventHandler (IconState state);
        
        public event UpdateStatusItemEventHandler UpdateStatusItemEvent = delegate { };
        public delegate void UpdateStatusItemEventHandler (string state_text);
        
        public event UpdateQuitItemEventHandler UpdateQuitItemEvent = delegate { };
        public delegate void UpdateQuitItemEventHandler (bool quit_item_enabled);
        
        public event UpdateRecentEventsItemEventHandler UpdateRecentEventsItemEvent = delegate { };
        public delegate void UpdateRecentEventsItemEventHandler (bool recent_events_item_enabled);
        
        public IconState CurrentState = IconState.Idle;
        public string StateText = string.Format("Welcome to {0}!", GlobalSettings.ApplicationName);
        
        public readonly int MenuOverflowThreshold   = 9;
        public readonly int MinSubmenuOverflowCount = 3;
        
        public string [] Folders;
        public string [] FolderErrors;
        
        public string [] OverflowFolders;
        public string [] OverflowFolderErrors;
        
        
        public string FolderSize {
            get {
                double size = 0;
                
                if (size == 0)
                    return "";
                else
                    return "— " + Program.Controller.FormatSize (size);
            }
        }
        
        public int ProgressPercentage {
            get {
                return (int) Program.Controller.ProgressPercentage;
            }
        }
        
        public string ProgressSpeed {
            get {
                return Program.Controller.ProgressSpeed;
            }
        }
        
        public bool QuitItemEnabled {
            get {
                return (CurrentState == IconState.Idle || CurrentState == IconState.Error);
            }
        }
        
        public IconController () : base ()
        {
            using (var a = new NSAutoreleasePool ())
            {
                this.status_item = NSStatusBar.SystemStatusBar.CreateStatusItem (28);
                this.status_item.HighlightMode = true;

                this.syncing_idle_image  = new NSImage (Path.Combine (NSBundle.MainBundle.ResourcePath, "Pixmaps", "process-syncing-idle.png"));
                this.syncing_up_image    = new NSImage (Path.Combine (NSBundle.MainBundle.ResourcePath, "Pixmaps", "process-syncing-up.png"));
                this.syncing_down_image  = new NSImage (Path.Combine (NSBundle.MainBundle.ResourcePath, "Pixmaps", "process-syncing-down.png"));
                this.syncing_image  = new NSImage (Path.Combine (NSBundle.MainBundle.ResourcePath, "Pixmaps", "process-syncing.png"));
                this.syncing_error_image = new NSImage (Path.Combine (NSBundle.MainBundle.ResourcePath, "Pixmaps", "process-syncing-error.png"));
                
                this.syncing_idle_image_active  = new NSImage (Path.Combine (NSBundle.MainBundle.ResourcePath, "Pixmaps", "process-syncing-idle-active.png"));
                this.syncing_up_image_active    = new NSImage (Path.Combine (NSBundle.MainBundle.ResourcePath, "Pixmaps", "process-syncing-up-active.png"));
                this.syncing_down_image_active  = new NSImage (Path.Combine (NSBundle.MainBundle.ResourcePath, "Pixmaps", "process-syncing-down-active.png"));
                this.syncing_image_active  = new NSImage (Path.Combine (NSBundle.MainBundle.ResourcePath, "Pixmaps", "process-syncing-active.png"));
                this.syncing_error_image_active = new NSImage (Path.Combine (NSBundle.MainBundle.ResourcePath, "Pixmaps", "process-syncing-error-active.png"));

                this.status_item.Image      = this.syncing_idle_image;
                this.status_item.Image.Size = new SizeF (16, 16);

                this.status_item.AlternateImage      = this.syncing_idle_image_active;
                this.status_item.AlternateImage.Size = new SizeF (16, 16);

                this.folder_image       = NSImage.ImageNamed ("NSFolder");
                this.caution_image      = NSImage.ImageNamed ("NSCaution");
                this.sparkleshare_image = NSImage.ImageNamed ("qloudsync-folder");

                CreateMenu ();
            }

            Program.Controller.OnIdle += delegate {

                if (CurrentState != IconState.Error) {
                    CurrentState = IconState.Idle;
                    
                    if (System.IO.Directory.GetDirectories(RuntimeSettings.HomePath).Length == 0 && System.IO.Directory.GetFiles (RuntimeSettings.HomePath).Length < 2)
                        StateText = string.Format("Welcome to {0}!",GlobalSettings.ApplicationName);
                    else
                        StateText = "Files up to date " + FolderSize;
                }
                
                UpdateQuitItemEvent (QuitItemEnabled);
                UpdateStatusItemEvent (StateText);
                UpdateIconEvent (CurrentState);
                UpdateMenuEvent (CurrentState);
            };

            Program.Controller.OnSyncing += delegate {
                bool syncDown = LocalSynchronizer.GetInstance().Status == SyncStatus.DOWNLOADING || 
                    RemoteSynchronizer.GetInstance().Status == SyncStatus.DOWNLOADING || 
                    BacklogSynchronizer.GetInstance().Status == SyncStatus.DOWNLOADING;                
                bool syncUp = LocalSynchronizer.GetInstance().Status == SyncStatus.UPLOADING || 
                    RemoteSynchronizer.GetInstance().Status == SyncStatus.UPLOADING || 
                        BacklogSynchronizer.GetInstance().Status == SyncStatus.UPLOADING;
                if(syncDown && syncUp){
                    CurrentState = IconState.Syncing;
                    StateText    = "Syncing changes…";
                    
                } else if (syncUp) {
                    CurrentState = IconState.SyncingUp;
                    StateText    = "Sending changes…";
                    
                } else if (syncDown){
                    CurrentState = IconState.SyncingDown;
                    StateText    = "Receiving changes…";
                }
                
                if (ProgressPercentage > 0)
                    StateText += " " + ProgressPercentage + "%  " + ProgressSpeed;
                
                UpdateIconEvent (CurrentState);
                UpdateStatusItemEvent (StateText);
                UpdateQuitItemEvent (QuitItemEnabled);
            };
            
            Program.Controller.OnError += delegate {
                CurrentState = IconState.Error;
                switch(Program.Controller.ErrorType)
                {
                    case ERROR_TYPE.DISCONNECTION:
                        StateText = "Lost network connection";
                    break;
                    case ERROR_TYPE.ACCESS_DENIED:
                        StateText = "Access Denied. Login again!";
                        this.preferences_item.Enabled = true;
                    break;
                    default:
                        StateText = "Failed to send some changes";
                    break;
                }
                UpdateQuitItemEvent (QuitItemEnabled);
                UpdateStatusItemEvent (StateText);
                UpdateIconEvent (CurrentState);
                UpdateMenuEvent (CurrentState);
            };			

            UpdateIconEvent += delegate (IconState state) {
                using (var a = new NSAutoreleasePool ())
                {
                    InvokeOnMainThread (delegate {
                        switch (state) {
                        case IconState.Idle: {
                            this.status_item.Image          = this.syncing_idle_image;
                            this.status_item.AlternateImage = this.syncing_idle_image_active;
                            break;
                        }
                        case IconState.SyncingUp: {
                            this.status_item.Image          = this.syncing_up_image;
                            this.status_item.AlternateImage = this.syncing_up_image_active;
                            break;
                        }
                        case IconState.SyncingDown: {
                            this.status_item.Image          = this.syncing_down_image;
                            this.status_item.AlternateImage = this.syncing_down_image_active;
                            break;
                        }
                        case IconState.Syncing: {
                            this.status_item.Image          = this.syncing_image;
                            this.status_item.AlternateImage = this.syncing_image_active;
                            break;
                        }
                        case IconState.Error: {
                            this.status_item.Image          = this.syncing_error_image;
                            this.status_item.AlternateImage = this.syncing_error_image_active;
                            break;
                        }
                        }

                        this.status_item.Image.Size = new SizeF (16, 16);
                        this.status_item.AlternateImage.Size = new SizeF (16, 16);
                    });
                }
            };

            UpdateStatusItemEvent += delegate (string state_text) {
                using (var a = new NSAutoreleasePool ())
                {
                    InvokeOnMainThread (delegate {
                        this.state_item.Title = state_text;
                    });
                }
            };

            UpdateMenuEvent += delegate {
                using (var a = new NSAutoreleasePool ())
                {
                    InvokeOnMainThread (() => CreateMenu ());
                }
            };

            UpdateQuitItemEvent += delegate (bool quit_item_enabled) {
                using (var a = new NSAutoreleasePool ())
                {
                    InvokeOnMainThread (delegate {
                        this.quit_item.Enabled = quit_item_enabled;
                    });
                }
            };

            UpdateRecentEventsItemEvent += delegate (bool events_item_enabled) {
                using (var a = new NSAutoreleasePool ())
                {
                    InvokeOnMainThread (delegate {
                        this.recent_events_item.Enabled = Program.Controller.RecentsTransfers.Count != 0;
                    });
                }
            };
        }


        public void CreateMenu ()
        {
            using (NSAutoreleasePool a = new NSAutoreleasePool ())
            {
                this.menu                  = new NSMenu ();
                this.menu.AutoEnablesItems = false;

                this.state_item = new NSMenuItem () {
                    Title   = StateText,
                    Enabled = false
                };

                this.folder_item = new NSMenuItem () {
                    Title = GlobalSettings.HomeFolderName+" Folder"
                };

                this.folder_item.Activated += delegate {
                    SparkleShareClicked ();
                };

                this.folder_item.Image      = this.sparkleshare_image;
                this.folder_item.Image.Size = new SizeF (16, 16);
                this.folder_item.Enabled    = true;

                this.preferences_item = new NSMenuItem () {
                    Title   = "Preferences…",
                    Enabled = false
                };

                this.preferences_item.Activated += delegate {
                    AddHostedProjectClicked ();
                };

                this.recent_events_item = new NSMenuItem () {
                    Title   = "Recent Changes…",
                    Enabled =  Program.Controller.RecentsTransfers.Count != 0
                };

                recent_events_item.Activated += delegate {
                    ChangeClicked ();
                };

                this.notify_item = new NSMenuItem () {
                    Enabled = false// (Controller.Folders.Length > 0)
                };

                if (Preferences.NotificationsEnabled)
                    this.notify_item.Title = "Turn Notifications Off";
                else
                    this.notify_item.Title = "Turn Notifications On";

                this.notify_item.Activated += delegate {
                    Program.Controller.ToggleNotifications ();

                    InvokeOnMainThread (delegate {
                        if (Preferences.NotificationsEnabled)
                            this.notify_item.Title = "Turn Notifications Off";
                        else
                            this.notify_item.Title = "Turn Notifications On";
                    });
                };

                this.about_item = new NSMenuItem () {
                    Title   = string.Format("About {0}", GlobalSettings.ApplicationName),
                    Enabled = true
                };

                this.openweb_item = new NSMenuItem() {
                    Title = "Share/View Online…",
                    Enabled = true
                };

               this.openweb_item.Activated += delegate {                    
                    string hash = Crypto.GetHMACbase64(Credential.SecretKey,Credential.PublicKey, true);
                    Program.Controller.OpenWebsite (string.Format("https://my.greenqloud.com/qloudsync?username={0}&hash={1}&returnUrl=/storageQloud", Credential.Username, hash));
               };

                this.about_item.Activated += delegate {
                    AboutClicked ();
                };

                this.quit_item = new NSMenuItem () {
                    Title   = "Quit",
                    Enabled = QuitItemEnabled
                };

                this.quit_item.Activated += delegate {
                    QuitClicked ();
                };


                co2_savings_item = new NSMenuItem () {
                    Title = "Loading data of savings",
                    Enabled = false
                };

                help_item = new NSMenuItem(){
                    Title = "Help Center"
                };

                help_item.Activated += delegate {
                    Program.Controller.OpenWebsite ("http://support.greenqloud.com");
               };

                this.menu.AddItem (this.state_item);
                this.menu.AddItem (co2_savings_item);
                this.menu.AddItem (NSMenuItem.SeparatorItem);
                this.menu.AddItem (this.folder_item);
                this.menu.AddItem (this.openweb_item);                
                this.menu.AddItem (this.recent_events_item);
                this.menu.AddItem (NSMenuItem.SeparatorItem);
                this.menu.AddItem (this.preferences_item);
                this.menu.AddItem (help_item);
				this.menu.AddItem (this.about_item);
			    this.menu.AddItem (NSMenuItem.SeparatorItem);
                this.menu.AddItem (this.quit_item);

                this.menu.Delegate    = new SparkleStatusIconMenuDelegate ();
                this.status_item.Menu = this.menu;
            }
        }

        
        public void SparkleShareClicked ()
        {
            Program.Controller.OpenSparkleShareFolder ();
        }
        
        
        
        public void AddHostedProjectClicked ()
        {
            new Thread (() => Program.Controller.ShowSetupWindow (PageType.Add)).Start ();
        }
        
        
        public void RecentEventsClicked ()
        {
            new Thread (() => Program.Controller.ShowEventLogWindow ()).Start ();
        }
        
        
        public void AboutClicked ()
        {
            Program.Controller.ShowAboutWindow ();
        }

        public void ChangeClicked ()
        {
            Program.Controller.ShowTransferWindow ();
        }
        
        public void QuitClicked ()
        {
            Program.Controller.Quit ();
        }

        void UpdateCO2Savings ()
        {
            CO2Savings saving = Statistics.EarlyCO2Savings;

            this.co2_savings_item.Title = string.Format ("Yearly CO2 Savings: {0}", saving.Saved);
           
        }

    }
    
    
    public class SparkleStatusIconMenuDelegate : NSMenuDelegate {

        public SparkleStatusIconMenuDelegate ()
        {
        }


        public SparkleStatusIconMenuDelegate (IntPtr handle) : base (handle)
        {
        }


        public override void MenuWillHighlightItem (NSMenu menu, NSMenuItem item)
        {
        }

    
        public override void MenuWillOpen (NSMenu menu)
        {
            InvokeOnMainThread (() => {
                NSApplication.SharedApplication.DockTile.BadgeLabel = null;
            });
        }
    }
}
