using System;
using System.Collections.Generic;
using GreenQloud.Repository;
using System.Linq;
using System.Xml;
using System.Threading;
using GreenQloud.Repository.Model;

namespace GreenQloud.Repository.Local
{
    class StorageQloudLogicalRepositoryController : LogicalRepositoryController
    {
        #region Repo implementation

        public override List<string> FilesNames {
            get {
                throw new NotImplementedException ();
            }
        }

        public override List<RepoObject> Files {
            get {
                throw new NotImplementedException ();
            }
        }

        #endregion

        #region implemented abstract members of LogicalRepositoryController

        public override List<LocalRepository> LocalRepositories {
            get {
                throw new NotImplementedException ();
            }
            set {
                throw new NotImplementedException ();
            }
        }

        #endregion

        public override RepoObject CreateObjectInstance (string fullPath)
        {
            throw new NotImplementedException ();
        }

        public override bool Exists (RepoObject repoObject)
        {
            throw new NotImplementedException ();
        }

        public override void Solve (RepoObject remoteObj)
        {
            throw new NotImplementedException ();
        }
    }

}


