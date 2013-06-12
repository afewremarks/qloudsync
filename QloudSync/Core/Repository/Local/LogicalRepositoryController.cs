using System;
using System.Collections.Generic;
using GreenQloud.Repository;
using System.Linq;
using System.Xml;
using System.Threading;
using GreenQloud.Model;

namespace GreenQloud.Repository.Local
{
	public abstract class LogicalRepositoryController 
	{
        #region RepositoryController implementation

        public abstract bool Exists (RepositoryItem repoObject);
     
        public abstract List<RepositoryItem> Items {
            get;
        }

        #endregion

        public abstract void Solve (RepositoryItem remoteObj);
        public abstract List<LocalRepository> LocalRepositories {
            get; set;
        }
	}
}


