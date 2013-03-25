using System;
using System.Collections.Generic;
using GreenQloud.Model;

namespace GreenQloud.Repository
{
    public interface RepositoryController
    {       
        bool Exists (RepositoryItem repoObject);

        List<RepositoryItem> Items {
            get;
        }

    }

}

