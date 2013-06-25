using System;
using System.IO;
using System.Collections.Generic;
using GreenQloud.Model;
using System.Linq;
using GreenQloud.Persistence;

namespace GreenQloud.Persistence
{
    public abstract class RepositoryItemDAO
    {
        public abstract void Update (RepositoryItem i);
        public abstract void Create (RepositoryItem e);
        public abstract void MarkAsMoved (RepositoryItem item);
        public abstract void MarkAsUnmoved (RepositoryItem item);
        public abstract void ActualizeUpdatedAt (RepositoryItem resultItem);
        public abstract void UpdateETAG (RepositoryItem i);
        public abstract List<RepositoryItem> All{
            get;
        }

    }

}

