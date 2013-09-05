using System;
using System.Collections.Generic;
using System.Linq;
using MonoMac.Foundation;
using MonoMac.AppKit;

namespace QloudSync
{
    public partial class SparkleSetup2 : MonoMac.AppKit.NSWindow
    {
        #region Constructors
        // Called when created from unmanaged code
        public SparkleSetup2 (IntPtr handle) : base (handle)
        {
            Initialize ();
        }
        // Called when created directly from a XIB file
        [Export ("initWithCoder:")]
        public SparkleSetup2 (NSCoder coder) : base (coder)
        {
            Initialize ();
        }
        // Shared initialization code
        void Initialize ()
        {
        }
        #endregion
    }
}

