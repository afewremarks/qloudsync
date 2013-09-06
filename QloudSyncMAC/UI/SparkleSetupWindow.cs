
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

using MonoMac.Foundation;
using MonoMac.AppKit;
using MonoMac.ObjCRuntime;
using MonoMac.WebKit;

namespace GreenQloud {

    public class SparkleSetupWindow : NSWindow {

        public List <NSButton> Buttons = new List <NSButton> ();
        public string Header;
        new public string Description;
        public string background_image_path;

        private NSImage background_image;
        private NSImageView background_image_view;
        private NSTextField header_text_field;
        private NSTextField description_text_field;


        public SparkleSetupWindow () : base ()
        {
            SetFrame (new RectangleF (0, 0, 450, 542), true);

            StyleMask   = NSWindowStyle.Titled | NSWindowStyle.Closable | NSWindowStyle.Miniaturizable;
            MaxSize     = new SizeF (450, 542);
            MinSize     = new SizeF (450, 542);
            HasShadow   = true;
            BackingType = NSBackingStore.Buffered;

            Center ();

            this.background_image_view = new NSImageView () {
                //Image = this.background_image,
                Frame = new RectangleF (0, 0, 450, 542)
            };

            this.header_text_field = new NSTextField () {
                Frame           = new RectangleF (190, Frame.Height - 100, Frame.Width, 48),
                BackgroundColor = NSColor.Clear,
                Bordered        = false,
                Editable        = false,
                Font            = NSFontManager.SharedFontManager.FontWithFamily (
                    "Lucida Grande", NSFontTraitMask.Bold, 0, 16)
            };
            
            this.description_text_field = new NSTextField () {
                Frame           = new RectangleF (190, Frame.Height - 210, 640 - 240, 105),
                BackgroundColor = NSColor.Clear,
                Bordered        = false,
                Editable        = false,
                Font            = NSFontManager.SharedFontManager.FontWithFamily (
                    "Lucida Grande", NSFontTraitMask.Condensed, 0, 13)
            };

            if (Program.UI != null)
                Program.UI.UpdateDockIconVisibility ();
        }

        
        public void Reset ()
        {
            ContentView.Subviews = new NSView [0];
            Buttons              = new List <NSButton> ();
            Header               = "";
            Description          = "";
            ContentView.AddSubview (this.background_image_view);
        }


        public void ShowAll ()
        {
            if (background_image_path != null) {
                this.background_image = new NSImage (background_image_path) {
                    Size = new SizeF (450, 542)
                };
                this.background_image_view.Image = this.background_image;
            }

            this.header_text_field.StringValue      = Header;
            this.description_text_field.StringValue = Description;
            
            ContentView.AddSubview (this.header_text_field);

            if (!string.IsNullOrEmpty (Description))
                ContentView.AddSubview (this.description_text_field);
            
            int i = 1;
            int x = 0;
            if (Buttons.Count > 0) {
                DefaultButtonCell = Buttons [0].Cell;
                
                foreach (NSButton button in Buttons) {
                    button.BezelStyle = NSBezelStyle.Rounded;
                    if (button.Frame.Height == 0) {
                        button.Frame = new RectangleF (Frame.Width - 15 - x - (105 * i), 12, 105, 32);

                        // Make the button a bit wider if the text is
                        // likely to be longer
                        if (button.Title.Contains (" ")) {
                            button.SizeToFit ();
                            button.Frame = new RectangleF (Frame.Width - 30 - 15 - (105 * (i - 1)) - button.Frame.Width,
                                                           12, button.Frame.Width + 30, 32);
                            x += 15;
                        }
                    }
                    button.Font = SparkleUI.Font;
                    ContentView.AddSubview (button);
                    i++;
                }
            }

            RecalculateKeyViewLoop ();
        }


        public override void OrderFrontRegardless ()
        {
            NSApplication.SharedApplication.AddWindowsItem (this, GlobalSettings.ApplicationName+" Setup", false);
            NSApplication.SharedApplication.ActivateIgnoringOtherApps (true);
            MakeKeyAndOrderFront (this);

            if (Program.UI != null)
                Program.UI.UpdateDockIconVisibility ();
            
            base.OrderFrontRegardless ();
        }


        public override void PerformClose (NSObject sender)
        {
            base.OrderOut (this);
            NSApplication.SharedApplication.RemoveWindowsItem (this);

            if (Program.UI != null)
                Program.UI.UpdateDockIconVisibility ();

            return;
        }


        public override bool AcceptsFirstResponder ()
        {
            return true;
        }
    }
}
