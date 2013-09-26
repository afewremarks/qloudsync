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
using GreenQloud.Model;
using GreenQloud.Persistence;
using System.Collections.Generic;
using GreenQloud.Persistence.SQLite;
using System.Diagnostics;


namespace GreenQloud {

    public enum IconState {
        Idle,
        Working,
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

        private List<NSMenuItem> recentChanges;


        private NSMenuItem notify_item;
        private NSMenuItem recent_events_title;
        private NSMenuItem pause_sync;
        private NSMenuItem quit_item;
        private NSMenuItem co2_savings_item;
        private NSMenuItem help_item;

        private NSImage syncing_working;
        private NSImage syncing_idle_image;
        private NSImage syncing_up_image;
        private NSImage syncing_down_image;
        private NSImage syncing_image;
        private NSImage syncing_error_image;
        private NSImage disconnected_image;

        private NSImage syncing_idle_image_active;
        private NSImage syncing_up_image_active;
        private NSImage syncing_down_image_active;
        private NSImage syncing_image_active;
        private NSImage syncing_error_image_active;

        private NSImage share_image;
        private NSImage folder_image;
        private NSImage caution_image;
        private NSImage sparkleshare_image;
        private NSImage up_to_date;
        private NSImage work_in_progress;
        private NSImage error_in_sync;

        private NSImage docs_image;
        private NSImage movies_image;
        private NSImage music_image;
        private NSImage pics_image;
        private NSImage default_image;
        private bool isPaused = false;


        public event UpdateIconEventHandler UpdateIconEvent = delegate { };
        public delegate void UpdateIconEventHandler (IconState state);
        
        public event UpdateMenuEventHandler UpdateMenuEvent = delegate { };
        public delegate void UpdateMenuEventHandler (IconState state);
        
        public event UpdateStatusItemEventHandler UpdateStatusItemEvent = delegate { };
        public delegate void UpdateStatusItemEventHandler (string state_text, NSImage image);
        
        public event UpdateQuitItemEventHandler UpdateQuitItemEvent = delegate { };
        public delegate void UpdateQuitItemEventHandler (bool quit_item_enabled);

            public IconState CurrentState = IconState.Working;
            public string StateText = string.Format ("Welcome to {0}!", GlobalSettings.ApplicationName);

            public readonly int MenuOverflowThreshold = 9;
            public readonly int MinSubmenuOverflowCount = 3;
            public string[] Folders;
            public string[] FolderErrors;
            public string[] OverflowFolders;
            public string[] OverflowFolderErrors;
            private Thread co2Update;



        
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

        public bool RecenEventsEnabled {
            get {
                return true;
            }
        }

        public IconController () : base ()
        {
            recentChanges = new List<NSMenuItem> ();
            using (var a = new NSAutoreleasePool ())
            {
                this.status_item = NSStatusBar.SystemStatusBar.CreateStatusItem (28);
                this.status_item.HighlightMode = true;


                this.syncing_working  = new NSImage (Path.Combine (NSBundle.MainBundle.ResourcePath, "Pixmaps", "process-syncing-working.png"));
                this.syncing_idle_image  = new NSImage (Path.Combine (NSBundle.MainBundle.ResourcePath, "Pixmaps", "process-syncing-idle.png"));
                this.syncing_up_image    = new NSImage (Path.Combine (NSBundle.MainBundle.ResourcePath, "Pixmaps", "process-syncing-up.png"));
                this.syncing_down_image  = new NSImage (Path.Combine (NSBundle.MainBundle.ResourcePath, "Pixmaps", "process-syncing-down.png"));
                this.syncing_image  = new NSImage (Path.Combine (NSBundle.MainBundle.ResourcePath, "Pixmaps", "process-syncing.png"));
                this.syncing_error_image = new NSImage (Path.Combine (NSBundle.MainBundle.ResourcePath, "Pixmaps", "process-syncing-error.png"));
                this.disconnected_image = new NSImage (Path.Combine (NSBundle.MainBundle.ResourcePath, "Pixmaps", "process-syncing-active_new.png"));

                this.syncing_idle_image_active  = new NSImage (Path.Combine (NSBundle.MainBundle.ResourcePath, "Pixmaps", "process-syncing-idle-active.png"));
                this.syncing_up_image_active    = new NSImage (Path.Combine (NSBundle.MainBundle.ResourcePath, "Pixmaps", "process-syncing-up-active.png"));
                this.syncing_down_image_active  = new NSImage (Path.Combine (NSBundle.MainBundle.ResourcePath, "Pixmaps", "process-syncing-down-active.png"));
                this.syncing_image_active  = new NSImage (Path.Combine (NSBundle.MainBundle.ResourcePath, "Pixmaps", "process-syncing-active.png"));
                this.syncing_error_image_active = new NSImage (Path.Combine (NSBundle.MainBundle.ResourcePath, "Pixmaps", "process-syncing-error-active.png"));

                this.status_item.Image      = this.syncing_working;
                this.status_item.Image.Size = new SizeF (16, 16);

                //this.status_item.AlternateImage      = this.syncing_idle_image_active;
                //this.status_item.AlternateImage.Size = new SizeF (16, 16);
                this.folder_image       = NSImage.ImageNamed ("NSFolder");
                this.caution_image      = NSImage.ImageNamed ("NSCaution");
                this.sparkleshare_image = new NSImage (Path.Combine (NSBundle.MainBundle.ResourcePath, "qloudsync-folder.icns"));

                this.share_image = new NSImage (Path.Combine (NSBundle.MainBundle.ResourcePath, "Pixmaps", "share.png"));
                this.docs_image = new NSImage (Path.Combine (NSBundle.MainBundle.ResourcePath, "Pixmaps", "folder-docs.png"));
                this.docs_image.Size = new SizeF (16, 16);
                this.movies_image = new NSImage (Path.Combine (NSBundle.MainBundle.ResourcePath, "Pixmaps", "folder-movies.png"));
                this.movies_image.Size = new SizeF (16, 16);
                this.music_image = new NSImage (Path.Combine (NSBundle.MainBundle.ResourcePath, "Pixmaps", "folder-music.png"));
                this.music_image.Size = new SizeF (16, 16);
                this.pics_image = new NSImage (Path.Combine (NSBundle.MainBundle.ResourcePath, "Pixmaps", "folder-pics.png"));
                this.pics_image.Size = new SizeF (16, 16);
                this.default_image  = new NSImage (Path.Combine (NSBundle.MainBundle.ResourcePath, "Pixmaps", "file-3.png"));
                this.default_image.Size = new SizeF (16, 16);

                CreateMenu ();
            }

            Program.Controller.OnIdle += delegate {
                CurrentState = IconState.Idle;
                StateText = "✓  Up to date ";
                UpdateQuitItemEvent (QuitItemEnabled);
                UpdateStatusItemEvent (StateText, this.up_to_date);
                UpdateIconEvent (CurrentState);
                UpdateMenuEvent (CurrentState);
            };


            Program.Controller.OnSyncing += delegate {
                bool syncDown = Program.Controller.IsDownloading();
                bool syncUp = Program.Controller.IsUploading();

                if(syncDown && syncUp){
                    CurrentState = IconState.Syncing;
                    StateText    = "⟳ Syncing changes…";
                    
                } else if (syncUp) {
                    CurrentState = IconState.SyncingUp;
                    StateText    = "⟳ Sending changes…";
                    
                } else if (syncDown){
                    CurrentState = IconState.SyncingDown;
                    StateText    = "⟳ Receiving changes…";
                }
                
                if (ProgressPercentage > 0)
                    StateText += " " + ProgressPercentage + "%  " + ProgressSpeed;
                
                UpdateIconEvent (CurrentState);
                UpdateStatusItemEvent (StateText, this.syncing_working);
                UpdateQuitItemEvent (QuitItemEnabled);
                UpdateMenuEvent (CurrentState);
            };
            
            Program.Controller.OnError += delegate {
                CurrentState = IconState.Error;
                switch(Program.Controller.ErrorType)
                {
                    case ERROR_TYPE.DISCONNECTION:
                    StateText = "✗   Lost network connection";
                    break;
                    case ERROR_TYPE.ACCESS_DENIED:
                    StateText = "✗   Access Denied. Login again!";
                        this.preferences_item.Enabled = true;
                    break;
                    default:
                    StateText = "✗   Failed to send some changes";
                    break;
                }
                UpdateQuitItemEvent (QuitItemEnabled);
                UpdateStatusItemEvent (StateText, this.error_in_sync);
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
                            break;
                        }
                        case IconState.SyncingUp: {
                            this.status_item.Image          = this.syncing_up_image;
                            break;
                        }
                        case IconState.SyncingDown: {
                            this.status_item.Image          = this.syncing_down_image;
                            break;
                        }
                        case IconState.Syncing: {
                            this.status_item.Image          = this.syncing_image;
                            break;
                        }
                        case IconState.Error: {
                            if(Program.Controller.ErrorType == ERROR_TYPE.DISCONNECTION){
                                this.status_item.Image          = this.disconnected_image;
                            } else {
                                this.status_item.Image          = this.syncing_error_image;
                            }
                            break;
                        }
                        }

                        this.status_item.Image.Size = new SizeF (16, 16);
                    });
                }
            };

            UpdateStatusItemEvent += delegate (string state_text, NSImage image) {
                using (var a = new NSAutoreleasePool ())
                {
                    InvokeOnMainThread (delegate {
                        StateText = state_text;
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
        }


        public void CreateMenu ()
        {
            this.recentChanges.Clear ();

            using (NSAutoreleasePool a = new NSAutoreleasePool ()) {
                this.menu = new NSMenu ();
                this.menu.AutoEnablesItems = false;

                this.state_item = new NSMenuItem () {
                    Title = StateText,
                    Enabled = true
                };

                this.folder_item = new NSMenuItem () {
                    Title = GlobalSettings.HomeFolderName+" Folder"
                };

                this.folder_item.Activated += delegate {
                    SparkleShareClicked ();
                };

                this.folder_item.Image = this.sparkleshare_image;
                this.folder_item.Image.Size = new SizeF (16, 16);
                this.folder_item.Enabled = true;

//                this.preferences_item = new NSMenuItem () {
//                    Title   = "Preferences…",
//                    Enabled = false
//                };

//                this.preferences_item.Activated += delegate {
//                    AddHostedProjectClicked ();
//                };

                this.recent_events_title = new NSMenuItem () {
                    Title   = "Recently Changed",
                    Enabled =  false
                };       
                recent_events_title.Activated += delegate {
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

                this.openweb_item = new NSMenuItem () {
                    Title = "Share/View Online…",
                    Enabled = true
                };

                this.openweb_item.Image = this.share_image;
                this.openweb_item.Image.Size = new SizeF (16, 16);
                this.openweb_item.Enabled = true;

                this.openweb_item.Activated += delegate {                    
                    Program.Controller.OpenStorageQloudWebSite();
                };

                this.about_item.Activated += delegate {
                    AboutClicked ();
                };

                this.pause_sync = new NSMenuItem() {
                    Title = PauseText(),
                    Enabled = true
                };

                this.pause_sync.Activated += delegate {
                  PauseSync();
                };

                this.quit_item = new NSMenuItem () {
                    Title   = "Quit",
                    Enabled = QuitItemEnabled
                };

                this.quit_item.Activated += delegate {
                    QuitClicked ();
                };



                co2_savings_item = new NSMenuItem () {
                    Title = "",
                    Enabled = true,
                    Hidden = true
                };

                if (co2Update == null) {
                    co2Update = new Thread (delegate() {
                        while (true) {
                            try {
                                string spent = Statistics.TotalUsedSpace.Spent;
                                string saved = Statistics.EarlyCO2Savings.Saved;
                                string subscript = "2";
                                subscript.ToLowerInvariant ();

                                using (var ns = new NSAutoreleasePool ()) {
                                    InvokeOnMainThread (() => { 
                                        if (spent != null && saved != null) {
                                            co2_savings_item.Title = spent + " used | " + saved + " CO₂ saved";
                                            co2_savings_item.Hidden = false;
                                        }
                                    });
                                }
                            } catch (Exception e) {
                                Console.WriteLine (e.Message);
                                Logger.LogInfo ("INFO", "Cannot load CO₂ savings.");
                            }
                            Thread.Sleep (60000);
                        }
                    });
                    co2Update.Start ();
                }

                help_item = new NSMenuItem () {
                    Title = "Help Center"
                };

                help_item.Activated += delegate {
                    Program.Controller.OpenWebsite ("http://support.greenqloud.com");
                };

               
                this.menu.AddItem (co2_savings_item);
                this.menu.AddItem (this.folder_item);
                this.menu.AddItem (this.openweb_item);  
                this.menu.AddItem (NSMenuItem.SeparatorItem);
                this.menu.AddItem (this.recent_events_title);
                this.menu.AddItem (NSMenuItem.SeparatorItem);


                if (Program.Controller.DatabaseLoaded()) {
                    SQLiteEventDAO eventDao = new SQLiteEventDAO ();
                    List<Event> events = eventDao.LastEvents;
                    string text = "";

                    foreach (Event e in events) {

                        NSMenuItem current = new NSMenuItem () {
                            Title = e.ItemName,
                            Enabled = true
                        };


                        current.Image = this.default_image;
                        if (e.ItemType == ItemType.IMAGE)
                            current.Image = this.pics_image;
                        if (e.ItemType == ItemType.TEXT)
                            current.Image = this.docs_image;
                        if (e.ItemType == ItemType.VIDEO)
                            current.Image = this.movies_image;
                        if (e.ItemType == ItemType.AUDIO)
                            current.Image = this.music_image;

                        current.ToolTip = e.ToString ();

                        EventHandler evt = new EventHandler(
                            delegate {
                                InvokeOnMainThread (() => RecentChangeItemClicked(e, null));
                            }
                        );
                        current.Activated += evt;

                        string title = "   "+e.ItemUpdatedAt;
                        NSAttributedString att = new NSAttributedString (title, NSFontManager.SharedFontManager.FontWithFamily ("Helvetica", NSFontTraitMask.Narrow, 5, 11));
                        NSMenuItem subtitle = new NSMenuItem () {
                            Enabled = false
                        };
                        subtitle.IndentationLevel = 1;
                        subtitle.AttributedTitle = att;

                        this.recentChanges.Add (current);
                        this.menu.AddItem (current);
                        this.menu.AddItem (subtitle);
                        text += e.ToString () + "\n\n";

                    }
                    this.recent_events_title.ToolTip = text;

                

                }
            
                this.menu.AddItem (NSMenuItem.SeparatorItem);
                this.menu.AddItem (this.state_item);
			    this.menu.AddItem (NSMenuItem.SeparatorItem);
                this.menu.AddItem (help_item);
                this.menu.AddItem (this.about_item);
               
                //this.menu.Delegate    = new SparkleStatusIconMenuDelegate ();
                this.status_item.Menu = this.menu;
                this.menu.AddItem (NSMenuItem.SeparatorItem);
                this.menu.AddItem (this.pause_sync);
                this.menu.AddItem (quit_item);
            }
        }

        public void PauseSync(){

            if (isPaused) {
                isPaused = false;
                SynchronizerUnit.ReconnectResolver ();
                this.pause_sync.Title = "Pause Sync";
                Console.Out.WriteLine("Syncronizers Resumed!");
            } else {
                isPaused = true;
                SynchronizerUnit.DisconnectResolver ();
                this.pause_sync.Title = "Resume Sync";
                Console.Out.WriteLine("Syncronizers Paused!");
            }

        }


        public void SparkleShareClicked ()
        {
            Program.Controller.OpenSparkleShareFolder ();
        }
        
        public void RecentChangeItemClicked (object sender, EventArgs e)
        {
            Program.Controller.OpenRepositoryitemFolder (((Event)sender).ItemLocalFolderPath);
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

        public String PauseText (){
            if (isPaused) {
                return "Resume Sync"; 
            }else{
                return "Pause Sync";
            }

        }

        public void QuitClicked ()
        {
            Program.Controller.Quit ();
        }

        void UpdateCO2Savings ()
        {
            CO2Savings saving = Statistics.EarlyCO2Savings;

            this.co2_savings_item.Title = string.Format ("Yearly CO₂ Savings: {0}", saving.Saved);
           
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

        public override void MenuDidClose (NSMenu menu) {
        
        }
    
        public override void MenuWillOpen (NSMenu menu)
        {
            InvokeOnMainThread (() => {
                NSApplication.SharedApplication.DockTile.BadgeLabel = null;
            });
        }
    }
}
            