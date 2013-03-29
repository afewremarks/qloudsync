
using System;
using System.Drawing;
using System.IO;
using System.Linq;

using MonoMac.AppKit;
using MonoMac.Foundation;
using MonoMac.ObjCRuntime;
using MonoMac.WebKit;
using System.Collections.Generic;
using GreenQloud.Model;

namespace GreenQloud {
    
    public class TransferWindow : NSWindow {

        private WebView transfersView;

        public event Action ShowWindowEvent = delegate { };
        public event Action HideWindowEvent = delegate { };
        public TransferWindow (IntPtr handle) : base (handle) { }
        
        public TransferWindow () : base ()
        {
            Program.Controller.ShowTransferWindowEvent += delegate {
                ShowWindowEvent ();
            };

            using (var a = new NSAutoreleasePool ())
            {
                SetFrame (new RectangleF (100, 50, 500, 800), true);
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
                        //ListingTransfers();
                        OrderFrontRegardless ();
                    });
                }
            };
        }

//        void ListingTransfers ()
//        {
//            System.Text.StringBuilder html = new System.Text.StringBuilder ();
//            html.AppendLine ("<div style=\"width: 505px; font-family:'myriad-pro';border-bottom:1px solid #005580; background: -webkit-linear-gradient(top,#05c4ed 150px,#6ccddd 420px);\">");
//            html.AppendLine ("<div style=\"font-size: 22px; width:500px; border-bottom: 2px #28a8e2 solid; padding-left: 22px; color: #fff;  background-color: #10c4ea; padding-top: 10px; margin-bottom: 5 px; border-top-left-radius: 6px;\">Recent Changes</div>");
//            List<Transfer> listTransfers = Program.Controller.RecentsTransfers;
//
//            int count = listTransfers.Count-1;
//            for (int i=count; i>=0; i--){
//                Transfer tr = listTransfers [i];
//                html.AppendLine ("<div style=\"height: 60px; width: 500px; float:left; border-bottom:1px solid #18b4ee\"> ");   
//                html.AppendLine ("    <div style=\"height: 40px; width: 69px; float:left; display:block; margin-left:auto;margin-right:auto\">");
//                html.AppendLine (string.Format("        <div style=\"vertical-align: central;margin-top:13px; text-align: center;\"><img src='{0}'/></div>", this.GetImageSyncToTransfer(tr)));
//                html.AppendLine ("        </div>    ");
//                html.AppendLine ( "        <div style=\"height: 50px; width: 315px; float:left\">        ");
//                html.AppendLine ( string.Format("        <div  style=\"height: 22px; margin-top: 15px;\"><div style=\"height: 25px; font-size:14px\">{0}</div></div>", tr.Item.Name));
//                html.AppendLine ( string.Format("                           <div  style=\"height: 17px; font-size: 12px\">{0} - {1}</div>", tr.InitialTime.ToString("MM/dd/yyyy hh:mm:ss tt"), tr.EndTime.ToString("MM/dd/yyyy hh:mm:ss tt")));
//                html.AppendLine ( "</div>        ");
//                html.AppendLine ( "                   </div>");
//            }
//            
//            html.AppendLine ( "</div>");
//            transfersView.MainFrame.LoadHtmlString(html.ToString(), new NSUrl(""));
//        }        
        
        private void CreateTransfer ()
        {
            using (var a = new NSAutoreleasePool ())
            {
                this.transfersView = new WebView () {
                    Frame           = new RectangleF (-8, -14, 510, 800),
                    DrawsBackground = false,
                    Editable        = false
                };

                string html = "";

//                foreach (Transfer tr in Program.Controller.RecentsTransfers)
//                    html += string.Format("{0}<br>", tr.Item.Name);


                transfersView.MainFrame.LoadHtmlString(html, new NSUrl(""));
                ContentView.AddSubview (this.transfersView);
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

        string GetImageSyncToTransfer (Transfer tr)
        {
            string path;
            switch (tr.Type) {
                case TransferType.LOCAL_CREATE_FOLDER:
                    path = Path.Combine (NSBundle.MainBundle.ResourcePath, "Pixmaps","folder_down.png");
                    break;
                case TransferType.REMOTE_CREATE_FOLDER:
                    path = Path.Combine (NSBundle.MainBundle.ResourcePath, "Pixmaps","folder_up.png");
                break;
                case TransferType.DOWNLOAD:
                    path = Path.Combine (NSBundle.MainBundle.ResourcePath, "Pixmaps","file_down.png");
                break;
                case TransferType.UPLOAD:
                    path = Path.Combine (NSBundle.MainBundle.ResourcePath, "Pixmaps","file_up.png");
                    break;
                case TransferType.LOCAL_REMOVE:
                case TransferType.REMOTE_REMOVE:
                    path = Path.Combine (NSBundle.MainBundle.ResourcePath, "Pixmaps","trash.png");
                    break;
                default:
                    path = string.Empty;
                break;
            }
            return path;
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