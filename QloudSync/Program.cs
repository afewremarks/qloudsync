using System;
using System.Threading;
using System.IO;
using GreenQloud.Repository;
using System.Linq;

namespace GreenQloud {

    public class Program {

        public static Controller Controller;
        public static SparkleUI UI;

        private static Mutex program_mutex = new Mutex (false, "QloudSync");

        #if !__MonoCS__
        [STAThread]
        #endif
        public static void Main (string [] args)
        {
            if (!program_mutex.WaitOne (0, false)) {
                Console.WriteLine ("QloudSync is already running.");
                Environment.Exit (-1);
            }
            try {
                Controller = new Controller ();
                Controller.Initialize ();
               
                UI = new SparkleUI ();
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
