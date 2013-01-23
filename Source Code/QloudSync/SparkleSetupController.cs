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
using System.Text.RegularExpressions;
using System.Threading;

 

namespace QloudSync {

    public enum PageType {
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
        Login
    }

    public enum FieldState {
        Enabled,
        Disabled
    }


    public class SparkleSetupController {

        public event Action ShowWindowEvent = delegate { };
        public event Action HideWindowEvent = delegate { };

        public event ChangePageEventHandler ChangePageEvent = delegate { };
        public delegate void ChangePageEventHandler (PageType page, string [] warnings);
        
        public event UpdateProgressBarEventHandler UpdateProgressBarEvent = delegate { };
        public delegate void UpdateProgressBarEventHandler (double percentage);

        public event UpdateSetupContinueButtonEventHandler UpdateSetupContinueButtonEvent = delegate { };
        public delegate void UpdateSetupContinueButtonEventHandler (bool button_enabled);

        public event UpdateCryptoSetupContinueButtonEventHandler UpdateCryptoSetupContinueButtonEvent = delegate { };
        public delegate void UpdateCryptoSetupContinueButtonEventHandler (bool button_enabled);

        public event UpdateCryptoPasswordContinueButtonEventHandler UpdateCryptoPasswordContinueButtonEvent = delegate { };
        public delegate void UpdateCryptoPasswordContinueButtonEventHandler (bool button_enabled);

        public event UpdateAddProjectButtonEventHandler UpdateAddProjectButtonEvent = delegate { };
        public delegate void UpdateAddProjectButtonEventHandler (bool button_enabled);

        public event ChangeAddressFieldEventHandler ChangeAddressFieldEvent = delegate { };
        public delegate void ChangeAddressFieldEventHandler (string text, string example_text, FieldState state);

        public event ChangePathFieldEventHandler ChangePathFieldEvent = delegate { };
        public delegate void ChangePathFieldEventHandler (string text, string example_text, FieldState state);

        public bool WindowIsOpen { get; private set; }
        public int TutorialPageNumber { get; private set; }
        public string SyncingFolder { get; private set; }
        public double ProgressBarPercentage  { get; private set; }

    

        public bool FetchPriorHistory {
            get {
                return this.fetch_prior_history;
            }
        }

        private PageType current_page;
        private string saved_address     = "";
        private string saved_remote_path = "";
        private bool create_startup_item = true;
        private bool fetch_prior_history = false;


        public SparkleSetupController ()
        {
            ChangePageEvent += delegate (PageType page_type, string [] warnings) {
                this.current_page = page_type;
            };

            TutorialPageNumber = 0;
            SyncingFolder      = "";

            Program.Controller.ShowSetupWindowEvent += delegate (PageType page_type) {
                if (page_type == PageType.CryptoSetup || page_type == PageType.CryptoPassword) {
                    ChangePageEvent (page_type, null);
                    return;
                }

                if (this.current_page == PageType.Syncing ||
                    this.current_page == PageType.Finished ||
                    this.current_page == PageType.CryptoSetup ||
                    this.current_page == PageType.CryptoPassword) {

                    ShowWindowEvent ();
                    return;
                }

                if (page_type == PageType.Add) {
                    if (WindowIsOpen) {
                        if (this.current_page == PageType.Error ||
                            this.current_page == PageType.Finished ||
                            this.current_page == PageType.None) {

                            ChangePageEvent (PageType.Add, null);
                        }

                        ShowWindowEvent ();

                    } else if (!Program.Controller.FirstRun && TutorialPageNumber == 0) {
                        WindowIsOpen = true;
                        ChangePageEvent (PageType.Add, null);
                        ShowWindowEvent ();
                    }

                    return;
                }

                WindowIsOpen = true;
                ChangePageEvent (page_type, null);
                ShowWindowEvent ();
            };
        }


        public void PageCancelled ()
        {
            this.fetch_prior_history = false;

            WindowIsOpen = false;
            HideWindowEvent ();
        }


        public void CheckSetupPage (string full_name, string email)
        {
            full_name = full_name.Trim ();
            email     = email.Trim ();

            bool fields_valid = (!string.IsNullOrEmpty (full_name) && IsValidEmail (email));
            UpdateSetupContinueButtonEvent (fields_valid);
        }

        
        public void SetupPageCancelled ()
        {
            Program.Controller.Quit ();
        }
        
        
        public void SetupPageCompleted (string full_name, string email)
        {
            Program.Controller.CurrentUser = new SparkleUser (full_name, email);

            /*new Thread (() => {
                string link_code_file_path = Path.Combine (Program.Controller.FoldersPath, "Your link code.txt");

                if (File.Exists (link_code_file_path)) {
                    string name = Program.Controller.CurrentUser.Name.Split (" ".ToCharArray ()) [0];

                    if (name.EndsWith ("s"))
                        name += "'";
                    else
                        name += "'s";

                    string new_file_path = Path.Combine (Program.Controller.FoldersPath, name + " link code.txt");
                    File.Move (link_code_file_path, new_file_path);
                }

            }).Start ();*/

           // TutorialPageNumber = 1;
            //ChangePageEvent (PageType.Tutorial, null);
        }


        public void TutorialSkipped ()
        {
            TutorialPageNumber = 4;
            ChangePageEvent (PageType.Tutorial, null);
        }


        public void HistoryItemChanged (bool fetch_prior_history)
        {
            this.fetch_prior_history = fetch_prior_history;
        }


        public void TutorialPageCompleted ()
        {
            TutorialPageNumber++;

            if (TutorialPageNumber == 5) {
                TutorialPageNumber = 0;

                WindowIsOpen = false;
                HideWindowEvent ();

                if (this.create_startup_item)
                    new Thread (() => Program.Controller.CreateStartupItem ()).Start ();

            } else {
                ChangePageEvent (PageType.Tutorial, null);
            }
        }



        public void StartupItemChanged (bool create_startup_item)
        {
            this.create_startup_item = create_startup_item;
        }


        public void CheckAddPage (string address, string remote_path, int selected_plugin)
        {
            address     = address.Trim ();
            remote_path = remote_path.Trim ();

            if (selected_plugin == 0)
                this.saved_address = address;

            this.saved_remote_path = remote_path;

            bool fields_valid = (!string.IsNullOrEmpty (address) &&
                !string.IsNullOrEmpty (remote_path) && !remote_path.Contains ("\"") && Program.Controller.Folders.Count == 0);

            UpdateAddProjectButtonEvent (fields_valid);
        }


        public void AddPageCompleted (string address, string remote_path)
        {

            Program.Controller.CurrentUser = new SparkleUser ("empty", "empty");
            QloudSync.Security.Credential.User = address;
            QloudSync.Security.Credential.Password = remote_path;
            address = "https://s.greenqloud.com";
            remote_path =  QloudSync.Security.Credential.User + "-default.SQ";

            SyncingFolder = Path.GetFileNameWithoutExtension (remote_path);
            SyncingFolder = SyncingFolder.Replace ("-crypto", "");
            ProgressBarPercentage = 1.0;
            
            ChangePageEvent (PageType.Syncing, null);
            
            address = Uri.EscapeUriString (address.Trim ());
            remote_path = remote_path.Trim ();
            remote_path = remote_path.TrimEnd ("/".ToCharArray ());

            Program.Controller.FolderFetched    += AddPageFetchedDelegate;
            Program.Controller.FolderFetchError += AddPageFetchErrorDelegate;
            Program.Controller.FolderFetching   += SyncingPageFetchingDelegate;
            
            /*new Thread (() => {
                Program.Controller.StartFetcher (address, remote_path, this.fetch_prior_history);
                
            }).Start ();*/

        }

        // The following private methods are
        // delegates used by the previous method

        private void AddPageFetchedDelegate (string remote_url, string [] warnings)
        {
            ChangePageEvent (PageType.Finished, warnings);

            Program.Controller.FolderFetched    -= AddPageFetchedDelegate;
            Program.Controller.FolderFetchError -= AddPageFetchErrorDelegate;
            Program.Controller.FolderFetching   -= SyncingPageFetchingDelegate;
        }

        private void AddPageFetchErrorDelegate (string remote_url, string [] errors)
        {
            ChangePageEvent (PageType.Error, errors);

            Program.Controller.FolderFetched    -= AddPageFetchedDelegate;
            Program.Controller.FolderFetchError -= AddPageFetchErrorDelegate;
            Program.Controller.FolderFetching   -= SyncingPageFetchingDelegate;
        }

        private void SyncingPageFetchingDelegate (double percentage)
        {
            ProgressBarPercentage = percentage;
            UpdateProgressBarEvent (ProgressBarPercentage);
        }



        public void SyncingCancelled ()
        {
            Program.Controller.StopFetcher ();

            ChangePageEvent (PageType.Login, null);
        }


        public void ErrorPageCompleted ()
        {
            ChangePageEvent (PageType.Add, null);
        }


        public void CheckCryptoSetupPage (string password)
        {
            bool valid_password = (password.Length > 0 && !password.Contains (" "));
            UpdateCryptoSetupContinueButtonEvent (valid_password);
        }
        public void OpenFolderClicked ()
        {
            Program.Controller.OpenSparkleShareFolder (Path.GetFileName (""));
            FinishPageCompleted ();
        }


        public void FinishPageCompleted ()
        {
            this.fetch_prior_history = false;

            this.current_page = PageType.None;
            HideWindowEvent ();
        }


        private bool IsValidEmail (string email)
        {
            return new Regex (@"^[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,4}$", RegexOptions.IgnoreCase).IsMatch (email);
        }
    }
}
