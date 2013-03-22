using System;
using GreenQloud.Repository.Remote;
using GreenQloud.Repository;
using GreenQloud.Persistence;
using GreenQloud.Repository.Local;

namespace GreenQloud.Synchrony
{
    public class StorageQloudBacklogSynchronizer : BacklogSynchronizer
    {
        private static StorageQloudBacklogSynchronizer instance;

        private StorageQloudBacklogSynchronizer (LogicalRepositoryController logical, PhysicalRepositoryController physical, RemoteRepositoryController remote, TransferDAO transferDAO) : base (logical, physical, remote, transferDAO)
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
        
        public static StorageQloudBacklogSynchronizer GetInstance(){
            if (instance == null) {
                StorageQloudLogicalRepositoryController logicalController = new StorageQloudLogicalRepositoryController ();
                instance = new StorageQloudBacklogSynchronizer (
                    logicalController, 
                    new StorageQloudPhysicalRepositoryController (logicalController),
                    new StorageQloudRemoteRepository (logicalController),
                    new StorageQloudTransferDAO()
                    );
            }
            return instance;
        }
    }
}

