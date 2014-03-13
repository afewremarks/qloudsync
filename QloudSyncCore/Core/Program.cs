using System;
using System.Threading;
using System.IO;
using GreenQloud.Repository;
using System.Linq;
using QloudSyncCore;
using System.Diagnostics;
using System.Web.Util;
using System.Net;
using System.Net.Sockets;

namespace GreenQloud.Core {

    public class Program {

        public static ApplicationController Controller;
        public static ApplicationUI UI;
        #if !__MonoCS__
        [STAThread]
        #endif
        public static void Run (ApplicationController controller, ApplicationUI ui)
        {
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(GeneralUnhandledExceptionHandler);

            HttpEncoder.Current = HttpEncoder.Default;
            Controller = controller;
            UI = ui;
            
            if (PriorProcess() != null)
            {
                throw new AbortedOperationException("Another instance of the app is already running.");
            }

            Controller.Initialize ();
            UI.Run ();
                 
            #if !__MonoCS__
            // Suppress assertion messages in debug mode
            GC.Collect (GC.MaxGeneration, GCCollectionMode.Forced);
            GC.WaitForPendingFinalizers ();
            #endif
        }

        public static void GeneralUnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs args)
        {
            Exception e = (Exception)args.ExceptionObject;
            try
            {
                throw e;
            } 
            catch (WebException webx) 
            {
                if (webx.Status == WebExceptionStatus.NameResolutionFailure || webx.Status == WebExceptionStatus.Timeout || webx.Status == WebExceptionStatus.ConnectFailure) {
                    Logger.LogInfo ("ERROR LOST CONNECTION", webx);
                } else {
                    Logger.LogInfo ("ERROR UNEXPECTED ON SYNCHRONIZER", webx);
                    Logger.LogInfo ("ERROR UNEXPECTED ON SYNCHRONIZER", "Calling HandleError");
                    Program.Controller.HandleError ();
                }
            }
            catch (SocketException sock) 
            {
                Logger.LogInfo ("ERROR LOST CONNECTION", sock);
            }
            catch (AbortedOperationException aex)
            {
                Logger.LogInfo("INFO INIT", "Operation aborted. Sesnding a QloudSync Kill.");
                Logger.LogInfo("INFO ABORTED", aex); 
                Logger.LogInfo ("INFO ABORTED", "Calling Kill");
                PriorProcess().Kill();
            } catch (WarningException warningException){
                Program.Controller.Alert(warningException.Message);
            } catch (Exception ex) {
                Logger.LogInfo("ERROR UNEXPECTED", ex);
                try
                {
                    //new SendMail().SendBugMessage(ex.Message);
                } catch {
                
                }
                Logger.LogInfo ("ERROR UNEXPECTED", "An unexpected error occourred. Check the log file.");
                Logger.LogInfo ("ERROR UNEXPECTED", "Calling HandleError");
                Program.Controller.HandleError();
            }
        }

        public static Process PriorProcess()
            // Returns a System.Diagnostics.Process pointing to
            // a pre-existing process with the same name as the
            // current one, if any; or null if the current process
        // is unique.,
        {
            Process curr = Process.GetCurrentProcess();
            Process[] procs = Process.GetProcessesByName(curr.ProcessName);
            foreach (Process p in procs)
            {
                if ((p.Id != curr.Id) &&
                    (p.MainModule.FileName == curr.MainModule.FileName))
                    return p;
            }
            return null;
        }

    }
}
