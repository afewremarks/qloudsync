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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Threading;
using System.Security.Cryptography;


namespace QloudSync {

    public abstract class SparkleControllerBase {

        public double ProgressPercentage = 0.0;
        public string ProgressSpeed      = "";


        public event ShowSetupWindowEventHandler ShowSetupWindowEvent = delegate { };
        public delegate void ShowSetupWindowEventHandler (PageType page_type);

        public event Action ShowAboutWindowEvent = delegate { };
        public event Action ShowEventLogWindowEvent = delegate { };

        public event FolderFetchedEventHandler FolderFetched = delegate { };
        public delegate void FolderFetchedEventHandler (string remote_url, string [] warnings);
        
        public event FolderFetchErrorHandler FolderFetchError = delegate { };
        public delegate void FolderFetchErrorHandler (string remote_url, string [] errors);
        
        public event FolderFetchingHandler FolderFetching = delegate { };
        public delegate void FolderFetchingHandler (double percentage);
        
        public event Action FolderListChanged = delegate { };


        public event Action OnIdle = delegate { };
        public event Action OnSyncing = delegate { };
        public event Action OnError = delegate { };



        public abstract string EventLogHTML { get; }
        public abstract string DayEntryHTML { get; }
        public abstract string EventEntryHTML { get; }


        // Adds the SparkleShare folder to the user's
        // list of bookmarked places
        public abstract void AddToBookmarks ();

        // Creates the SparkleShare folder in the user's home folder
        public abstract bool CreateHomeFolder ();

        // Opens the SparkleShare folder or an (optional) subfolder
        public abstract void OpenFolder (string path);
        
        // Opens a file with the appropriate application
        public abstract void OpenFile (string path);
        
        // Opens a file with the appropriate application
        public abstract void OpenWebsite (string url);

        public SparkleControllerBase ()
        {
        }


        public virtual void Initialize ()
        {
                       // Create the SparkleShare folder and add it to the bookmarks
            if (CreateHomeFolder ())
                AddToBookmarks ();
        }


        public void UIHasLoaded ()
        {
            if (Settings.FirstRun) {
                ShowSetupWindow (PageType.Login);
            } else {
                AddRepository();
            }
        }

        SparkleRepoBase repo = null;

        private void AddRepository ()
        {

            string backend       ="SQ";
            try {
              
            } catch (Exception e ){
                Console.WriteLine (e.InnerException);
                     Logger.LogInfo ("Controller",
                    "Failed to load '" + backend + "' backend for: " + e.Message);

                return;
            }

            repo.ChangesDetected += delegate {
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
         
            repo.Initialize ();
        }


        // Fires events for the current syncing state
        private void UpdateState ()
        {
            bool has_unsynced_repos = false;
            if (repo.Status == SyncStatus.SyncDown || repo.Status == SyncStatus.SyncUp || repo.IsBuffering) {
                OnSyncing ();
                return;

            } else if (repo.HasUnsyncedChanges) {
                has_unsynced_repos = true;
            }


            if (has_unsynced_repos)
               OnError ();
            else
                OnIdle ();
        }

        SparkleFetcher fetcher;
        public void StartFetcher (string address, string remote_path, bool fetch_prior_history)
        {
            string tmp_path = Settings.TmpPath;
			if (!Directory.Exists (tmp_path)) {
                Directory.CreateDirectory (tmp_path);
                File.SetAttributes (tmp_path, File.GetAttributes (tmp_path) | FileAttributes.Hidden);
            }
            DownloadController downloadController = DownloadController.GetInstance();
            this.fetcher.Failed += delegate {
                FolderFetchError (this.fetcher.RemoteUrl.ToString (), this.fetcher.Errors);
                StopFetcher ();
            };
            
            downloadController.ProgressChanged += delegate (double percentage) {
                FolderFetching (percentage);
            };

            this.fetcher.Start ();
        }


        public void StopFetcher ()
        {
            this.fetcher.Stop ();

            if (Directory.Exists (this.fetcher.TargetFolder)) {
                try {
                    Directory.Delete (this.fetcher.TargetFolder, true);
                    Logger.LogInfo ("Controller", "Deleted " + this.fetcher.TargetFolder);

                } catch (Exception e) {
                    Logger.LogInfo ("Controller",
                        "Failed to delete '" + this.fetcher.TargetFolder + "': " + e.Message);
                }
            }

            this.fetcher.Dispose ();
            this.fetcher = null;

        }

        public void FinishFetcher ()
        {

            this.fetcher.Complete ();
            try {
                Directory.Move (this.fetcher.TargetFolder, Settings.HomePath);

            } catch (Exception e) {
                Logger.LogInfo ("Controller", "Error moving directory: " + e.Message);
                return;
            }

            FolderFetched (this.fetcher.RemoteUrl.ToString (), this.fetcher.Warnings.ToArray ());
            AddRepository ();
            FolderListChanged ();

            this.fetcher.Dispose ();
            this.fetcher = null;
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
            OpenFolder (Settings.HomePath);
        }

       


        public void ToggleNotifications () {
            Settings.NotificationsEnabled = !Settings.NotificationsEnabled;
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
    }
}
