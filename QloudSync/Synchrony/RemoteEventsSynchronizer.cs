using System;
using GreenQloud.Model;
using GreenQloud.Repository.Local;
using GreenQloud.Repository.Remote;
using GreenQloud.Persistence;

namespace GreenQloud.Synchrony
{
    public class RemoteEventsSynchronizer : Synchronizer
    {

        public RemoteEventsSynchronizer  
            (LogicalRepositoryController logicalLocalRepository, PhysicalRepositoryController physicalLocalRepository, RemoteRepositoryController remoteRepository, TransferDAO transferDAO, EventDAO eventDAO) :
            base (logicalLocalRepository, physicalLocalRepository, remoteRepository, transferDAO, eventDAO)
        {
        }

        public new void Synchronize (){
            AddEvents ();
            base.Synchronize();
        }

        public void AddEvents ()
        {
            foreach (RepositoryItem localItem in physicalLocalRepository.Files) {
                if (remoteRepository.Exists(localItem)){
                    Event e = new Event ();
                    e.Item = localItem;
                    e.EventType = EventType.DELETE;
                    e.RepositoryType = RepositoryType.REMOTE;
                    e.Synchronized = false;
                    eventDAO.Create (e);
                }
            }     
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

