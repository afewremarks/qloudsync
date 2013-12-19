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


    }
}

