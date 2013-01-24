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
using System.Drawing;
using System.IO;

using MonoMac.AppKit;
using MonoMac.Foundation;
using MonoMac.ObjCRuntime;
using MonoMac.WebKit;

namespace QloudSync {

    public class AboutWindow : NSWindow {

        private NSImage about_image;
        private NSImageView about_image_view;
        private NSTextField updates_text_field;
        private NSTextField credits_text_field;


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

                Delegate    = new SparkleAboutDelegate ();
                StyleMask   = (NSWindowStyle.Closable | NSWindowStyle.Titled);
                Title       = "About QloudSync";
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


               
                this.updates_text_field = new NSTextField () {
                    StringValue     = "Checking for updates...",
                    Frame           = new RectangleF (295, Frame.Height - 232, 318, 98),
                    Bordered        = false,
                    Editable        = false,
                    DrawsBackground = false,
                    Font            = NSFontManager.SharedFontManager.FontWithFamily
                        ("Lucida Grande", NSFontTraitMask.Unbold, 0, 11),
                    TextColor       = NSColor.FromCalibratedRgba (0.45f, 0.62f, 0.81f, 1.0f) // Tango Sky Blue #1
                };

                this.credits_text_field = new NSTextField () {
                    StringValue     = @"Copyright © 2010–" + DateTime.Now.Year + " Hylke Bons and others." +
                    "\n" +
                    "\n" +
                    "SparkleShare is Open Source software. You are free to use, modify, and redistribute it " +
                    "under the GNU General Public License version 3 or later.",
                    Frame           = new RectangleF (295, Frame.Height - 260, 318, 98),
                    TextColor       = NSColor.White,
                    DrawsBackground = false,
                    Bordered        = false,
                    Editable        = false,
                    Font            = NSFontManager.SharedFontManager.FontWithFamily (
                        "Lucida Grande", NSFontTraitMask.Unbold, 0, 11),
                };

                ContentView.AddSubview (this.about_image_view);
                ContentView.AddSubview (this.updates_text_field);
                ContentView.AddSubview (this.credits_text_field);
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


    public class SparkleAboutDelegate : NSWindowDelegate {
        
        public override bool WindowShouldClose (NSObject sender)
        {
            (sender as AboutWindow).WindowClosed ();
            return false;
        }


    }


    public class SparkleLink : NSTextField {

        private NSUrl url;


        public SparkleLink (string text, string address) : base ()
        {
            this.url = new NSUrl (address);

            AllowsEditingTextAttributes = true;
            BackgroundColor = NSColor.White;
            Bordered        = false;
            DrawsBackground = false;
            Editable        = false;
            Selectable      = false;

            NSData name_data = NSData.FromString ("<a href='" + this.url +
                "' style='font-size: 8pt; font-family: \"Lucida Grande\"; color: #739ECF'>" + text + "</a></font>");

            NSDictionary name_dictionary       = new NSDictionary();
            NSAttributedString name_attributes = new NSAttributedString (name_data, new NSUrl ("file://"), out name_dictionary);

            NSMutableAttributedString s = new NSMutableAttributedString ();
            s.Append (name_attributes);

            Cell.AttributedStringValue = s;

            SizeToFit ();
        }


        public override void MouseUp (NSEvent e)
        {
            Program.Controller.OpenWebsite (this.url.ToString ());
        }


        public override void ResetCursorRects ()
        {
            AddCursorRect (Bounds, NSCursor.PointingHandCursor);
        }
    }
}
