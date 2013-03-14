
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

using MonoMac.Foundation;
using MonoMac.AppKit;
using MonoMac.ObjCRuntime;
using MonoMac.WebKit;

using Mono.Unix;
using GreenQloud.Repository;

namespace GreenQloud {

    public class SparkleSetup : SparkleSetupWindow {

        public SparkleSetupController Controller = new SparkleSetupController ();

        private NSButton ContinueButton;
        private NSButton TryAgainButton;
        private NSButton CancelButton;
        private NSButton SkipTutorialButton;
        private NSButton StartupCheckButton;
        private NSButton OpenFolderButton;
        private NSButton FinishButton;
        private NSImage SlideImage;
        private NSImageView SlideImageView;
        private NSProgressIndicator ProgressIndicator;
        private NSTextField EmailLabel;
        private NSTextField FullNameTextField;
        private NSTextField FullNameLabel;
        private NSTextField WarningTextField;
        private NSImage WarningImage;
        private NSImageView WarningImageView;
        private HyperLink hDescription;
        public SparkleSetup () : base ()
        {
            Controller.HideWindowEvent += delegate {
                InvokeOnMainThread (delegate {
                    PerformClose (this);
                });
            };

            Controller.ShowWindowEvent += delegate {
                InvokeOnMainThread (delegate {
                    OrderFrontRegardless ();
                });
            };

            Controller.ChangePageEvent += delegate (PageType type, string [] warnings) {
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


        public void ShowPage (PageType type, string [] warnings)
        {
            if (type == PageType.Login) {
                Header      = string.Format("Welcome to {0} for StorageQloud™!", GlobalSettings.ApplicationName);
                Description = "";

                string pretext = "<div style='font-size: 10pt; font-family: \"Lucida Grande\";'>With QloudSync you can sync your photos, music, documents and movies to and from your computer to StorageQloud, " +
                    "the truly green cloud storage run on 100% renewable energy! All you need to get started is a GreenQloud Username and Password. " +
                    "Don't have one? No problem! Click ";
                    string linktext = "here";
                    string postext =" to register in one easy step it's totally free to try.</div>";

                hDescription = new HyperLink (pretext, linktext, postext, "https://my.greenqloud.com/registration", ""){
                    Frame           = new RectangleF (190, Frame.Height - 210, 640 - 240, 105)
                };

                FullNameLabel = new NSTextField () {
                    Alignment       = NSTextAlignment.Right,
                    BackgroundColor = NSColor.WindowBackground,
                    Bordered        = false,
                    Editable        = false,
                    Frame           = new RectangleF (165, Frame.Height - 254, 160, 17),
                    StringValue     = "Username:",
                    Font            = NSFontManager.SharedFontManager.FontWithFamily (
                        "Lucida Grande", NSFontTraitMask.Condensed, 0, 13)
                };

                FullNameTextField = new NSTextField () {
                    Frame       = new RectangleF (330, Frame.Height - 258, 196, 22),
                    Delegate    = new SparkleTextFieldDelegate ()
                };

                EmailLabel = new NSTextField () {
                    Alignment       = NSTextAlignment.Right,
                    BackgroundColor = NSColor.WindowBackground,
                    Bordered        = false,
                    Editable        = false,
                    Frame           = new RectangleF (165, Frame.Height - 284, 160, 17),
                    StringValue     = "Password:",
                    Font            = NSFontManager.SharedFontManager.FontWithFamily (
                        "Lucida Grande", NSFontTraitMask.Condensed, 0, 13)
                };

                NSSecureTextField PasswordTextField = new NSSecureTextField(){
                    Frame       = new RectangleF (330, Frame.Height - 288, 196, 22),
                    Delegate    = new SparkleTextFieldDelegate (),
                };


                NSTextField MessageLabel = new NSTextField () {
                    Alignment       = NSTextAlignment.Left,
                    BackgroundColor = NSColor.WindowBackground,
                    Bordered        = false,
                    Editable        = false,
                    Frame           = new RectangleF (325, 60 , Frame.Width, 60),
                    StringValue     = "The password you entered is incorrect.\n",
                    Font            = NSFontManager.SharedFontManager.FontWithFamily (
                        "Lucida Grande", NSFontTraitMask.Bold, 0, 10),
                    TextColor = NSColor.Red
                };


                CancelButton = new NSButton () {
                    Title = "Cancel"
                };

                ContinueButton = new NSButton () {
                    Title    = "Continue",
                    Enabled  = false
                };

                (PasswordTextField.Delegate as SparkleTextFieldDelegate).StringValueChanged += delegate {
                    if(PasswordTextField.StringValue.Length < 3)
                        ContinueButton.Enabled = false;
                    else
                        ContinueButton.Enabled = true;
                };

                CancelButton.Activated += delegate {
                    Controller.PageCancelled ();
                    Program.Controller.Quit();
                };

                ContinueButton.Activated += delegate {
                    try{
                        StorageQloudRepo.Authenticate (FullNameTextField.StringValue, PasswordTextField.StringValue);
                        Credential.Username = FullNameTextField.StringValue;
                        Controller.AddPageCompleted (FullNameTextField.StringValue, PasswordTextField.StringValue);
                    }
                    catch (System.Net.WebException)
                    {
                        ContentView.AddSubview (MessageLabel);
                    }
                };

                ContentView.AddSubview (hDescription);
                ContentView.AddSubview (FullNameLabel);
                ContentView.AddSubview (FullNameTextField);
                ContentView.AddSubview (EmailLabel);
                ContentView.AddSubview (PasswordTextField);


                Buttons.Add (ContinueButton);
                Buttons.Add (CancelButton);
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
                    DoubleValue   = Controller.ProgressBarPercentage
                };

                ProgressIndicator.StartAnimation (this);

                NSTextField TimeRemaining = new NSTextField () {
                    Alignment       = NSTextAlignment.Left,
                    BackgroundColor = NSColor.WindowBackground,
                    Bordered        = false,
                    Editable        = false,
                    Frame           = new RectangleF (190, Frame.Height - 230, 640 - 150 - 80, 20),
                    StringValue     = "",
                    Font            = SparkleUI.Font
                };

                CancelButton = new NSButton () {
                    Title = "Cancel"
                };

                FinishButton = new NSButton () {
                    Title = "Finish",
                    Enabled = false
                };

                Controller.UpdateProgressBarEvent += delegate (double percentage) {
                    InvokeOnMainThread (() => {
                        ProgressIndicator.DoubleValue = percentage;
                    });
                };

                Controller.UpdateTimeRemaningEvent += delegate (double time){
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
                                TimeRemaining.StringValue = string.Format("Time remaining: {0}", stime);}
                        catch{

                        }
                    });
                };

                CancelButton.Activated += delegate {

                    Controller.SyncingCancelled ();

                };

                ContentView.AddSubview (ProgressIndicator);
                ContentView.AddSubview (TimeRemaining);
                Buttons.Add (FinishButton);
                Buttons.Add (CancelButton);
            }
            if (type == PageType.Finished) {
                Header      = "Your shared project is ready!";
                Description = string.Format("You can find the files in your {0} folder.", GlobalSettings.ApplicationName);


                if (warnings.Length > 0) {
                    WarningImage = NSImage.ImageNamed ("NSInfo");
                    WarningImage.Size = new SizeF (24, 24);

                    WarningImageView = new NSImageView () {
                        Image = WarningImage,
                        Frame = new RectangleF (200, Frame.Height - 175, 24, 24)
                    };

                    WarningTextField = new NSTextField () {
                        Frame           = new RectangleF (235, Frame.Height - 245, 325, 100),
                        StringValue     = warnings [0],
                        BackgroundColor = NSColor.WindowBackground,
                        Bordered        = false,
                        Editable        = false,
                        Font            = SparkleUI.Font
                    };

                    ContentView.AddSubview (WarningImageView);
                    ContentView.AddSubview (WarningTextField);
                }


                OpenFolderButton = new NSButton () {
                    Title = "Show folder"
                };

                FinishButton = new NSButton () {
                    Title = "Finish"
                };


                OpenFolderButton.Activated += delegate {
                    Controller.OpenFolderClicked ();
                };

                FinishButton.Activated += delegate {
                    Controller.FinishPageCompleted ();
                };


                Buttons.Add (FinishButton);
                Buttons.Add (OpenFolderButton);

                NSApplication.SharedApplication.RequestUserAttention (NSRequestUserAttentionType.CriticalRequest);
            }

            if (type == PageType.Tutorial) {
                string slide_image_path = Path.Combine (NSBundle.MainBundle.ResourcePath,
                    "Pixmaps", "tutorial-slide-" + Controller.TutorialPageNumber + ".png");

                SlideImage = new NSImage (slide_image_path) {
                    Size = new SizeF (350, 200)
                };

                SlideImageView = new NSImageView () {
                    Image = SlideImage,
                    Frame = new RectangleF (215, Frame.Height - 350, 350, 200)
                };

                ContentView.AddSubview (SlideImageView);


                switch (Controller.TutorialPageNumber) {

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
                            Controller.TutorialSkipped ();
                        };

                        ContinueButton.Activated += delegate {
                            Controller.TutorialPageCompleted ();
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
                            Controller.TutorialPageCompleted ();
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
                            Controller.TutorialPageCompleted ();
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
                            Controller.StartupItemChanged (StartupCheckButton.State == NSCellStateValue.On);
                        };

                        FinishButton.Activated += delegate {
                            Controller.TutorialPageCompleted ();
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
