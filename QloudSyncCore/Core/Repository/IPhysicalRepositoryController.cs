using System;
using GreenQloud.Repository;
using GreenQloud.Model;
using System.Collections.Generic;
using System.IO;

namespace GreenQloud.Repository
{
    public interface IPhysicalRepositoryController
    {


        bool Exists (RepositoryItem repoObject);
        bool Exists (string repoObject);
        List<RepositoryItem> GetItems(string prefixDir);
        void Copy (RepositoryItem item);
        void Delete (RepositoryItem  item);
        void Move (RepositoryItem item);
        bool IsSync (RepositoryItem item);
        RepositoryItem CreateItemInstance (string fullLocalName);
    }
}

