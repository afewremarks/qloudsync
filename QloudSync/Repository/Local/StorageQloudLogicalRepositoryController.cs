using System;
using System.Collections.Generic;
using GreenQloud.Repository;
using System.Linq;
using System.Xml;
using System.Threading;
using GreenQloud.Model;

namespace GreenQloud.Repository.Local
{
    class StorageQloudLogicalRepositoryController : LogicalRepositoryController
    {
        #region RepositoryController implementation

        public override List<RepositoryItem> Items {
            get {
                throw new NotImplementedException ();
            }
        }

        public override bool Exists (RepositoryItem repoObject)
        {
            throw new NotImplementedException ();
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

        public override RepositoryItem CreateObjectInstance (string fullPath)
        {
            throw new NotImplementedException ();
        }

        public override void Solve (RepositoryItem remoteObj)
        {
            throw new NotImplementedException ();
        }
    }

}


