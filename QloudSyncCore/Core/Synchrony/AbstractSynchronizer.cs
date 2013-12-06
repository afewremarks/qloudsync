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
        protected volatile bool _killed, _wasKilled;
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
                if (!_killed)
                {
                    _stoped = false;
                    _killed = false;
                    _wasKilled = false;
                    if (_thread == null)
                    {
                        _thread = new Thread(new ThreadStart(this.GenericRun));
                    }
                    if (!_thread.IsAlive)
                    {
                        if (_thread.ThreadState == ThreadState.Stopped)
                        {
                            _thread = new Thread(new ThreadStart(this.GenericRun));
                        }
                        _thread.Start();
                    }
                }
            }
        }
        public void Join() { _thread.Join(); }
        public bool IsAlive { get { return _thread != null && _thread.IsAlive; } }
        public bool Killed { get { return _wasKilled; } }
        public void Stop () { 
            lock (lockk) {
                if (!_stoped) {
                    _stoped = true;
                }
            }
        }

        public void Kill()
        {
            Stop();
            lock (lockk)
            {
                if (!_killed)
                {
                    _killed = true;
                }
            }
        }

        public bool IsStoped () {
            return _stoped;
        }
        void GenericRun ()
        {
            try
            {
                while (!_killed)
                {
                    while (_stoped)
                    {
                        Wait(1000);
                        if (_killed)
                        {
                            _wasKilled = true;
                            return;
                        }
                    }
                    Run();
                }
                _wasKilled = true;
            }
            catch (Exception e)
            {
                _stoped = true;
                _killed = true;
                _wasKilled = true;
                Program.GeneralUnhandledExceptionHandler(this, new UnhandledExceptionEventArgs(e, false));
            }
        }

        public void Skip()
        {
            _stoped = true;
            _killed = true;
            _wasKilled = true;
            canChange = true;
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
                    if (_killed)
                        canChange = true;
                    Wait(1000);
                    elapsed += 1000;
                }
                canChange = false;
            }
        }

        internal void Wait(int timeout)
        {
            int elapsed = 0;
            bool leave = false;
            while (!leave && (timeout == 0 || elapsed < timeout))
            {
                if (_killed)
                    leave = true;
                Thread.Sleep(1000);
                elapsed += 1000;
            }
        }
    }
}