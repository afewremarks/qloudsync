using System;

using System.IO;
using System.Linq;
using System.Collections.Generic;
using GreenQloud.Synchrony;
using GreenQloud.Repository;
using GreenQloud.Util;
using System.Threading;
using GreenQloud.Model;
using GreenQloud.Repository.Local;
using GreenQloud.Persistence;
using GreenQloud.Persistence.SQLite;

namespace GreenQloud.Synchrony
{
    public abstract class AbstractLocalEventsSynchronizer : AbstractSynchronizer<AbstractLocalEventsSynchronizer>
    {
        bool creatingEvent;
        private SQLiteEventDAO eventDAO = new SQLiteEventDAO();
        private IRemoteRepositoryController remoteRepository = new RemoteRepositoryController ();
        private IPhysicalRepositoryController physicalLocalRepository = new StorageQloudPhysicalRepositoryController ();

        protected AbstractLocalEventsSynchronizer 
            () :
                base ()
        {
           
        }

        public void Create (Event e){       
            e.RepositoryType = RepositoryType.LOCAL;
            eventDAO.Create (e);
            creatingEvent = true;
        }

        public override void Run(){
            while (true){
                if (creatingEvent){
                    creatingEvent = false;
                    //if(SyncStatus == SyncStatus.IDLE){
                        //base.Synchronize ();
                    //}
                }
                Thread.Sleep (500);
            }
        }

        bool isCopy (RepositoryItem item)
        {
            if (!physicalLocalRepository.Exists (item))
                return false;

            List<RepositoryItem> copys = remoteRepository.GetCopys (item);

            if (copys.Count == 0)
                return false;

            foreach(RepositoryItem i in copys){
                if(!physicalLocalRepository.Exists (i))
                    return false;
            }

            return true;
        }

        bool isMove (RepositoryItem item)
        {
            if (!physicalLocalRepository.Exists (item))
                return false;

            List<RepositoryItem> copys = remoteRepository.GetCopys (item);

            if (copys.Count == 0)
                return false;

            foreach(RepositoryItem i in copys){
                if(!physicalLocalRepository.Exists (i))
                    return true;
            }

            return false;
        }

    }

}

