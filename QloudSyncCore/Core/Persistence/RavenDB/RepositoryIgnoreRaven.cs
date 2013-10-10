using GreenQloud.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QloudSyncCore.Core.Persistence
{
    public class RepositoryIgnoreRaven
    {   
        public void Create (LocalRepository repo, string path)
        {
            DataDocumentStore.Insert(new RepositoryIgnore(repo, path));
        }

        public List<RepositoryIgnore> All(LocalRepository repo)
        {
            return DataDocumentStore.Instance.OpenSession().Query<RepositoryIgnore>().Where(ri => ri.RepositoryId == repo.Id).ToList();
        }
    }
}
