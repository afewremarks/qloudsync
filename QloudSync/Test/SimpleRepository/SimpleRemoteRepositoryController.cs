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
        public override List<GreenQloud.Model.RepositoryItem> RecentChangedItems {
            get {
                return recentChangedItems;
            }
            set {
                recentChangedItems = value;
            }
        }


        public override List<GreenQloud.Model.RepositoryItem> GetCopys (GreenQloud.Model.RepositoryItem file)
        {
            throw new NotImplementedException ();
        }

        public override bool ExistsVersion (GreenQloud.Model.RepositoryItem file)
        {
            throw new NotImplementedException ();
        }

        public override Transfer Download (GreenQloud.Model.RepositoryItem request)
        {
            if (physicalController.list.Any (o=>o.Key.FullLocalName == request.FullLocalName)){
                RepositoryItem key = physicalController.list.First (o=>o.Key.FullLocalName == request.FullLocalName).Key;
                physicalController.list[key] = list.First (o=>o.Key.FullLocalName==request.FullLocalName).Value;
            }else{
                physicalController.Create (request);
            }
            return new Transfer (request, TransferType.DOWNLOAD);
        }

        public override Transfer Upload (GreenQloud.Model.RepositoryItem request)
        {
            if (list.Any (o=> o.Key.FullLocalName == request.FullLocalName))
            {
                RepositoryItem key = physicalController.list.First (o=>o.Key.FullLocalName == request.FullLocalName).Key;
                
                return Upload (request, physicalController.list[key]);
            }
            else
                return Upload (request, string.Empty);
        }

        public override Transfer MoveFileToTrash (GreenQloud.Model.RepositoryItem request)
        {
            list.Remove (request);
            trash.Add (request);
            return new Transfer (request, TransferType.UPLOAD);
        }

        public override Transfer MoveFolderToTrash (GreenQloud.Model.RepositoryItem folder)
        {
            throw new NotImplementedException ();
        }

        public override Transfer Delete (GreenQloud.Model.RepositoryItem request)
        {
            throw new NotImplementedException ();
        }

        public override Transfer SendLocalVersionToTrash (GreenQloud.Model.RepositoryItem request)
        {
            list.Remove (request);
            trash.Add (request);
            
            return new Transfer (request, TransferType.UPLOAD);
        }

        public override Transfer CreateFolder (GreenQloud.Model.RepositoryItem request)
        {
            throw new NotImplementedException ();
        }

        public override Transfer Copy (GreenQloud.Model.RepositoryItem source, GreenQloud.Model.RepositoryItem destination)
        {
            throw new NotImplementedException ();
        }

        public override bool ExistsFolder (GreenQloud.Model.RepositoryItem folder)
        {
            throw new NotImplementedException ();
        }

        public override bool Exists (GreenQloud.Model.RepositoryItem sqObject)
        {
            return list.Any (r=> r.Key.Name == sqObject.Name);
        }

        public override void DownloadFull (GreenQloud.Model.RepositoryItem file)
        {
            throw new NotImplementedException ();
        }

        public override bool ExistsCopys (GreenQloud.Model.RepositoryItem item)
        {
            return list.Any (element => element.Key.MD5Hash == item.MD5Hash && element.Key.FullLocalName != item.FullLocalName);
        }

        public override List<GreenQloud.Model.RepositoryItem> AllFiles {
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

       
        public override List<GreenQloud.Model.RepositoryItem> TrashFiles {
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

