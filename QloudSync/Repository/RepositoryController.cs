using System;
using System.Collections.Generic;
using GreenQloud.Repository.Model;

namespace GreenQloud.Repository
{
    public interface RepositoryController
    {       
        bool Exists (RepoObject repoObject);

        List<string> FilesNames {
            get;
        }

        List<RepoObject> Files {
            get;
        }

    }

}

