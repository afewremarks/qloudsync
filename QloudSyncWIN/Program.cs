using System;
using System.Threading;
using System.IO;
using GreenQloud.Repository;
using System.Linq;
using GreenQloud.UI;
using System.Windows.Forms;
using QloudSyncCore.Core.Util;
using System.Diagnostics;

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
                UI = UIManager.GetInstance();
                GreenQloud.Core.Program.Run(Controller, UI);
                Application.Run(UI);

            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
                Logger.LogInfo("Init", e);
                Exit();
            }

#if !__MonoCS__
            // Suppress assertion messages in debug mode
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
            GC.WaitForPendingFinalizers();
#endif
        }

        internal static void Exit()
        {
            Environment.Exit(-1);
        }
    }
}
