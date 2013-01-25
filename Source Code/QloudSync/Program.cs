using System;
using System.Threading;

namespace QloudSync {

    public class Program {

        public static Controller Controller;
        public static SparkleUI UI;

       // private static Mutex program_mutex = new Mutex (false, "QloudSync");

        #if !__MonoCS__
        [STAThread]
        #endif
        public static void Main (string [] args)
        {
            try {

              
               Controller = new Controller ();

                Controller.Initialize ();
               
                UI = new SparkleUI ();

                UI.Run ();
            } catch (Exception e){
                Logger.LogInfo ("Init", e);
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
