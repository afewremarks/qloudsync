using System;
using GreenQloud.Repository.Local;
using GreenQloud.Repository.Remote;
using GreenQloud.Persistence;

namespace GreenQloud.Test.SimpleSynchrony
{
    public class SimpleLocalEventsSynchronizer : GreenQloud.Synchrony.LocalEventsSynchronizer
    {
        public SimpleLocalEventsSynchronizer (LogicalRepositoryController logical, PhysicalRepositoryController physical, 
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

