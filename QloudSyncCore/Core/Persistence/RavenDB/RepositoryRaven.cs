using GreenQloud.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QloudSyncCore.Core.Persistence
{
    public class RepositoryRaven
    {
         #region implemented abstract members of RepositoryDAO

        public void Create (LocalRepository e)
        {
            DataDocumentStore.Insert(e);
        }

        public void Update (LocalRepository repo)
        {
            LocalRepository localRepo = DataDocumentStore.Instance.OpenSession().Load<LocalRepository>(repo.Id);
            localRepo.Path = repo.Path;
            localRepo.Recovering = repo.Recovering;
            localRepo.RemoteFolder = repo.RemoteFolder;
            DataDocumentStore.Instance.OpenSession().SaveChanges();
        }

        public List<LocalRepository> All {
            get {
              return DataDocumentStore.Instance.OpenSession().Query<LocalRepository>().ToList<LocalRepository>();  
            }
        }

        public void DeleteAll ()
        {
            DataDocumentStore.Clear<LocalRepository>();
        }
        #endregion

        public LocalRepository GetRepositoryByItemFullName (string itemFullName)
        {
            return DataDocumentStore.Instance.OpenSession().Query<LocalRepository>().Where( r => itemFullName.StartsWith(r.Path)).First();
        }

        public LocalRepository FindOrCreate (string root, string remoteFolder)
        {
            List<LocalRepository> repos = DataDocumentStore.Instance.OpenSession().Query<LocalRepository>().Where
                (r => r.Path == root && r.RemoteFolder == remoteFolder).ToList<LocalRepository>();
            LocalRepository repo;
            if (repos.Count > 0) {
                repo = repos.First ();
                return repo;
            } else {
                repo = new LocalRepository (root, remoteFolder);
                repo.Recovering = true;
                Create (repo);
                return FindOrCreate (root, remoteFolder);
            }
        }

        public LocalRepository GetById(int id)
        {
            return DataDocumentStore.Instance.OpenSession().Load<LocalRepository>(id);
        }
    
    }
}
