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
		MonoMac.AppKit.NSView foldersView { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTextField itemsLabel { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTextField itemsProcessedLabel { get; set; }

		[Outlet]
		MonoMac.AppKit.NSButton moveSQFolderButton { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTextField pathLabel { get; set; }

		[Outlet]
		MonoMac.AppKit.NSProgressIndicator statusProgressIndicator { get; set; }

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
			if (changeFoldersButton != null) {
				changeFoldersButton.Dispose ();
				changeFoldersButton = null;
			}

			if (downloadBandwidthLabel != null) {
				downloadBandwidthLabel.Dispose ();
				downloadBandwidthLabel = null;
			}

			if (downloadLimiterMatrix != null) {
				downloadLimiterMatrix.Dispose ();
				downloadLimiterMatrix = null;
			}

			if (foldersView != null) {
				foldersView.Dispose ();
				foldersView = null;
			}

			if (itemsLabel != null) {
				itemsLabel.Dispose ();
				itemsLabel = null;
			}

			if (moveSQFolderButton != null) {
				moveSQFolderButton.Dispose ();
				moveSQFolderButton = null;
			}

			if (pathLabel != null) {
				pathLabel.Dispose ();
				pathLabel = null;
			}

			if (itemsProcessedLabel != null) {
				itemsProcessedLabel.Dispose ();
				itemsProcessedLabel = null;
			}

			if (statusProgressIndicator != null) {
				statusProgressIndicator.Dispose ();
				statusProgressIndicator = null;
			}

			if (totalBandwidthLabel != null) {
				totalBandwidthLabel.Dispose ();
				totalBandwidthLabel = null;
			}

			if (unlinkAccountButton != null) {
				unlinkAccountButton.Dispose ();
				unlinkAccountButton = null;
			}

			if (uploadBandwidthLabel != null) {
				uploadBandwidthLabel.Dispose ();
				uploadBandwidthLabel = null;
			}

			if (uploadLimiterMatrix != null) {
				uploadLimiterMatrix.Dispose ();
				uploadLimiterMatrix = null;
			}

			if (usernameLabel != null) {
				usernameLabel.Dispose ();
				usernameLabel = null;
			}

			if (versionLabel != null) {
				versionLabel.Dispose ();
				versionLabel = null;
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
