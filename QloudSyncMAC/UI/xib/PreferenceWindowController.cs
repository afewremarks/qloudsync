using System;
using System.Collections.Generic;
using System.Linq;
using MonoMac.Foundation;
using MonoMac.AppKit;
using GreenQloud;

namespace QloudSync
{
    public partial class PreferenceWindowController : MonoMac.AppKit.NSWindowController
    {
        #region Constructors
        // Called when created from unmanaged code
        public PreferenceWindowController (IntPtr handle) : base (handle)
        {
            Initialize ();
        }
        // Called when created directly from a XIB file
        [Export ("initWithCoder:")]
        public PreferenceWindowController (NSCoder coder) : base (coder)
        {
            Initialize ();
        }
        // Call to load from the XIB/NIB file
        public PreferenceWindowController () : base ("PreferenceWindow")
        {
            Initialize ();

            Program.Controller.ShowEventPreferenceWindow += delegate {
                base.LoadWindow ();
                //will render for generic 
                base.ShowWindow (new NSObject ());
            };
        }
        // Shared initialization code
        void Initialize ()
        {
        }
        #endregion
        //strongly typed window accessor
        public new PreferenceWindow Window {
            get {
                return (PreferenceWindow)base.Window;
            }
        }

        public override void AwakeFromNib ()
        {
            base.AwakeFromNib ();
            pathLabel.StringValue = Environment.SystemDirectory;
            usernameLabel.StringValue = Environment.UserName;
            versionLabel.StringValue = Environment.Version.ToString();
        }


    }
}

