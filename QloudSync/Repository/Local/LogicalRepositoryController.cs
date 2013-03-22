using System;
using System.Collections.Generic;
using GreenQloud.Repository;
using System.Linq;
using System.Xml;
using System.Threading;
using GreenQloud.Repository.Model;

namespace GreenQloud.Repository.Local
{
	public abstract class LogicalRepositoryController : RepositoryController
	{
        #region RepositoryController implementation
        public abstract RepoObject CreateObjectInstance (string fullPath);
        public abstract void Solve (RepoObject remoteObj);
        public abstract bool Exists (RepoObject repoObject);
        public abstract List<string> FilesNames {
            get;
        }
        public abstract List<RepoObject> Files {
            get;
        }
        #endregion

        public abstract List<LocalRepository> LocalRepositories {
            get; set;
        }
	}
}


