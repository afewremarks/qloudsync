
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;

 

namespace GreenQloud {

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

        public event UpdateTimeRemaningEventHandler UpdateTimeRemaningEvent = delegate { };
        public delegate void UpdateTimeRemaningEventHandler (double time);

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

                    } else if (!RuntimeSettings.FirstRun && TutorialPageNumber == 0) {
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
                //this.create_startup_item = true;


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
                !string.IsNullOrEmpty (remote_path) && !remote_path.Contains ("\""));

            UpdateAddProjectButtonEvent (fields_valid);
        }


        public void AddPageCompleted (string username, string password)
        {
            ProgressBarPercentage = 1.0;
            ChangePageEvent (PageType.Syncing, null);

            Program.Controller.FolderFetched    += AddPageFetchedDelegate;
            Program.Controller.FolderFetchError += AddPageFetchErrorDelegate;
            Program.Controller.FolderFetching   += SyncingPageFetchingDelegate;


            new Thread (() => {
                Program.Controller.SyncStart ();
            }).Start ();

        }

        // The following private methods are
        // delegates used by the previous method

        private void AddPageFetchedDelegate (string [] warnings)
        {
            ChangePageEvent (PageType.Finished, warnings);

            Program.Controller.FolderFetched    -= AddPageFetchedDelegate;
            Program.Controller.FolderFetchError -= AddPageFetchErrorDelegate;
            Program.Controller.FolderFetching   -= SyncingPageFetchingDelegate;
        }

        private void AddPageFetchErrorDelegate (string [] errors)
        {
            ChangePageEvent (PageType.Error, errors);

            Program.Controller.FolderFetched    -= AddPageFetchedDelegate;
            Program.Controller.FolderFetchError -= AddPageFetchErrorDelegate;
            Program.Controller.FolderFetching   -= SyncingPageFetchingDelegate;
        }

        private void SyncingPageFetchingDelegate (double percentage, double time)
        {
            ProgressBarPercentage = percentage;
            UpdateProgressBarEvent (ProgressBarPercentage);
            UpdateTimeRemaningEvent (time);
        }

        public void SyncingCancelled ()
        {
            Program.Controller.SyncStop ();

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
            Program.Controller.OpenSparkleShareFolder();
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
