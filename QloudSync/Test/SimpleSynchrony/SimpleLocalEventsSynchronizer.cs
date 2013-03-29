using System;
using GreenQloud.Repository.Local;
using GreenQloud.Repository.Remote;
using GreenQloud.Persistence;

namespace GreenQloud.Test.SimpleSynchrony
{
    public class SimpleLocalEventsSynchronizer : GreenQloud.Synchrony.AbstractLocalEventsSynchronizer
    {
        public SimpleLocalEventsSynchronizer (LogicalRepositoryController logical, PhysicalRepositoryController physical, 
                                              RemoteRepositoryController remote, TransferDAO transfers, EventDAO eventDAO) : 
            base (logical, physical, remote, transfers, eventDAO)
        {
        }

    }
}

