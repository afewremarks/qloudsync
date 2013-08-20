using System;
using GreenQloud.Repository.Local;
using GreenQloud.Model;
using System.Collections.Generic;
using LitS3;

namespace GreenQloud.Repository
{
    public interface IRemoteRepositoryController
    {


        List<RepositoryItem> Items {
            get;
        }

        List<RepositoryItem> TrashItems {
            get;
        }

        List<RepositoryItem> GetCopys (RepositoryItem file);
        void Move (RepositoryItem item);
        string RemoteETAG (RepositoryItem item);
        void Download (RepositoryItem request, bool recursive);
        void Upload (RepositoryItem request);
        void Delete(RepositoryItem request);
        void Copy (RepositoryItem item);
        bool Exists (RepositoryItem sqObject);
        bool ExistsCopies (RepositoryItem item);
        GetObjectResponse GetMetadata (string key);
    }
}

