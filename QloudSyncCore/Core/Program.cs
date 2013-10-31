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
                    Logger.LogInfo ("LOST CONNECTION", webx);
                    Program.Controller.HandleDisconnection ();
                } else {
                    Logger.LogInfo ("SYNCHRONIZER ERROR", webx);
                    Program.Controller.HandleError ();
                }
            }
            catch (SocketException sock) 
            {
                Logger.LogInfo ("LOST CONNECTION", sock);
                Program.Controller.HandleDisconnection ();
            }
            catch (AbortedOperationException aex)
            {
                Logger.LogInfo("Init", "Operation aborted. Sending a QloudSync Kill.");
                Logger.LogInfo("ABORTED", aex); 
                PriorProcess().Kill();
            } catch (WarningException warningException){
                Program.Controller.Alert(warningException.Message);
            } catch (Exception ex) {
                Logger.LogInfo("Unexpected Exception", ex);
                try
                {
                    new SendMail().SendBugMessage(ex.Message);
                } catch {
                
                }
                Program.Controller.HandleError();
            }
        }

        public static Process PriorProcess()
            // Returns a System.Diagnostics.Process pointing to
            // a pre-existing process with the same name as the
            // current one, if any; or null if the current process
            // is unique.
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
