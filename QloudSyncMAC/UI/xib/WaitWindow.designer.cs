// WARNING
//
// This file has been generated automatically by Xamarin Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using MonoMac.Foundation;
using System.CodeDom.Compiler;

namespace QloudSync
{
	[Register ("WaitWindowController")]
	partial class WaitWindowController
	{
		[Outlet]
		MonoMac.AppKit.NSProgressIndicator indeterminateProgress { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (indeterminateProgress != null) {
				indeterminateProgress.Dispose ();
				indeterminateProgress = null;
			}
		}
	}

	[Register ("WaitWindow")]
	partial class WaitWindow
	{
		
		void ReleaseDesignerOutlets ()
		{
		}
	}
}
