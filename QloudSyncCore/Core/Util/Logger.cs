
using System;
using System.IO;
using GreenQloud.Model;

namespace GreenQloud {
    
    public static class Logger {

        private static Object debug_lock = new Object ();
        private static int log_size = 0;

        public static void LogInfo (string type, string message, bool useData = true)
        {
            string timestamp = "";
            string line = "";
            if (useData) {
                try {
                    timestamp = GlobalDateTime.Now.ToString ("HH:mm:ss");
                } catch (Exception e) {
                    timestamp = "[CANNOT GET DATE FROM SERVER!]";
                    line += timestamp + " | " + "ERROR" + " | " + e.Message + Environment.NewLine;
                }
            }
            line += timestamp + " | " + type + " | " + message;

            #if DEBUG
                Console.WriteLine(line);
            #endif

            lock (debug_lock) {
                Directory.CreateDirectory(RuntimeSettings.ConfigPath);
                if (log_size >= 50000) {
                    File.WriteAllText (RuntimeSettings.LogFilePath, line + Environment.NewLine);
                    log_size = 0;

                } else {
                    File.AppendAllText (RuntimeSettings.LogFilePath, line + Environment.NewLine);
                    log_size++;
                }
            }
        }
        public static void LogEvent (string type, Event e ){
            Logger.LogInfo(type, e.ToString());
        }
        public static void LogInfo (string type, Exception e ){
            string message = string.Format("{0}\n{1}\n{2}\n{3}\n", e.GetType(), e.Message, e.StackTrace, e.GetBaseException());
            if(e.InnerException != null)
                message+= e.InnerException.Message;
            LogInfo(type, message);
        }
    }
}
