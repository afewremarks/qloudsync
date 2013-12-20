using System;
using System.Collections.Generic;
using System.Linq;
using MonoMac.Foundation;
using MonoMac.AppKit;

namespace QloudSync
{
    public partial class WaitWindowController : MonoMac.AppKit.NSWindowController
    {
        #region Constructors
        // Called when created from unmanaged code
        public WaitWindowController (IntPtr handle) : base (handle)
        {
            Initialize ();
        }
        // Called when created directly from a XIB file
        [Export ("initWithCoder:")]
        public WaitWindowController (NSCoder coder) : base (coder)
        {
            Initialize ();
        }
        // Call to load from the XIB/NIB file
        public WaitWindowController () : base ("WaitWindow")
        {
            Initialize ();
        }
        // Shared initialization code
        void Initialize ()
        {
        }
        #endregion
        //strongly typed window accessor
        public new WaitWindow Window {
            get {
                return (WaitWindow)base.Window;
            }
        }

        public override void AwakeFromNib ()
        {
            base.AwakeFromNib ();
            indeterminateProgress.StartAnimation (null);
        }
    }
}

