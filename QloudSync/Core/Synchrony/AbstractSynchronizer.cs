using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.Threading;

using GreenQloud.Model;
using GreenQloud.Repository;
using GreenQloud.Util;
using GreenQloud.Persistence;
using GreenQloud.Repository.Local;
using System.Net.Sockets;

namespace GreenQloud.Synchrony
{
    
    public abstract class AbstractSynchronizer<T>
    {
        private Thread _thread;
        protected bool _stoped;
        static T instance;

        public AbstractSynchronizer() 
        {
            _stoped = true;
            _thread = new Thread(new ThreadStart(this.GenericRun)); 
        }

        public static T GetInstance(){
            if (instance == null)
                instance = Activator.CreateInstance<T> ();
            return instance;
        }

        public void Start() {
            if(_thread == null)
                _thread = new Thread(new ThreadStart(this.GenericRun));

            _stoped = false;
            _thread.IsBackground = true;
            if(!_thread.IsAlive)
                _thread.Start();
        }
        public void Join() { _thread.Join(); }
        public bool IsAlive { get { return _thread != null && _thread.IsAlive; } }
        public void Kill () { 
            _stoped = true;
            _thread = null;
        }

        void GenericRun ()
        {
            try {
                Run();
            } catch (WebException webx) {
                if (webx.Status == WebExceptionStatus.NameResolutionFailure || webx.Status == WebExceptionStatus.Timeout || webx.Status == WebExceptionStatus.ConnectFailure) {
                    Logger.LogInfo ("LOST CONNECTION", webx);
                    Program.Controller.HandleDisconnection ();
                } else {
                    Logger.LogInfo ("SYNCHRONIZER ERROR", webx);
                    Logger.LogInfo ("INFO", "Preparing to run rescue mode...");
                    Program.Controller.HandleError ();
                }
            } catch (SocketException sock) {
                Logger.LogInfo ("LOST CONNECTION", sock);
                Program.Controller.HandleDisconnection ();
            } catch (Exception e) {
                Logger.LogInfo ("SYNCHRONIZER ERROR", e);
                Logger.LogInfo ("INFO", "Preparing to run rescue mode...");
                Program.Controller.HandleError ();
            }
        }

        #region Abstract Methods
        public abstract void Run();
        #endregion
    }
}