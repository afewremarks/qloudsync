using System;
using System.Threading;
using System.IO;
using GreenQloud.Repository;
using System.Linq;
using GreenQloud.UI;
using System.Windows.Forms;

namespace GreenQloud
{

    public class Program
    {

        public static Controller Controller;
        public static UIManager UI;

        // private static Mutex program_mutex = new Mutex (false, "QloudSync");

#if !__MonoCS__
        [STAThread]
#endif
        public static void Main(string[] args)
        {
            try
            {
                Controller = new Controller();                
                UI = new UIManager();
                GreenQloud.Core.Program.Run(Controller, UI);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
                Logger.LogInfo("Init", e);
                Environment.Exit(-1);
            }

#if !__MonoCS__
            // Suppress assertion messages in debug mode
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
            GC.WaitForPendingFinalizers();
#endif
        }
    }
}
