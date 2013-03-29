using System;
using System.IO;
using System.Collections.Generic;
using GreenQloud.Repository.Remote;
using GreenQloud.Model;
using GreenQloud.Repository.Local;
using System.Linq;

namespace GreenQloud.Test.SimpleRepository
{
    public class SimpleRemoteRepositoryController : RemoteRepositoryController
    {
        #region implemented abstract members of RemoteRepositoryController

        public override TimeSpan DiffClocks {
            get {
                return new TimeSpan (0);
            }
        }
       
        List<RepositoryItem> recentChangedItems = new List<RepositoryItem>();
        public override List<GreenQloud.Model.RepositoryItem> RecentChangedItems (DateTime LastSyncTime) {
            return recentChangedItems;
        }


        public override List<GreenQloud.Model.RepositoryItem> GetCopys (GreenQloud.Model.RepositoryItem file)
        {
            throw new NotImplementedException ();
        }

        public override bool ExistsVersion (GreenQloud.Model.RepositoryItem file)
        {
            throw new NotImplementedException ();
        }

        public override Transfer Download (RepositoryItem item)
        {
            if (physicalController.list.Any (o=>o.Key.FullLocalName == item.FullLocalName)){
                RepositoryItem key = physicalController.list.First (o=>o.Key.FullLocalName == item.FullLocalName).Key;
                physicalController.list[key] = list.First (o=>o.Key.FullLocalName==item.FullLocalName).Value;
            }else{
                physicalController.Create (item);
            }
            return new Transfer (item, TransferType.DOWNLOAD);
        }

        public override Transfer Upload (RepositoryItem item)
        {
            if (list.Any (o=> o.Key.FullLocalName == item.FullLocalName))
            {
                RepositoryItem key = physicalController.list.First (o=>o.Key.FullLocalName == item.FullLocalName).Key;
                
                return Upload (item, physicalController.list[key]);
            }
            else
                return Upload (item, string.Empty);
        }

        public override Transfer MoveToTrash (RepositoryItem item)
        {
            list.Remove (item);
            trash.Add (item);
            return new Transfer (item, TransferType.UPLOAD);
        }

        public override Transfer Delete (RepositoryItem item)
        {
            throw new NotImplementedException ();
        }

        public override Transfer SendLocalVersionToTrash (RepositoryItem item)
        {
            list.Remove (item);
            trash.Add (item);
            
            return new Transfer (item, TransferType.UPLOAD);
        }

        public override Transfer CreateFolder (RepositoryItem item)
        {
            throw new NotImplementedException ();
        }

        public override Transfer Copy (GreenQloud.Model.RepositoryItem source, GreenQloud.Model.RepositoryItem destination)
        {
            throw new NotImplementedException ();
        }

        public override bool Exists (GreenQloud.Model.RepositoryItem sqObject)
        {
            return list.Any (r=> r.Key.Name == sqObject.Name);
        }


        public override bool ExistsCopies (GreenQloud.Model.RepositoryItem item)
        {
            return list.Any (element => element.Key.RemoteMD5Hash == item.RemoteMD5Hash && element.Key.FullLocalName != item.FullLocalName);
        }

        public override List<GreenQloud.Model.RepositoryItem> AllItems {
            get {
                throw new NotImplementedException ();
            }
        }

        public override List<GreenQloud.Model.RepositoryItem> Items {
            get {
                List<RepositoryItem> files = new List<RepositoryItem>();
                files.AddRange(list.Keys.ToList());
                return files; 
            }
        }

       
        public override List<GreenQloud.Model.RepositoryItem> TrashItems {
            get {
                throw new NotImplementedException ();
            }
        }

        #endregion

        public Dictionary<RepositoryItem, string> list = new Dictionary<RepositoryItem, string>();
        public List<RepositoryItem> trash = new List<RepositoryItem>();
        SimplePhysicalRepositoryController physicalController;

        public SimpleRemoteRepositoryController (){
        }

        public SimpleRemoteRepositoryController (SimplePhysicalRepositoryController physicalController){
            this.physicalController = physicalController;
        }
              
        public Transfer Upload (RepositoryItem request, string value){
            if (list.Any (o=>o.Key.FullLocalName==request.FullLocalName)){
                list [list.First (o=>o.Key.FullLocalName==request.FullLocalName).Key] = value;
            }else{
                list.Add (request, value);
            }
            return new Transfer(request, TransferType.UPLOAD);
        }
       
    }
}

