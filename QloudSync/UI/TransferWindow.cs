
using System;
using System.Drawing;
using System.IO;
using System.Linq;

using MonoMac.AppKit;
using MonoMac.Foundation;
using MonoMac.ObjCRuntime;
using MonoMac.WebKit;
using System.Collections.Generic;

namespace GreenQloud {
    
    public class TransferWindow : NSWindow {

        private WebView credits_text_field;

        public event Action ShowWindowEvent = delegate { };
        public event Action HideWindowEvent = delegate { };
        public TransferWindow (IntPtr handle) : base (handle) { }
        
        public TransferWindow () : base ()
        {
            Program.Controller.ShowTransferWindowEvent += delegate {
                ShowWindowEvent ();
            };

            Program.Controller.RecentsTransfers.OnAdd += delegate {
                ListingTransfers ();
            };
            
            using (var a = new NSAutoreleasePool ())
            {
                SetFrame (new RectangleF (100, 50, 415, 800), true);
                Center ();
                
                Delegate    = new TransferDelegate ();
                StyleMask   = (NSWindowStyle.Closable | NSWindowStyle.Titled);
                Title       = "Recent Changes";
                MaxSize     = new SizeF (640, 281);
                MinSize     = new SizeF (640, 281);
                HasShadow   = true;
                BackingType = NSBackingStore.Buffered;
                BackgroundColor = NSColor.White;
                CreateTransfer ();
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
                        ListingTransfers();
                        OrderFrontRegardless ();
                    });
                }
            };
        }

        void ListingTransfers ()
        {
            
            string html = "<div style=\"width: 389px; font-family:'Lucida Grande';border-bottom:1px solid #005580\">";
            List<TransferResponse> listTransfers = Program.Controller.RecentsTransfers;

            int count = listTransfers.Count-1;
            for (int i=count; i==0; i--){
                TransferResponse tr = listTransfers [i];
                html += "<div style=\"height: 94px; width: 389px; float:left; border-bottom:1px solid #666\"> ";   
                html += "    <div style=\"height: 93px; width: 69px; float:left; display:block; margin-left:auto;margin-right:auto\">";
                html += string.Format("        <div style=\"vertical-align: central;margin-top:20px;\"><img src='{0}'/></div>", this.GetImageSyncToTransfer(tr));
                html += "        </div>    ";
                html += "        <div style=\"height: 90px; width: 315px; float:left\">        ";
                html += string.Format("       <div style=\"height: 19px; text-align: right; font-size:10pt; font-weight: bold\">{0}%</div>", tr.Percentage);
                html += string.Format("        <div  style=\"height: 45px\"><div style=\"height: 25px; font-size:11pt\">{0}</div><div style=\"font-size: 8pt\">{1}</div></div>", tr.StorageQloudObject.Name, tr.StorageQloudObject.FullLocalName);
                html += string.Format("                           <div  style=\"height: 30px; font-size: 10pt\">{0} - {1}</div>", tr.InitialTime.ToString("MM/dd/yyyy hh:mm:ss ff"), tr.EndTime.ToString("MM/dd/yyyy hh:mm:ss ff"));
                html += "</div>        ";
                html += "                   </div>";
            }
            
            html += "</div>";
            credits_text_field.MainFrame.LoadHtmlString(html, new NSUrl(""));
        }        
        
        private void CreateTransfer ()
        {
            using (var a = new NSAutoreleasePool ())
            {
                this.credits_text_field = new WebView () {
                    Frame           = new RectangleF (0, -23, 415, 800),
                    DrawsBackground = false,
                    Editable        = false
                };

                string html = "";

                foreach (TransferResponse tr in Program.Controller.RecentsTransfers)
                    html += string.Format("{0}<br>", tr.StorageQloudObject.Name);


                credits_text_field.MainFrame.LoadHtmlString(html, new NSUrl(""));
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

        string GetImageSyncToTransfer (TransferResponse tr)
        {
            if (tr.Type == TransferType.UPLOAD && tr.Status == TransferStatus.DONE)
                return Path.Combine (NSBundle.MainBundle.ResourcePath, "Pixmaps", "up_done.png");
            if (tr.Type == TransferType.UPLOAD && tr.Status == TransferStatus.PENDING)
                return Path.Combine (NSBundle.MainBundle.ResourcePath, "Pixmaps", "up_sync.png");
            if(tr.Type == TransferType.DOWNLOAD && tr.Status == TransferStatus.PENDING)
                return Path.Combine (NSBundle.MainBundle.ResourcePath, "Pixmaps", "down_sync.png");
            if(tr.Type == TransferType.DOWNLOAD && tr.Status == TransferStatus.DONE)
                return Path.Combine (NSBundle.MainBundle.ResourcePath, "Pixmaps", "down_done.png");
            if(tr.Type == TransferType.REMOVE && tr.Status == TransferStatus.DONE)
                return Path.Combine (NSBundle.MainBundle.ResourcePath, "Pixmaps", "remove.png");
            return "";
        }
    }
    
    
    public class TransferDelegate : NSWindowDelegate {
        
        public override bool WindowShouldClose (NSObject sender)
        {
            (sender as TransferWindow).WindowClosed ();
            return false;
        }
    }
}