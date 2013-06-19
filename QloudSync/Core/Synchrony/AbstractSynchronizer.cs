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

namespace GreenQloud.Synchrony
{
    
    public abstract class AbstractSynchronizer<T>
    {
        private Thread _thread;
        static T instance;

        public AbstractSynchronizer() { _thread = new Thread(new ThreadStart(this.GenericRun)); }

        public static T GetInstance(){
            if (instance == null)
                instance = Activator.CreateInstance<T> ();
            return instance;
        }

        public void Start() { _thread.Start(); }
        public void Join() { _thread.Join(); }
        public bool IsAlive { get { return _thread.IsAlive; } }
        public void Abort () { _thread.Abort (); }

        void GenericRun ()
        {
            Run();
        }

        #region Abstract Methods
        public abstract void Run();
        #endregion

    }
}