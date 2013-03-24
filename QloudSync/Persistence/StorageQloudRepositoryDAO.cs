using System;
using System.IO;
using System.Collections.Generic;
using GreenQloud.Model;
using System.Linq;

namespace GreenQloud.Persistence
{
    class StorageQloudRepositoryDAO : RepositoryDAO
    {
        #region implemented abstract members of RepoDAO
        public override void Create (LocalRepository e)
        {
            throw new NotImplementedException ();
        }
        public override List<LocalRepository> All {
            get {
                throw new NotImplementedException ();
            }
        }
        #endregion

        public LocalRepository GetRepositoryByItemFullName (string itemFullName)
        {
            throw new NotImplementedException ();
        }
    }

}

