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
	[Register ("PreferenceWindowController")]
	partial class PreferenceWindowController
	{
		[Outlet]
		MonoMac.AppKit.NSButton changeFoldersButton { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTextField downloadBandwidthLabel { get; set; }

		[Outlet]
		MonoMac.AppKit.NSMatrix downloadLimiterMatrix { get; set; }

		[Outlet]
		MonoMac.AppKit.NSScrollView foldersScrollView { get; set; }

		[Outlet]
		MonoMac.AppKit.NSButton moveSQFolderButton { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTextField pathLabel { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTextField totalBandwidthLabel { get; set; }

		[Outlet]
		MonoMac.AppKit.NSButton unlinkAccountButton { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTextField uploadBandwidthLabel { get; set; }

		[Outlet]
		MonoMac.AppKit.NSMatrix uploadLimiterMatrix { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTextField usernameLabel { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTextField versionLabel { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (foldersScrollView != null) {
				foldersScrollView.Dispose ();
				foldersScrollView = null;
			}

			if (changeFoldersButton != null) {
				changeFoldersButton.Dispose ();
				changeFoldersButton = null;
			}

			if (moveSQFolderButton != null) {
				moveSQFolderButton.Dispose ();
				moveSQFolderButton = null;
			}

			if (downloadLimiterMatrix != null) {
				downloadLimiterMatrix.Dispose ();
				downloadLimiterMatrix = null;
			}

			if (uploadLimiterMatrix != null) {
				uploadLimiterMatrix.Dispose ();
				uploadLimiterMatrix = null;
			}

			if (totalBandwidthLabel != null) {
				totalBandwidthLabel.Dispose ();
				totalBandwidthLabel = null;
			}

			if (downloadBandwidthLabel != null) {
				downloadBandwidthLabel.Dispose ();
				downloadBandwidthLabel = null;
			}

			if (uploadBandwidthLabel != null) {
				uploadBandwidthLabel.Dispose ();
				uploadBandwidthLabel = null;
			}

			if (usernameLabel != null) {
				usernameLabel.Dispose ();
				usernameLabel = null;
			}

			if (versionLabel != null) {
				versionLabel.Dispose ();
				versionLabel = null;
			}

			if (pathLabel != null) {
				pathLabel.Dispose ();
				pathLabel = null;
			}

			if (unlinkAccountButton != null) {
				unlinkAccountButton.Dispose ();
				unlinkAccountButton = null;
			}
		}
	}

	[Register ("PreferenceWindow")]
	partial class PreferenceWindow
	{
		
		void ReleaseDesignerOutlets ()
		{
		}
	}
}
