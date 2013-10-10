using GreenQloud.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QloudSyncCore.Core.Persistence
{
    public class RepositoryItemRaven
    {
        #region implemented abstract members of RepositoryItemDAO

        public RepositoryItem Create (Event e)
        {
            if (!ExistsUnmoved (e.Item)) {
                Create (e.Item);
            } else {
                Update (e.Item);
            }
            if (e.Item.ResultItem != null && e.Item.ResultItem.Id == 0) {
                Create (e.Item.ResultItem);
                Update (e.Item);
            }else if(e.Item.ResultItem != null && e.Item.ResultItem.Id != 0)
                Update (e.Item.ResultItem);

            return e.Item;
        }

        public void Create (RepositoryItem item)
        {
            DataDocumentStore.Insert(item);
        }


        public void Update (RepositoryItem i)
        {
            RepositoryItem item = DataDocumentStore.Instance.OpenSession().Load<RepositoryItem>(i.Id);
            item.Key = i.Key;
            item.Repository.Id = i.Repository.Id;
            item.IsFolder = i.IsFolder;
            item.ResultItemId = (i.ResultItem == null) ? 0 : i.ResultItemId;
            item.ETag = i.ETag;
            item.LocalETag = i.LocalETag;
            item.Moved = i.Moved;
            item.UpdatedAt = i.UpdatedAt;
            DataDocumentStore.Instance.OpenSession().SaveChanges();
        }

        public List<RepositoryItem> All {
            get {
                return DataDocumentStore.GetAll<RepositoryItem>();
            }
        }
        #endregion

        public bool IsFolder (RepositoryItem item)
        {
            if (Exists(item))
            {
                int ID =  GetId (item);
                return GetById (ID).IsFolder;
            }
            return true;
        }

        public bool Exists (RepositoryItem item)
        {
            int i = DataDocumentStore.Instance.OpenSession().Query<RepositoryItem>().Where
                (r => r.Key == item.Key).Count();
            return i > 0;
        }

        public bool ExistsUnmoved (RepositoryItem item)
        {
            int i = DataDocumentStore.Instance.OpenSession().Query<RepositoryItem>().Where
                (r => r.Key == item.Key && r.Moved == true).Count();
            return i > 0;
        }

        public RepositoryItem GetFomDatabase (RepositoryItem item)
        {
            return DataDocumentStore.Instance.OpenSession().Query<RepositoryItem>().Where
            (r => r.Key.Equals(item.Key)).OrderByDescending(r => r.Id).First();
        }

        public bool ExistsUnmoved(string key, LocalRepository repo)
        {
            int i = DataDocumentStore.Instance.OpenSession().Query<RepositoryItem>().Where
                (r => r.Key.Equals(key) && r.Moved == true).Count();
            return i > 0;
        }

        public void MarkAsMoved (RepositoryItem item)
        {
            item.Moved = true;
            List<RepositoryItem> repoItems;
            RepositoryItem repoItem;
            var session = DataDocumentStore.Instance.OpenSession();
            if (item.IsFolder)
            {
                repoItems = session.Query<RepositoryItem>().Where
                    (r => r.Id.Equals(item.Id) && r.Key.StartsWith(item.Key)).ToList();
                foreach (RepositoryItem it in repoItems)
                {
                    it.Moved = true;
                }
                session.SaveChanges();
            }
            else
            {
                repoItem = session.Query<RepositoryItem>().Where
                    (r => r.Id.Equals(item.Id) && r.Key.Equals(item.Key)).First();
                repoItem.Moved = true;
                session.SaveChanges();
            }
        }

        public void ActualizeUpdatedAt (RepositoryItem item){
            var session = DataDocumentStore.Instance.OpenSession();
            RepositoryItem repoItem = session.Load<RepositoryItem>(item.Id);
            repoItem.UpdatedAt = item.UpdatedAt;
            session.SaveChanges();
        }
        public void UpdateETAG (RepositoryItem item){
            var session = DataDocumentStore.Instance.OpenSession();
            RepositoryItem repoItem = session.Load<RepositoryItem>(item.Id);
            repoItem.ETag = item.ETag;
            repoItem.LocalETag = item.LocalETag;
            session.SaveChanges();
        }

        public void MarkAsUnmoved (RepositoryItem item)
        {
            item.Moved = false;
            List<RepositoryItem> repoItems;
            RepositoryItem repoItem;
            var session = DataDocumentStore.Instance.OpenSession();
            if (item.IsFolder)
            {
                repoItems = session.Query<RepositoryItem>().Where
                   (r => r.Id.Equals(item.Id) && r.Key.StartsWith(item.Key)).ToList();
                foreach (RepositoryItem it in repoItems)
                {
                    it.Moved = false;
                }
                session.SaveChanges();
            }
            else
            {
                repoItem = session.Query<RepositoryItem>().Where
                    (r => r.Id.Equals(item.Id)).First();
                repoItem.Moved = false;
                session.SaveChanges();
            }
        }

        public int GetId (RepositoryItem item)
        {
            if (Exists (item)){
                return DataDocumentStore.Instance.OpenSession().Query<RepositoryItem>().Where
                    (r => r.Key.Equals(item.Key) && r.Repository.Id.Equals(item.Repository.Id)).OrderByDescending(ri => ri.Id).First().Id;
            }
            return 0;
        }

        public RepositoryItem GetById (int id)
        {
            return DataDocumentStore.Instance.OpenSession().Load<RepositoryItem>(id);
        }

    }
}
