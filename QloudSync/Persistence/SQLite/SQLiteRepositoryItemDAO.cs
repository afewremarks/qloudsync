using System;
using System.IO;
using System.Collections.Generic;
using GreenQloud.Model;
using System.Linq;
using GreenQloud.Persistence;

namespace GreenQloud.Persistence
{
    class SQLiteRepositoryItemDAO : RepositoryItemDAO
    {
        #region implemented abstract members of RepositoryItemDAO
        public override void Create (RepositoryItem e)
        {
            throw new NotImplementedException ();
        }
        public override List<RepositoryItem> All {
            get {
                throw new NotImplementedException ();
            }
        }
        #endregion
    }

}

