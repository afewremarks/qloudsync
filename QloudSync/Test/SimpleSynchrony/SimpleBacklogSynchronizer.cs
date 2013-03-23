using System;
using GreenQloud.Repository.Remote;
using GreenQloud.Repository;
using GreenQloud.Persistence;
using GreenQloud.Repository.Local;
using GreenQloud.Model;
using System.Collections.Generic;

namespace GreenQloud.Test.SimpleSynchrony
{
    public class SimpleBacklogSynchronizer : GreenQloud.Synchrony.BacklogSynchronizer
    {
        public List<RepositoryItem> list = new List<RepositoryItem>();

        public SimpleBacklogSynchronizer (LogicalRepositoryController logical, PhysicalRepositoryController physical, 
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

