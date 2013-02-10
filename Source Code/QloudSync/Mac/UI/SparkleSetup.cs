
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

using MonoMac.Foundation;
using MonoMac.AppKit;
using MonoMac.ObjCRuntime;
using MonoMac.WebKit;

using Mono.Unix;

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
                Header      = "Welcome to QloudSync!";


                FullNameLabel = new NSTextField () {
                    Alignment       = NSTextAlignment.Right,
                    BackgroundColor = NSColor.WindowBackground,
                    Bordered        = false,
                    Editable        = false,
                    Frame           = new RectangleF (165, Frame.Height - 234, 160, 17),
                    StringValue     = "Username:",
                    Font            = SparkleUI.Font
                };

                FullNameTextField = new NSTextField () {
                    Frame       = new RectangleF (330, Frame.Height - 238, 196, 22),
                    Delegate    = new SparkleTextFieldDelegate ()
                };

                EmailLabel = new NSTextField () {
                    Alignment       = NSTextAlignment.Right,
                    BackgroundColor = NSColor.WindowBackground,
                    Bordered        = false,
                    Editable        = false,
                    Frame           = new RectangleF (165, Frame.Height - 264, 160, 17),
                    StringValue     = "Password:",
                    Font            = SparkleUI.Font
                };

                NSSecureTextField PasswordTextField = new NSSecureTextField(){
                    Frame       = new RectangleF (330, Frame.Height - 268, 196, 22),
                    Delegate    = new SparkleTextFieldDelegate (),
                };


                NSTextField MessageLabel = new NSTextField () {
                    Alignment       = NSTextAlignment.Left,
                    BackgroundColor = NSColor.WindowBackground,
                    Bordered        = false,
                    Editable        = false,
                    Frame           = new RectangleF (325, 75 , Frame.Width, 60),
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
                };

                ContinueButton.Activated += delegate {
                    try{
                        Net.S3.ConnectionManager.Authenticate (FullNameTextField.StringValue, PasswordTextField.StringValue);
                        Credential.Username = FullNameTextField.StringValue;

                        Controller.AddPageCompleted (FullNameTextField.StringValue, PasswordTextField.StringValue);
                    }
                    catch (System.Net.WebException)
                    {
                        ContentView.AddSubview (MessageLabel);
                    }
                };

                ContentView.AddSubview (FullNameLabel);
                ContentView.AddSubview (FullNameTextField);
                ContentView.AddSubview (EmailLabel);
                ContentView.AddSubview (PasswordTextField);


                Buttons.Add (ContinueButton);
                Buttons.Add (CancelButton);
            }

            if (type == PageType.Syncing) {
                Header      = "Adding project ‘" + Controller.SyncingFolder + "’…";
                Description = "This may take a while for large projects.\nIsn't it coffee-o'clock?";


                ProgressIndicator = new NSProgressIndicator () {
                    Frame         = new RectangleF (190, Frame.Height - 200, 640 - 150 - 80, 20),
                    Style         = NSProgressIndicatorStyle.Bar,
                    MinValue      = 0.0,
                    MaxValue      = 100.0,
                    Indeterminate = false,
                    DoubleValue   = Controller.ProgressBarPercentage
                };

                ProgressIndicator.StartAnimation (this);

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

                CancelButton.Activated += delegate {

                    Controller.SyncingCancelled ();

                };


                ContentView.AddSubview (ProgressIndicator);

                Buttons.Add (FinishButton);
                Buttons.Add (CancelButton);
            }

            if (type == PageType.Error) {
                Header      = "Oops! Something went wrong…";
                Description = "Please check the following:";


                // Displaying marked up text with Cocoa is
                // a pain, so we just use a webview instead
                WebView web_view = new WebView ();
                web_view.Frame   = new RectangleF (190, Frame.Height - 525, 375, 400);

                string html = "<style>" +
                    "* {" +
                    "  font-family: 'Lucida Grande';" +
                    "  font-size: 12px; cursor: default;" +
                    "}" +
                    "body {" +
                    "  -webkit-user-select: none;" +
                    "  margin: 0;" +
                    "  padding: 3px;" +
                    "}" +
                    "li {" +
                    "  margin-bottom: 16px;" +
                    "  margin-left: 0;" +
                    "  padding-left: 0;" +
                    "  line-height: 20px;" +
                    "}" +
                    "ul {" +
                    "  padding-left: 24px;" +
                    "}" +
                    "</style>" +
                    "<ul>" +
                    "  <li>The address we've compiled. Does this look alright?</li>" +
                    "  <li>Do you have access rights to this remote project?</li>" +
                    "</ul>";

                if (warnings.Length > 0) {
                    string warnings_markup = "";

                    foreach (string warning in warnings)
                        warnings_markup += "<br><b>" + warning + "</b>";

                    html = html.Replace ("</ul>", "<li>Here's the raw error message: " + warnings_markup + "</li></ul>");
                }

                web_view.MainFrame.LoadHtmlString (html, new NSUrl (""));
                web_view.DrawsBackground = false;

                CancelButton = new NSButton () {
                    Title = "Cancel"
                };

                TryAgainButton = new NSButton () {
                    Title = "Try again…"
                };


                CancelButton.Activated += delegate {
                    Controller.PageCancelled ();
                };

                TryAgainButton.Activated += delegate {
                    Controller.ErrorPageCompleted ();
                };


                ContentView.AddSubview (web_view);

                Buttons.Add (TryAgainButton);
                Buttons.Add (CancelButton);
            }

            if (type == PageType.Finished) {
                Header      = "Your shared project is ready!";
                Description = "You can find the files in your QloudSync folder.";


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
                        Description = "QloudSync creates a special folder on your computer " +
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
                        Header      = "Adding projects to QloudSync";
                        Description = "You can do this through the status icon menu, or by clicking " +
                            "magic buttons on webpages that look like this:";


                        StartupCheckButton = new NSButton () {
                            Frame = new RectangleF (190, Frame.Height - 400, 300, 18),
                            Title = "Add QloudSync to startup items",
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
