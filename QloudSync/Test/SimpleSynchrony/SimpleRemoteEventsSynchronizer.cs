using System;
using GreenQloud.Repository.Local;
using GreenQloud.Repository.Remote;
using GreenQloud.Persistence;
using GreenQloud.Synchrony;

namespace GreenQloud.Test.SimpleSynchrony
{
    public class SimpleRemoteEventsSynchronizer : RemoteEventsSynchronizer
    {
        public SimpleRemoteEventsSynchronizer (LogicalRepositoryController logical, PhysicalRepositoryController physical, 
                                               RemoteRepositoryController remote, TransferDAO transfers, EventDAO eventDAO) : 
            base (logical, physical, remote, transfers, eventDAO)
        {
        }



    }
}

