using System;
using System.Collections.Generic;
using GreenQloud.Repository;
using System.Linq;
using System.Xml;
using System.Threading;
using GreenQloud.Model;
using GreenQloud.Persistence.SQLite;

namespace GreenQloud.Repository.Local
{
    public class StorageQloudLogicalRepositoryController : LogicalRepositoryController
    {
                
        private SQLiteRepositoryItemDAO repositoryItemDAO = new SQLiteRepositoryItemDAO();
        private SQLiteRepositoryDAO repositoryDAO = new SQLiteRepositoryDAO();
        private StorageQloudPhysicalRepositoryController physicalController = new StorageQloudPhysicalRepositoryController();
        public StorageQloudLogicalRepositoryController (){
        }
        
        #region implemented abstract members of LogicalRepositoryController
        

        public override void Solve (RepositoryItem remoteObj)
        {
            if (!physicalController.Exists (remoteObj)){
                if (Exists(remoteObj))                
                    repositoryItemDAO.Remove (remoteObj);

            }
            else{
                if (!Exists(remoteObj)) 
                    repositoryItemDAO.Create (remoteObj);
            }
        }
        
        public override bool Exists (RepositoryItem item)
        {
            return repositoryItemDAO.Exists(item);
        }
        
        
        public override List<RepositoryItem> Items {
            get {
                throw new NotImplementedException ();
            }
        }
        
        public override List<LocalRepository> LocalRepositories {
            get{
                return repositoryDAO.All;
            }
            set{}
        }
        
        #endregion
        
       

    }

}


