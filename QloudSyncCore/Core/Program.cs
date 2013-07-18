using System;
using System.Threading;
using System.IO;
using GreenQloud.Repository;
using System.Linq;
using QloudSyncCore;

namespace GreenQloud.Core {

    public class Program {

        public static ApplicationController Controller;
        public static ApplicationUI UI;
        #if !__MonoCS__
        [STAThread]
        #endif
        public static void Run (ApplicationController controller, ApplicationUI ui)
        {
            Controller = controller;
            UI = ui;
            try {
                Controller.Initialize ();
                UI.Run (); 
            } catch (Exception e){
                Logger.LogInfo ("Init", e);
                Console.WriteLine (e.StackTrace);
                Environment.Exit (-1);
            }
            #if !__MonoCS__
            // Suppress assertion messages in debug mode
            GC.Collect (GC.MaxGeneration, GCCollectionMode.Forced);
            GC.WaitForPendingFinalizers ();
            #endif
        }
    }
}