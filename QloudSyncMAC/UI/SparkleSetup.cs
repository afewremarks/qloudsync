
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
using GreenQloud.Model;

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
        private NSButton ChangeSQFolder;
        List<NSButton> remoteFoldersCheckboxes;
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
        private NSTextField SQFolderText;
        private NSImage WarningImage;
        private NSImageView WarningImageView;
        private HyperLink hDescription;
        private bool currentWindowCloseApplication = false;

        public SparkleSetup () : base ()
        {
            SparkleSetupController.HideWindowEvent += delegate {
                NSRunLoop.Main.BeginInvokeOnMainThread (delegate {
                    PerformClose (this);
                });
            };

            SparkleSetupController.ShowWindowEvent += delegate {
                NSRunLoop.Main.BeginInvokeOnMainThread (delegate {
                    OrderFrontRegardless ();
                });
            };

            SparkleSetupController.ChangePageEvent += delegate (Controller.PageType type, string [] warnings) {
                using (var a = new NSAutoreleasePool ())
                {
                    NSRunLoop.Main.BeginInvokeOnMainThread (delegate {
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
            EventHandler WillCloseDelegate = delegate {
                if(currentWindowCloseApplication){
                    Program.Controller.Quit();
                } else {
                    SparkleSetupController.FinishPageCompleted ();
                }
            };
            this.WillClose += WillCloseDelegate; 

            if (type == Controller.PageType.Login) {
                currentWindowCloseApplication = true;

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
                try {
                    currentWindowCloseApplication = true;

                    this.background_image_path = Path.Combine (NSBundle.MainBundle.ResourcePath, "Pixmaps", "SelectSync1.png");

                    NSTextField SQFolderTextLabel = new NSTextField () {
                        Alignment       = NSTextAlignment.Center,
                        BackgroundColor = NSColor.Clear,
                        Bordered        = false,
                        Editable        = false,
                        Frame           = new RectangleF (0, 75 , Frame.Width, 60),
                        StringValue     = "Current StorageQloud Path:",
                        Font            = NSFontManager.SharedFontManager.FontWithFamily (
                            "Lucida Grande", NSFontTraitMask.Unbold, 0, 9),
                        TextColor = NSColor.White
                    };

                    if(SQFolderText == null) {
                        SQFolderText = new NSTextField () {
                            Alignment       = NSTextAlignment.Center,
                            BackgroundColor = NSColor.Clear,
                            Bordered        = false,
                            Editable        = false,
                            Frame           = new RectangleF (0, 60 , Frame.Width, 60),
                            StringValue     = RuntimeSettings.DefaultHomePath,
                            Font            = NSFontManager.SharedFontManager.FontWithFamily (
                                "Lucida Grande", NSFontTraitMask.Unbold, 0, 9),
                            TextColor = NSColor.White
                        };
                    }

                    ChangeSQFolder = new NSButton () {
                        Frame = new RectangleF (49, 18, 137, 40),
                        Image = new NSImage(Path.Combine (NSBundle.MainBundle.ResourcePath, "Pixmaps", "Changeit220.png")),
                        Transparent = false,
                        Bordered = false,
                        Enabled  = true
                    };

                    ChangeSQFolder.Activated += delegate {
                        SQFolderText.StringValue = SparkleSetupController.ChangeSQFolder ();
                        Reset ();
                        ShowPage(AbstractApplicationController.PageType.ConfigureFolders, null);
                        ShowAll();
                    };

                    FinishButton = new NSButton () {
                        Frame = new RectangleF (264, 18, 137, 40),
                        Image = new NSImage(Path.Combine (NSBundle.MainBundle.ResourcePath, "Pixmaps", "Continue159.png")),
                        Transparent = false,
                        Bordered = false,
                        Enabled  = true
                    };
                    FinishButton.Activated += delegate {
                        DirectoryInfo dir = new DirectoryInfo(SQFolderText.StringValue);
                        bool proceed = true;
                        if(dir.Exists && (dir.GetDirectories().Length > 0 || dir.GetFiles().Length > 0) ){
                            if(!Program.Controller.Confirm("This folder is not empty, do you wanna proceed? QloudSync will merge all files with your account.") ){
                                proceed = false;
                            }
                        }
                        if(proceed){
                            SparkleSetupController.Finish (SQFolderText.StringValue, remoteFoldersCheckboxes);
                        } else {
                            Reset ();
                            ShowPage(AbstractApplicationController.PageType.ConfigureFolders, null);
                            ShowAll();
                        }

                    };


                    InitializeCheckboxesFolders ();
                    ContentView.AddSubview (SQFolderTextLabel);
                    ContentView.AddSubview (SQFolderText);
                    Buttons.Add (ChangeSQFolder);
                    Buttons.Add (FinishButton);
                    NSApplication.SharedApplication.RequestUserAttention (NSRequestUserAttentionType.CriticalRequest);
                } catch (Exception e) {
                    Logger.LogInfo ("ERROR", e);
                    Reset ();
                    ShowPage(AbstractApplicationController.PageType.Login, null);
                    ShowAll ();
                    NSTextField MessageLabel = new NSTextField () {
                        Alignment       = NSTextAlignment.Left,
                        BackgroundColor = NSColor.Clear,
                        Bordered        = false,
                        Editable        = false,
                        Frame           = new RectangleF (88, 55 , Frame.Width, 60),
                        StringValue     = "An unexpected error occurred, please try again later.\n",
                        Font            = NSFontManager.SharedFontManager.FontWithFamily (
                            "Lucida Grande", NSFontTraitMask.Bold, 0, 10),
                        TextColor = NSColor.Red
                    };
                    ContentView.AddSubview (MessageLabel);
                }
            }

            if (type == Controller.PageType.Finished) {
                currentWindowCloseApplication = false;
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
        }

        void InitializeCheckboxesFolders ()
        {
            remoteFoldersCheckboxes = new List<NSButton> ();
            List<RepositoryItem> remoteItems = new RemoteRepositoryController (null).RootFolders;
            List<RepositoryItem> localItems = new PhysicalRepositoryController (new LocalRepository(this.SQFolderText.StringValue, "", true, true)).RootFolders;

            foreach (RepositoryItem item in remoteItems) 
            {
                NSButton chk = new NSButton () {
                    Frame = new RectangleF (82,  Frame.Height - 100 - ((remoteFoldersCheckboxes.Count + 1) * 17), 300, 18),
                    Title = item.Key
                };
                chk.SetButtonType(NSButtonType.Switch);
                chk.State = NSCellStateValue.On;
                remoteFoldersCheckboxes.Add (chk);
            }

            foreach (RepositoryItem item in localItems) 
            {
                if (!remoteItems.Contains (item)) {
                    NSButton chk = new NSButton () {
                        Frame = new RectangleF (82, Frame.Height - 100 - ((remoteFoldersCheckboxes.Count + 1) * 17), 300, 18),
                        Title = item.Key
                    };
                    chk.SetButtonType (NSButtonType.Switch);
                    chk.State = NSCellStateValue.On;
                    remoteFoldersCheckboxes.Add (chk);
                }
            }

            foreach (NSButton chk in remoteFoldersCheckboxes) 
            {
                ContentView.AddSubview (chk);
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
