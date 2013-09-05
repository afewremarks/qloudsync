
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

using MonoMac.Foundation;
using MonoMac.AppKit;
using MonoMac.ObjCRuntime;
using MonoMac.WebKit;

using GreenQloud.Repository;
using QloudSync.Repository;
using System.Threading;

namespace GreenQloud {

    public class SparkleSetup : SparkleSetupWindow {

        public SparkleSetupController SparkleSetupController = new SparkleSetupController ();

        private NSButton RegisterButton;
        private NSButton ContinueButton;
        private NSButton TryAgainButton;
        private NSButton CancelButton;
        private NSButton SkipTutorialButton;
        private NSButton StartupCheckButton;
        private NSButton OpenFolderButton;
        private NSButton FinishButton;
        private NSButton StopButton;
        private NSImage SlideImage;
        private NSImageView SlideImageView;
        private NSProgressIndicator ProgressIndicator;
        private NSTextField EmailLabel;
        private NSTextField FullNameTextField;
        private NSTextField FullNameLabel;
        private NSTextField DescriptionText;
        private NSTextField WarningTextField;
        private NSTextField WinningText;
        private NSTextField DisclaimText;
        private NSImage WarningImage;
        private NSImageView WarningImageView;
        private HyperLink hDescription;
        public SparkleSetup () : base ()
        {
            SparkleSetupController.HideWindowEvent += delegate {
                InvokeOnMainThread (delegate {
                    PerformClose (this);
                });
            };

            SparkleSetupController.ShowWindowEvent += delegate {
                InvokeOnMainThread (delegate {
                    OrderFrontRegardless ();
                });
            };

            SparkleSetupController.ChangePageEvent += delegate (PageType type, string [] warnings) {
                using (var a = new NSAutoreleasePool ())
                {
                    InvokeOnMainThread (delegate {
                        Reset ();
                        ShowPage (type, warnings);
                        ShowAll ();
                    });
                }
            };
        }

        public static Action LoadingStart = delegate {};
        public static Action CalculatingStart = delegate {};
        public static Action SynchronizingStart = delegate {};
        public static Action LoadingDone = delegate {};
        public static Action CalculatingDone = delegate {};
        public static Action SynchronizingDone = delegate {};

        public void ShowPage (PageType type, string [] warnings)
        {
            if (type == PageType.Login) {
                background_image_path = Path.Combine (NSBundle.MainBundle.ResourcePath, "Pixmaps", "loginScreen.png");

                Console.WriteLine (background_image_path);

                FullNameTextField = new NSTextField () {
                    Frame       = new RectangleF (90, Frame.Height - 347, 270, 47),
                    Delegate    = new SparkleTextFieldDelegate (),
                    Bordered = false,
                    Alignment = NSTextAlignment.Center
                };

                NSSecureTextField PasswordTextField = new NSSecureTextField(){
                    Frame       = new RectangleF (90, Frame.Height - 407, 270, 47),
                    Delegate    = new SparkleTextFieldDelegate (),
                    Bordered = false,
                    Alignment = NSTextAlignment.Center
                };
                NSTextField MessageLabel = new NSTextField () {
                    Alignment       = NSTextAlignment.Left,
                    BackgroundColor = NSColor.Clear,
                    Bordered        = false,
                    Editable        = false,
                    Frame           = new RectangleF (88, 70 , Frame.Width, 60),
                    StringValue     = "The username or password you entered is incorrect.\n",
                    Font            = NSFontManager.SharedFontManager.FontWithFamily (
                        "Lucida Grande", NSFontTraitMask.Bold, 0, 10),
                    TextColor = NSColor.Red
                };


                RegisterButton = new NSButton () {
                    Frame = new RectangleF (30, 12, 150, 32),
                    Title = "Create Account"
                };
                ContinueButton = new NSButton () {
                    Frame = new RectangleF (270, 12, 150, 32),
                    Title    = "Log in",
                    Enabled  = false
                };

                (PasswordTextField.Delegate as SparkleTextFieldDelegate).StringValueChanged += delegate {
                    if(PasswordTextField.StringValue.Length < 3)
                        ContinueButton.Enabled = false;
                    else
                        ContinueButton.Enabled = true;
                };

                RegisterButton.Activated += delegate {
                    Program.Controller.OpenWebsite("https://my.greenqloud.com/registration/qloudsync");
                };

                /*CancelButton.Activated += delegate {
                    SparkleSetupController.PageCancelled ();
                    Program.Controller.Quit();
                };*/

                ContinueButton.Activated += delegate {
                    try{
                        S3Connection.Authenticate (FullNameTextField.StringValue, PasswordTextField.StringValue);
                        Credential.Username = FullNameTextField.StringValue;
                        SparkleSetupController.AddPageCompleted (FullNameTextField.StringValue, PasswordTextField.StringValue);
                    }
                    catch (System.Net.WebException)
                    {
                        ContentView.AddSubview (MessageLabel);
                    }
                };

                /*ContentView.AddSubview (DescriptionText);
                ContentView.AddSubview (FullNameLabel);
                ContentView.AddSubview (FullNameTextField);
                ContentView.AddSubview (EmailLabel);
                ContentView.AddSubview (PasswordTextField);*/

                ContentView.AddSubview (FullNameTextField);
                ContentView.AddSubview (PasswordTextField);

                Buttons.Add (ContinueButton);
                Buttons.Add (RegisterButton);
            }

            if (type == PageType.Syncing) {
                Header      = "Winning! QloudSync is now connected to your \nGreenQloud account…";
                Description = "You have successfully logged into GreenQloud and now QloudSync will find your truly green™ files in StorageQloud and sync them to your computer in a folder named StorageQloud.";


                ProgressIndicator = new NSProgressIndicator () {
                    Frame         = new RectangleF (190, Frame.Height - 250, 640 - 150 - 80, 20),
                    Style         = NSProgressIndicatorStyle.Bar,
                    MinValue      = 0.0,
                    MaxValue      = 100.0,
                    Indeterminate = false,
                    DoubleValue   = SparkleSetupController.ProgressBarPercentage
                };

                ProgressIndicator.StartAnimation (this);

                NSTextField Loading = new NSTextField () {
                    Alignment       = NSTextAlignment.Left,
                    BackgroundColor = NSColor.WindowBackground,
                    Bordered        = false,
                    Editable        = false,
                    Frame           = new RectangleF (190, Frame.Height - 230, 640 - 150 - 80, 20),
                    StringValue     = "Starting synchronizers",
                    Font            = SparkleUI.Font
                };

                NSTextField Calculating = new NSTextField () {
                    Alignment       = NSTextAlignment.Left,
                    BackgroundColor = NSColor.WindowBackground,
                    Bordered        = false,
                    Editable        = false,
                    Frame           = new RectangleF (190, Frame.Height - 260, 640 - 150 - 80, 20),
                    StringValue     = "Calculating changes with remote repository",
                    Font            = SparkleUI.Font
                };
                NSTextField Synchronizing = new NSTextField () {
                    Alignment       = NSTextAlignment.Left,
                    BackgroundColor = NSColor.WindowBackground,
                    Bordered        = false,
                    Editable        = false,
                    Frame           = new RectangleF (190, Frame.Height - 290, 640 - 150 - 80, 20),
                    StringValue     = "Synchronizing changes",
                    Font            = SparkleUI.Font
                };




                StopButton = new NSButton () {
                    Title = "Cancel",
                    Enabled = false
                };

                FinishButton = new NSButton () {
                    Title = "Finish",
                    Enabled = false
                };

                SparkleSetupController.UpdateProgressBarEvent += delegate (double percentage) {
                    InvokeOnMainThread (() => {
                        ProgressIndicator.DoubleValue = percentage;
                    });
                };

                SparkleSetupController.UpdateTimeRemaningEvent += delegate (double time){
                    InvokeOnMainThread (() => {
                        try{
                            TimeSpan t = TimeSpan.FromSeconds(time);
                            string stime = "";
                            if (t.Hours >0)
                                stime = string.Format("{0} hours and {1} minutes",t.Hours, t.Minutes);
                            else if (t.Minutes>0)
                                stime = string.Format("{0} minutes and {1} seconds",t.Minutes, t.Seconds);
                            else if(t.Seconds>0)
                                stime = string.Format("{0} seconds", t.Seconds); 
                            else
                                stime = "estimating";
                                //TimeRemaining.StringValue = string.Format("Time remaining: {0}", stime);
                        }catch{

                        }
                    });
                };

                StopButton.Activated += delegate {
                    SparkleSetupController.SyncingCancelled ();
                };

                LoadingStart += delegate() {
                    InvokeOnMainThread (() => {
                        ContentView.AddSubview (Loading);
                        if(!Loading.StringValue.EndsWith("✓")) {
                            if(Loading.StringValue.EndsWith("..."))
                                Loading.StringValue = Loading.StringValue.Replace("...", "");
                            else
                                Loading.StringValue += ".";
                        }

                        if(!StopButton.Enabled)
                            StopButton.Enabled = true;
                    });
                };
                CalculatingStart += delegate() {
                    InvokeOnMainThread (() => {
                        ContentView.AddSubview (Calculating);
                        if(!Calculating.StringValue.EndsWith("✓")) {
                            if(Calculating.StringValue.EndsWith("..."))
                                Calculating.StringValue = Calculating.StringValue.Replace("...", "");
                            else
                                Calculating.StringValue += ".";
                        }
                    });
                };
                SynchronizingStart += delegate() {
                    InvokeOnMainThread (() => {
                        ContentView.AddSubview (Synchronizing);
                        if(!Synchronizing.StringValue.EndsWith("✓")) {
                            if(Synchronizing.StringValue.EndsWith("..."))
                                Synchronizing.StringValue = Synchronizing.StringValue.Replace("...", "");
                            else
                                Synchronizing.StringValue += ".";
                        }
                    });
                };
                LoadingDone += delegate() {
                    InvokeOnMainThread (() => {
                        if(!Loading.StringValue.EndsWith("✓")){
                            Loading.StringValue.Replace(".", "");
                            Loading.StringValue += "... ✓";
                        }
                    });
                };
                CalculatingDone += delegate() {
                    InvokeOnMainThread (() => {
                        if(!Calculating.StringValue.EndsWith("✓")){
                            Calculating.StringValue.Replace(".", "");
                            Calculating.StringValue += "... ✓";
                        }
                    });
                };
                SynchronizingDone += delegate() {
                    InvokeOnMainThread (() => {
                        if(!Synchronizing.StringValue.EndsWith("✓")){
                            Synchronizing.StringValue.Replace(".", "");
                            Synchronizing.StringValue += "... ✓";
                        }
                    });
                };
                Thread thread = new Thread(delegate() {
                    while(Program.Controller.StartState != Controller.START_STATE.SYNC_DONE) {
                        if(Program.Controller.StartState != Controller.START_STATE.NULL){
                            if((int)Program.Controller.StartState >= (int)Controller.START_STATE.LOAD_START){
                                SparkleSetup.LoadingStart();
                            }
                            if((int)Program.Controller.StartState >= (int)Controller.START_STATE.CALCULATING_START){
                                SparkleSetup.CalculatingStart();
                            }
                            if((int)Program.Controller.StartState >= (int)Controller.START_STATE.SYNC_START){
                                SparkleSetup.SynchronizingStart();
                            }

                            if((int)Program.Controller.StartState >= (int)Controller.START_STATE.LOAD_DONE){
                                SparkleSetup.LoadingDone();
                            }
                            if((int)Program.Controller.StartState >= (int)Controller.START_STATE.CALCULATING_DONE){
                                SparkleSetup.CalculatingDone();
                            }
                            if((int)Program.Controller.StartState >= (int)Controller.START_STATE.SYNC_DONE){
                                SparkleSetup.SynchronizingDone();
                            }
                        }
                        Thread.Sleep(2000);
                    }
                });
                thread.Start ();

                Buttons.Add (FinishButton);
                Buttons.Add (StopButton);
            }

            if (type == PageType.Finished) {
                this.background_image_path = Path.Combine (NSBundle.MainBundle.ResourcePath, "Pixmaps", "getStarted.png");
                OpenFolderButton = new NSButton () {
                    Title = "Show folder"
                };
                OpenFolderButton.Activated += delegate {
                    SparkleSetupController.FinishPageCompleted ();
                    SparkleSetupController.GetStartedClicked ();

                };
                Buttons.Add (OpenFolderButton);
                NSApplication.SharedApplication.RequestUserAttention (NSRequestUserAttentionType.CriticalRequest);
            }

            if (type == PageType.Tutorial) {
                string slide_image_path = Path.Combine (NSBundle.MainBundle.ResourcePath,
                    "Pixmaps", "tutorial-slide-" + SparkleSetupController.TutorialPageNumber + ".png");

                SlideImage = new NSImage (slide_image_path) {
                    Size = new SizeF (350, 200)
                };

                SlideImageView = new NSImageView () {
                    Image = SlideImage,
                    Frame = new RectangleF (215, Frame.Height - 350, 350, 200)
                };

                ContentView.AddSubview (SlideImageView);


                switch (SparkleSetupController.TutorialPageNumber) {

                    case 1: {
                        Header      = "What's happening next?";
                    Description = string.Format("{0} creates a special folder on your computer ", GlobalSettings.ApplicationName) +
                            "that will keep track of your projects.";


                        SkipTutorialButton = new NSButton () {
                            Title = "Skip Tutorial"
                        };

                        ContinueButton = new NSButton () {
                            Title = "Continue"
                        };


                        SkipTutorialButton.Activated += delegate {
                            SparkleSetupController.TutorialSkipped ();
                        };

                        ContinueButton.Activated += delegate {
                            SparkleSetupController.TutorialPageCompleted ();
                        };


                        ContentView.AddSubview (SlideImageView);

                        Buttons.Add (ContinueButton);
                        Buttons.Add (SkipTutorialButton);

                        break;
                    }

                    case 2: {
                        Header      = "Sharing files with others";
                        Description = "All files added to your project folders are synced automatically with " +
                            "the host and your team members.";

                        ContinueButton = new NSButton () {
                            Title = "Continue"
                        };

                        ContinueButton.Activated += delegate {
                            SparkleSetupController.TutorialPageCompleted ();
                        };

                        Buttons.Add (ContinueButton);

                        break;
                    }

                    case 3: {
                        Header      = "The status icon is here to help";
                        Description = "It shows the syncing progress, provides easy access to " +
                            "your projects and let's you view recent changes.";

                        ContinueButton = new NSButton () {
                            Title = "Continue"
                        };

                        ContinueButton.Activated += delegate {
                            SparkleSetupController.TutorialPageCompleted ();
                        };

                        Buttons.Add (ContinueButton);

                        break;
                    }

                    case 4: {
                        Header      = "Adding projects to "+GlobalSettings.ApplicationName;
                        Description = "You can do this through the status icon menu, or by clicking " +
                            "magic buttons on webpages that look like this:";


                        StartupCheckButton = new NSButton () {
                            Frame = new RectangleF (190, Frame.Height - 400, 300, 18),
                            Title = string.Format("Add {0} to startup items",GlobalSettings.ApplicationName),
                            State = NSCellStateValue.On
                        };

                        StartupCheckButton.SetButtonType (NSButtonType.Switch);

                        FinishButton = new NSButton () {
                            Title = "Finish"
                        };

                        SlideImage.Size = new SizeF (350, 64);


                        StartupCheckButton.Activated += delegate {
                            SparkleSetupController.StartupItemChanged (StartupCheckButton.State == NSCellStateValue.On);
                        };

                        FinishButton.Activated += delegate {
                            SparkleSetupController.TutorialPageCompleted ();
                        };


                        ContentView.AddSubview (StartupCheckButton);
                        Buttons.Add (FinishButton);

                        break;
                    }
                }
            }
        }
    }


    public class SparkleTextFieldDelegate : NSTextFieldDelegate {

        public event StringValueChangedHandler StringValueChanged;
        public delegate void StringValueChangedHandler ();


        public override void Changed (NSNotification notification)
        {
            if (StringValueChanged != null)
                StringValueChanged ();
        }
    }


    public class SparkleTableViewDelegate : NSTableViewDelegate {

        public event SelectionChangedHandler SelectionChanged;
        public delegate void SelectionChangedHandler ();


        public override void SelectionDidChange (NSNotification notification)
        {
            if (SelectionChanged != null)
                SelectionChanged ();
        }
    }
}
