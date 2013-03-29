using System;
using GreenQloud.Repository;
using GreenQloud.Model;
using System.Collections.Generic;

namespace GreenQloud.Repository.Local
{
    public abstract class PhysicalRepositoryController : IRepositoryController
    {

        public PhysicalRepositoryController (){
        }

        #region RepositoryController implementation

        public abstract bool Exists (RepositoryItem repoObject);


        public abstract List<RepositoryItem> Items {
            get ;
        }

        #endregion

        public abstract RepositoryItem CreateItemInstance (string fullLocalName);

        public abstract void Delete (RepositoryItem  item);

        public abstract RepositoryItem GetCopy (RepositoryItem remoteItem);

        public abstract bool IsSync (RepositoryItem item);
    }
}

