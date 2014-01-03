using System;
using System.Collections.Generic;
using System.Linq;
using MonoMac.Foundation;
using MonoMac.AppKit;
using GreenQloud;

namespace QloudSync
{
    public partial class PreferenceWindow : MonoMac.AppKit.NSWindow
    {
        public event Action ShowWindowEvent = delegate { };
        public event Action HideWindowEvent = delegate { };
        #region Constructors
        // Called when created from unmanaged code
        public PreferenceWindow (IntPtr handle) : base (handle)
        {
            Initialize ();
        }
        // Called when created directly from a XIB file
        [Export ("initWithCoder:")]
        public PreferenceWindow (NSCoder coder) : base (coder)
        {
            Initialize ();
        }
        // Shared initialization code
        void Initialize ()
        {
            HideWindowEvent += delegate {
                using (var a = new NSAutoreleasePool ())
                {
                    NSRunLoop.Main.BeginInvokeOnMainThread (delegate {
                        PerformClose (this);
                    });
                }
            };

            ShowWindowEvent += delegate {
                using (var a = new NSAutoreleasePool ())
                {
                    NSRunLoop.Main.BeginInvokeOnMainThread (delegate {
                        OrderFrontRegardless ();
                    });
                }
            };
        }
        #endregion

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

        public void WindowClosed ()
        {
            HideWindowEvent ();
        }

        public void OpenWindow()
        {
            ShowWindowEvent ();
        }

       

    }
   
}

