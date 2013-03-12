using System;
using MonoMac.AppKit;
using MonoMac.Foundation;

namespace GreenQloud
{
    public class HyperLink : NSTextField {
        
        private NSUrl url;
        
        
        public HyperLink (string text, string address) : base ()
        {
            this.url = new NSUrl (address);
            
            AllowsEditingTextAttributes = true;
            BackgroundColor = NSColor.White;
            Bordered        = false;
            DrawsBackground = false;
            Editable        = false;
            Selectable      = false;
            
            NSData name_data = NSData.FromString ("<a href='" + this.url +
                                                  "' style='font-size: 8pt; font-family: \"Lucida Grande\"; color: white'>" + text + "</a></font>");
            
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

