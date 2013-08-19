using System;
using MonoMac.AppKit;
using MonoMac.Foundation;

namespace GreenQloud
{
    public class HyperLink : NSTextField {
        
        private NSUrl url;
     

        public HyperLink (string text, string address) : this("", text, "</font>", address, "style='font-size: 8pt; font-family: \"Lucida Grande\"; color: white'"){
            this.BackgroundColor = NSColor.White;
        }


        public HyperLink (string pretext, string text, string postext, string address, string style) : base ()
        {
            this.url = new NSUrl (address);
            
            AllowsEditingTextAttributes = true;
            Bordered        = false;
            DrawsBackground = false;
            Editable        = false;
            Selectable      = false;
            base.Font = NSFontManager.SharedFontManager.FontWithFamily (
                "Lucida Grande", NSFontTraitMask.Condensed, 0, 13);
            
            NSData name_data = NSData.FromString (pretext+"<a href='" + this.url +
                                                  "' "+style+" >" + text + "</a>"+postext);

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

