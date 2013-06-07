//   SparkleShare, a collaboration and sharing tool.
//   Copyright (C) 2010  Hylke Bons <hylkebons@gmail.com>
//
//   This program is free software: you can redistribute it and/or modify
//   it under the terms of the GNU General Public License as published by
//   the Free Software Foundation, either version 3 of the License, or
//   (at your option) any later version.
//
//   This program is distributed in the hope that it will be useful,
//   but WITHOUT ANY WARRANTY; without even the implied warranty of
//   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//   GNU General Public License for more details.
//
//   You should have received a copy of the GNU General Public License
//   along with this program.  If not, see <http://www.gnu.org/licenses/>.


using System;
using System.IO;
using GreenQloud.Model;

namespace GreenQloud {
    
    public static class Logger {

        private static Object debug_lock = new Object ();
        private static int log_size = 0;

        public static void LogInfo (string type, string message)
        {
            string timestamp = GlobalDateTime.Now.ToString ("HH:mm:ss");
            string line      = timestamp + " | " + type + " | " + message;

#if DEBUG
                Console.WriteLine (line);
#endif
            lock (debug_lock) {
                // Don't let the log get bigger than 1000 lines
                Directory.CreateDirectory(RuntimeSettings.ConfigPath);
                if (log_size >= 1000) {
                    //File.WriteAllText (RuntimeSettings.LogFilePath, line + Environment.NewLine);
                    log_size = 0;

                } else {
                    //File.AppendAllText (RuntimeSettings.LogFilePath, line + Environment.NewLine);
                    log_size++;
                }
            }
        }
        public static void LogEvent (string type, Event e ){
            string s = String.Format (" {0} {1} {2}",e.EventType, e.RepositoryType, e.Item.FullLocalName);
            if(e.Item.ResultObjectRelativePath != ""){
                s += String.Format ("  Result Object: {0} \n",e.Item.ResultObjectRelativePath);
            }
            Logger.LogInfo(type, s);
        }
        public static void LogInfo (string type, Exception e ){
            string message = string.Format("{0}\n{1}\n{2}\n{3}\n", e.GetType(), e.Message, e.StackTrace, e.GetBaseException());
            if(e.InnerException != null)
                message+= e.InnerException.Message;
            LogInfo(type, message);
        }
    }
}
