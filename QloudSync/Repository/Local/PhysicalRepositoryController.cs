using System;
using GreenQloud.Repository;
using GreenQloud.Repository.Model;

namespace GreenQloud.Repository.Local
{
    public abstract class PhysicalRepositoryController : RepositoryController
    {
        protected LogicalRepositoryController logicalController;

        public PhysicalRepositoryController (){
        }

        public PhysicalRepositoryController (LogicalRepositoryController logicalController)
        {
            this.logicalController = logicalController;
        }

        #region RepositoryController implementation

        public abstract void CreateOrUpdate (GreenQloud.Repository.Model.RepoObject remoteObj);
        public abstract bool Exists (GreenQloud.Repository.Model.RepoObject repoObject);


        public abstract System.Collections.Generic.List<string> FilesNames {
            get;
        }

        public abstract System.Collections.Generic.List<GreenQloud.Repository.Model.RepoObject> Files {
            get ;
        }

        #endregion

        public abstract RepoObject CreateObjectInstance (string fullLocalName);

        public abstract void Delete (RepoObject repoObj);
    }
}

