using System;
using GreenQloud.Synchrony;
using GreenQloud.Repository.Local;
using GreenQloud.Repository.Remote;
using GreenQloud.Persistence;
using GreenQloud.Model;
using System.Collections.Generic;

namespace GreenQloud.Test.SimpleSynchrony
{
    public class SimpleSynchronizer : AbstractSynchronizer
    {
        public List<RepositoryItem> list = new List<RepositoryItem>();
        public SimpleSynchronizer (LogicalRepositoryController logical, PhysicalRepositoryController physical, 
                                   RemoteRepositoryController remote, TransferDAO transfers, EventDAO eventDAO) : 
            base (logical, physical, remote, transfers, eventDAO)
        {
        }

        #region implemented abstract members of Synchronizer

        public override void Start ()
        {
            throw new NotImplementedException ();
        }

        public override void Pause ()
        {
            throw new NotImplementedException ();
        }

        public override void Stop ()
        {
            throw new NotImplementedException ();
        }

        #endregion
    }
}

