using System;
using GreenQloud.Repository;
using GreenQloud.Model;
using System.Collections.Generic;
using LitS3;

namespace GreenQloud.Repository
{
    public interface IRemoteRepositoryController
    {
        void Move (RepositoryItem item);
        string RemoteETAG (RepositoryItem item);
        void Download (RepositoryItem request, bool recursive);
        void Upload (RepositoryItem request);
        void Delete(RepositoryItem request);
        void Copy (RepositoryItem item);
        bool Exists (RepositoryItem sqObject);
        GetObjectResponse GetMetadata (string key, bool recoveryFolder);
        long GetContentLength(string key);
        List<RepositoryItem> GetItems(string prefix);
    }
}

