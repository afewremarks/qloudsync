using System;
using GreenQloud.Repository;
using GreenQloud.Model;
using System.Collections.Generic;
using System.IO;

namespace GreenQloud.Repository.Local
{
    public interface IPhysicalRepositoryController
    {


        bool Exists (RepositoryItem repoObject);
        bool Exists (string repoObject);

        List<RepositoryItem> Items {
            get ;
        }

        List<RepositoryItem> GetItems(DirectoryInfo dir);
        void Copy (RepositoryItem item);
        void Delete (RepositoryItem  item);
        void Move (RepositoryItem item);
        RepositoryItem GetCopy (RepositoryItem remoteItem);
        bool IsSync (RepositoryItem item);
        RepositoryItem CreateItemInstance (string fullLocalName);
    }
}

