
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

            SparkleSetupController.ChangePageEvent += delegate (Controller.PageType type, string [] warnings) {
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

        public void ShowPage (Controller.PageType type, string [] warnings)
        {
            EventHandler closeAppDelegate = delegate {
                Program.Controller.Quit();
            };
            EventHandler hiddeWindowDelegate = delegate {
                SparkleSetupController.FinishPageCompleted ();
            };

            if (type == Controller.PageType.Login) {
                this.WillClose += closeAppDelegate;

                background_image_path = Path.Combine (NSBundle.MainBundle.ResourcePath, "Pixmaps", "loginScreen.png");

                Console.WriteLine (background_image_path);

                FullNameTextField = new NSTextField () {
                    Frame       = new RectangleF (90, Frame.Height - 337, 270, 25),
                    Delegate    = new SparkleTextFieldDelegate (),
                    Bordered = false,
                    Bezeled = false,
                    FocusRingType = NSFocusRingType.None,
                    Alignment = NSTextAlignment.Center
                };

                NSSecureTextField PasswordTextField = new NSSecureTextField(){
                    Frame       = new RectangleF (90, Frame.Height - 397, 270, 25),
                    Delegate    = new SparkleTextFieldDelegate (),
                    Bordered = false,
                    Bezeled = false,
                    FocusRingType = NSFocusRingType.None,
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
                    Frame = new RectangleF (49, 18, 137, 40),
                    Image = new NSImage(Path.Combine (NSBundle.MainBundle.ResourcePath, "Pixmaps", "CreateButton159.png")),
                    Transparent = false,
                    Bordered = false,
                    Enabled  = true
                };
                ContinueButton = new NSButton () {
                    Frame = new RectangleF (264, 18, 137, 40),
                    Image = new NSImage(Path.Combine (NSBundle.MainBundle.ResourcePath, "Pixmaps", "LoginButton242.png")),
                    Transparent = false,
                    Bordered = false,
                    Enabled  = true
                };

                RegisterButton.Activated += delegate {
                    Program.Controller.OpenWebsite("https://my.greenqloud.com/registration/qloudsync");
                };

                ContinueButton.Activated += delegate {
                    try{
                        S3Connection.Authenticate (FullNameTextField.StringValue, PasswordTextField.StringValue);
                        Credential.Username = FullNameTextField.StringValue;
                        SparkleSetupController.LoginDone();
                    }
                    catch (System.Net.WebException)
                    {
                        ContentView.AddSubview (MessageLabel);
                    }
                };

                ContentView.AddSubview (FullNameTextField);
                ContentView.AddSubview (PasswordTextField);

                Buttons.Add (ContinueButton);
                Buttons.Add (RegisterButton);
            }

            if (type == Controller.PageType.ConfigureFolders) {
                this.WillClose -= closeAppDelegate;
                this.WillClose += hiddeWindowDelegate;
                //this.background_image_path = Path.Combine (NSBundle.MainBundle.ResourcePath, "Pixmaps", "getStarted.png");
                OpenFolderButton = new NSButton () {
                    Frame = new RectangleF (157, 18, 137, 40),
                    Image = new NSImage(Path.Combine (NSBundle.MainBundle.ResourcePath, "Pixmaps", "getstartedbutton.png")),
                    Transparent = false,
                    Bordered = false,
                    Enabled  = true
                };
                OpenFolderButton.Activated += delegate {
                    SparkleSetupController.Finish ();
                };
                Buttons.Add (OpenFolderButton);
                NSApplication.SharedApplication.RequestUserAttention (NSRequestUserAttentionType.CriticalRequest);
            }

            if (type == Controller.PageType.Finished) {
                this.WillClose -= closeAppDelegate;
                this.WillClose += hiddeWindowDelegate;
                this.background_image_path = Path.Combine (NSBundle.MainBundle.ResourcePath, "Pixmaps", "getStarted.png");
                OpenFolderButton = new NSButton () {
                    Frame = new RectangleF (157, 18, 137, 40),
                    Image = new NSImage(Path.Combine (NSBundle.MainBundle.ResourcePath, "Pixmaps", "getstartedbutton.png")),
                    Transparent = false,
                    Bordered = false,
                    Enabled  = true
                };
                OpenFolderButton.Activated += delegate {
                    SparkleSetupController.FinishPageCompleted ();
                    SparkleSetupController.GetStartedClicked ();
                };
                Buttons.Add (OpenFolderButton);
                NSApplication.SharedApplication.RequestUserAttention (NSRequestUserAttentionType.CriticalRequest);
            }

            if (type == Controller.PageType.Tutorial) {
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
