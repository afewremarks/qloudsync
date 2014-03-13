using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

using MonoMac.Foundation;
using MonoMac.AppKit;
using MonoMac.ObjCRuntime;
using GreenQloud.Synchrony;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using GreenQloud.Persistence.SQLite;
using GreenQloud.Model;
using System.Collections;

 

namespace GreenQloud {
    public class Controller : AbstractApplicationController {

        public override void Initialize ()
        {

        }
        public override void CreateMenuItem ()
        {

        }

        public override void SetIcon(string folderPath){
            NSRunLoop.Main.BeginInvokeOnMainThread (() => {
                NSImage folder_icon = NSImage.ImageNamed ("qloudsync-folder.icns");
                NSWorkspace.SharedWorkspace.SetIconforFile (folder_icon, folderPath, 0);
            });
        }

        public override void CheckForUpdates()
        {

        }
        public override void Alert(string message)
        {
            NSAlert alert = new NSAlert(); 
            alert.MessageText = message; 
            alert.AddButton("Ok"); 
            alert.RunModal ();
        }

        public override bool Confirm(string message)
        {  
            NSAlert alert = new NSAlert(); 
            alert.MessageText = message; 
            alert.AddButton("No");
            alert.AddButton("Yes");
            int ret = alert.RunModal ();
            if (ret == 1001) {
                return true;
            }

            return false;
        }

        public override void FirstRunAction ()
        {
            CreateStartupItem ();
        }

        public void CreateStartupItem ()
        {
            // There aren't any bindings in MonoMac to support this yet, so
            // we call out to an applescript to do the job
            System.Diagnostics.Process process = new System.Diagnostics.Process ();
            process.StartInfo.FileName               = "defaults";
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute        = false;

            process.StartInfo.Arguments = "write loginwindow AutoLaunchedApplicationDictionary -array-add '{Path=\""+ MonoMac.Foundation.NSBundle.MainBundle.BundlePath + "\";}'";


            process.Start ();
            process.WaitForExit ();

            Logger.LogInfo ("INFO STARTUP ITEM", "Added " + MonoMac.Foundation.NSBundle.MainBundle.BundlePath + " to startup items");
        }

        public override void Quit ()
        {
            Process.GetProcessesByName("QloudSync")[0].Kill();
            Environment.Exit (0);
        }
        public override void OpenFolder (string path)
        {
            NSWorkspace.SharedWorkspace.OpenFile (path);
        }
        public override void OpenWebsite (string url)
        {
            NSWorkspace.SharedWorkspace.OpenUrl (new NSUrl (url));
        }
   	}
}
