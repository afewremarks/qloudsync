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

namespace GreenQloud.Synchrony
{
    public abstract class AbstractLocalEventsSynchronizer : AbstractSynchronizer
    {
        Thread threadSync;
        bool creatingEvent;

        protected AbstractLocalEventsSynchronizer 
            (LogicalRepositoryController logicalLocalRepository, PhysicalRepositoryController physicalLocalRepository, RemoteRepositoryController remoteRepository, TransferDAO transferDAO, EventDAO eventDAO, RepositoryItemDAO repositoryItemDAO) :
                base (logicalLocalRepository, physicalLocalRepository, remoteRepository, transferDAO, eventDAO, repositoryItemDAO)
        {
            threadSync = new Thread(() =>{
                Synchronize ();
            });
           
        }

        public void Create (Event e){           
            eventDAO.Create (LoadEvent (e));
            creatingEvent = true;
        }

        public new void Synchronize(){
            while (Working){
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

        public Event LoadEvent (Event e)
        {
            e.RepositoryType = RepositoryType.LOCAL;
            e.User = Credential.Username;
            e.Application = GlobalSettings.FullApplicationName;
            e.ApplicationVersion = GlobalSettings.RunningVersion;
            e.DeviceId = GlobalSettings.MachineName;
            e.OS = GlobalSettings.OSVersion;
            e.Bucket = RuntimeSettings.DefaultBucketName;
            return e;
            
        }

        void CreateSubEvents (Event e)
        {
            foreach(RepositoryItem item in physicalLocalRepository.GetSubRepositoyItems(e.Item)){
                eventDAO.Create (new Event(){
                    Item = item,
                    EventType = e.EventType,
                    RepositoryType = e.RepositoryType
                });            }
        }

        #region implemented abstract members of Synchronizer
        public override void Start ()
        {
            Working = true;
            try{
                threadSync.Start();
            }catch{
                // do nothing
            }
        }
        public override void Pause ()
        {
            Working = false;
        }

        public override void Stop ()
        {
            Working = false;
            threadSync.Join();
        }
        #endregion
    }

}

