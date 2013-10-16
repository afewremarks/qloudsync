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
using GreenQloud.Repository;
using System.Net.Sockets;
using GreenQloud.Core;

namespace GreenQloud.Synchrony
{
    
    public abstract class AbstractSynchronizer<T>
    {
        protected LocalRepository repo;
        protected SynchronizerUnit unit;
        private Thread _thread;
        protected volatile bool _stoped;
        protected Object lockk = new object();

        public AbstractSynchronizer(LocalRepository repo, SynchronizerUnit unit) 
        {
            this.repo = repo;
            this.unit = unit;
            _stoped = true;
            _thread = new Thread(new ThreadStart(this.GenericRun)); 
        }

        public static T NewInstance(LocalRepository repo, SynchronizerUnit unit){
            T instance;
            instance = (T)Activator.CreateInstance(typeof(T), repo, unit);
            return instance;
        }

        public void Start() {
            lock (lockk) {
                _stoped = false;
                if (_thread == null) {
                    _thread = new Thread(new ThreadStart(this.GenericRun));
                }
                if (!_thread.IsAlive) {
                    if (_thread.ThreadState == ThreadState.Stopped) {
                        _thread = new Thread(new ThreadStart(this.GenericRun));
                    }
                    _thread.Start ();
                }
                    
            }
        }
        public void Join() { _thread.Join(); }
        public bool IsAlive { get { return _thread != null && _thread.IsAlive; } }
        public void Stop () { 
            lock (lockk) {
                if (!_stoped) {
                    _stoped = true;
                    //_thread = null;
                }
            }
        }

        public bool IsStoped () {
            return _stoped;
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
                    Program.Controller.HandleError (repo);
                }
            } catch (SocketException sock) {
                Logger.LogInfo ("LOST CONNECTION", sock);
                Program.Controller.HandleDisconnection ();
            } catch (Exception e) {
                Logger.LogInfo ("ERROR", e);
                Logger.LogInfo ("INFO", "Preparing to run rescue mode...");
                Program.Controller.HandleError (repo);
            }
        }

        #region Abstract Methods
        public abstract void Run();
        #endregion

        protected bool canChange = false;
        protected object lockChange = new object();
        internal void WaitForChanges(int timeout)
        {
            lock (lockChange)
            {
                int elapsed = 0;
                while (!canChange && (timeout == 0 || elapsed < timeout))
                {
                    Thread.Sleep(1000);
                    elapsed += 1000;
                }
                canChange = false;
            }
        }
    }
}