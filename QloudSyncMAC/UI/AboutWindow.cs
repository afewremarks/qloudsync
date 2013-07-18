
using System;
using System.Drawing;
using System.IO;

using MonoMac.AppKit;
using MonoMac.Foundation;
using MonoMac.ObjCRuntime;
using MonoMac.WebKit;

namespace GreenQloud {

    public class AboutWindow : NSWindow {

        private NSImage about_image;
        private NSImageView about_image_view;
        private NSTextField updates_text_field, version_text_field;
        private NSTextField credits_text_field;
       
        private HyperLink website_link, credits_link, report_problem_link, debug_log_link;


        public event Action ShowWindowEvent = delegate { };
        public event Action HideWindowEvent = delegate { };
        public AboutWindow (IntPtr handle) : base (handle) { }

        public AboutWindow () : base ()
        {
            Program.Controller.ShowAboutWindowEvent += delegate {
                ShowWindowEvent ();
            };

            using (var a = new NSAutoreleasePool ())
            {
                SetFrame (new RectangleF (0, 0, 640, 281), true);
                Center ();

                Delegate    = new AboutDelegate ();
                StyleMask   = (NSWindowStyle.Closable | NSWindowStyle.Titled);
                Title       = "About "+GlobalSettings.ApplicationName;
                MaxSize     = new SizeF (640, 281);
                MinSize     = new SizeF (640, 281);
                HasShadow   = true;
                BackingType = NSBackingStore.Buffered;

                CreateAbout ();
            }

            HideWindowEvent += delegate {
                using (var a = new NSAutoreleasePool ())
                {
                    InvokeOnMainThread (delegate {
                        PerformClose (this);
                    });
                }
            };

            ShowWindowEvent += delegate {
                using (var a = new NSAutoreleasePool ())
                {
                    InvokeOnMainThread (delegate {
                        OrderFrontRegardless ();
                    });
                }
            };
        }


        private void CreateAbout ()
        {
            using (var a = new NSAutoreleasePool ())
            {
                string about_image_path = Path.Combine (NSBundle.MainBundle.ResourcePath,
                    "Pixmaps", "about.png");

                this.about_image = new NSImage (about_image_path) {
                    Size = new SizeF (640, 260)
                };

                this.about_image_view = new NSImageView () {
                    Image = this.about_image,
                    Frame = new RectangleF (0, 0, 640, 260)
                };

                this.version_text_field = new NSTextField () {
                    StringValue     = "Version " + GlobalSettings.RunningVersion,
                    DrawsBackground = false,
                    Bordered        = false,
                    Editable        = false,
                    Frame           = new RectangleF (295, 140, 318, 22),
                    TextColor       = NSColor.White,
                    Font            = NSFontManager.SharedFontManager.FontWithFamily (
                        "Lucida Grande", NSFontTraitMask.Unbold, 0, 11)
                };
               
                this.updates_text_field = new NSTextField () {                   
                    Frame           = new RectangleF (295, Frame.Height - 232, 318, 98),
                    Bordered        = false,
                    Editable        = false,
                    DrawsBackground = false,
                    Font            = NSFontManager.SharedFontManager.FontWithFamily
                        ("Lucida Grande", NSFontTraitMask.Unbold, 0, 11),
                    TextColor       = NSColor.FromCalibratedRgba (0.45f, 1.62f, 1.81f, 1.0f) // Tango Sky Blue #1
                };

                if(GlobalSettings.RunningVersion == Statistics.VersionAvailable)
                    updates_text_field.StringValue = "This is lastest version available..";
                else
                    updates_text_field.StringValue = "Lastest version available: "+Statistics.VersionAvailable;

                NSColor color = NSColor.White;
                this.credits_text_field = new NSTextField () {
                    StringValue     = @"Copyright © 2013 GreenQloud" +
                    "\n" +
                    "\n" +
                    "QloudSync is Open Source software. You are free to use, modify, and redistribute it under GNU General Public License version 3 or later.",
                    Frame           = new RectangleF (295, Frame.Height - 260, 318, 98),
                    TextColor       = NSColor.White,
                    DrawsBackground = false,
                    Bordered        = false,
                    Editable        = false,
                    Font            = NSFontManager.SharedFontManager.FontWithFamily (
                        "Lucida Grande", NSFontTraitMask.Unbold, 0, 11),
                };

                this.website_link       = new HyperLink ("Website", "http://qloudsync.com");
                this.website_link.Frame = new RectangleF (new PointF (295, 25), this.website_link.Frame.Size);
                this.website_link.TextColor =  color;
                
                this.credits_link       = new HyperLink ("Credits", "https://github.com/greenqloud/qloudsync/tree/master/legal/Authors.txt");
                this.credits_link.Frame = new RectangleF (
                    new PointF (this.website_link.Frame.X + this.website_link.Frame.Width + 10, 25),
                    this.credits_link.Frame.Size);
                
                this.report_problem_link       = new HyperLink ("Report a problem", "https://github.com/greenqloud/qloudsync/issues");
                this.report_problem_link.Frame = new RectangleF (
                    new PointF (this.credits_link.Frame.X + this.credits_link.Frame.Width + 10, 25),
                    this.report_problem_link.Frame.Size);
                
                this.debug_log_link       = new HyperLink ("Debug log", "file://"+RuntimeSettings.LogFilePath);
                this.debug_log_link.Frame = new RectangleF (
                    new PointF (this.report_problem_link.Frame.X + this.report_problem_link.Frame.Width + 10, 25),
                    this.debug_log_link.Frame.Size);

                ContentView.AddSubview (this.about_image_view);
                ContentView.AddSubview (this.version_text_field);
                ContentView.AddSubview (this.updates_text_field);
                ContentView.AddSubview (this.credits_text_field);
                ContentView.AddSubview (this.website_link);
                ContentView.AddSubview (this.credits_link);
                ContentView.AddSubview (this.report_problem_link);
                ContentView.AddSubview (this.debug_log_link);
            }
        }

        public void WindowClosed ()
        {
            HideWindowEvent ();
        }

        public override void OrderFrontRegardless ()
        {
            NSApplication.SharedApplication.ActivateIgnoringOtherApps (true);
            MakeKeyAndOrderFront (this);

            if (Program.UI != null)
                Program.UI.UpdateDockIconVisibility ();

            base.OrderFrontRegardless ();
        }


        public override void PerformClose (NSObject sender)
        {
            base.OrderOut (this);

            if (Program.UI != null)
                Program.UI.UpdateDockIconVisibility ();

            return;
        }
    }


    public class AboutDelegate : NSWindowDelegate {
        
        public override bool WindowShouldClose (NSObject sender)
        {
            (sender as AboutWindow).WindowClosed ();
            return false;
        }
    }
}
