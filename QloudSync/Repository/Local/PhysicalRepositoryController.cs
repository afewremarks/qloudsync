using System;
using GreenQloud.Repository;
using GreenQloud.Model;

namespace GreenQloud.Repository.Local
{
    public abstract class PhysicalRepositoryController : RepositoryController
    {

        public PhysicalRepositoryController (){
        }

        #region RepositoryController implementation

        public abstract bool Exists (GreenQloud.Model.RepositoryItem repoObject);


        public abstract System.Collections.Generic.List<GreenQloud.Model.RepositoryItem> Items {
            get ;
        }

        #endregion

        public abstract RepositoryItem CreateObjectInstance (string fullLocalName);

        public abstract void Delete (RepositoryItem  item);

        public abstract RepositoryItem GetCopy (RepositoryItem remoteItem);
    }
}

