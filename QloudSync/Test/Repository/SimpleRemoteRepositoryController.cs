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
        public Dictionary<RepositoryItem, string> list = new Dictionary<RepositoryItem, string>();
        public List<RepositoryItem> trash = new List<RepositoryItem>();
        SimplePhysicalRepositoryController physicalController;

        public SimpleRemoteRepositoryController (){
        }

        public SimpleRemoteRepositoryController (SimplePhysicalRepositoryController physicalController){
            this.physicalController = physicalController;
        }


        #region implemented abstract members of RemoteRepositoryController

        public override List<RepositoryItem> GetCopys (RepositoryItem file)
        {
            throw new NotImplementedException ();
        }

        public override bool ExistsVersion (RepositoryItem file)
        {
            throw new NotImplementedException ();
        }

        public override Transfer Download (RepositoryItem request)
        {
            if (physicalController.list.Any (o=>o.Key.FullLocalName == request.FullLocalName)){
                RepositoryItem key = physicalController.list.First (o=>o.Key.FullLocalName == request.FullLocalName).Key;
                physicalController.list[key] = list.First (o=>o.Key.FullLocalName==request.FullLocalName).Value;
            }else{
                physicalController.Create (request);
            }
            return new Transfer (request, TransferType.DOWNLOAD);
        }

        public override Transfer Upload (RepositoryItem request)
        {
            if (list.Any (o=> o.Key.FullLocalName == request.FullLocalName))
            {
                RepositoryItem key = physicalController.list.First (o=>o.Key.FullLocalName == request.FullLocalName).Key;

                return Upload (request, physicalController.list[key]);
            }
            else
                return Upload (request, string.Empty);
        }

        public Transfer Upload (RepositoryItem request, string value){
            if (list.Any (o=>o.Key.FullLocalName==request.FullLocalName)){
                list [list.First (o=>o.Key.FullLocalName==request.FullLocalName).Key] = value;
            }else{
                list.Add (request, value);
            }
            return new Transfer(request, TransferType.UPLOAD);
        }

        public override Transfer MoveFileToTrash (RepositoryItem request)
        {
            list.Remove (request);
            trash.Add (request);
            return new Transfer (request, TransferType.UPLOAD);
        }

        public override Transfer MoveFolderToTrash (RepositoryItem folder)
        {
            throw new NotImplementedException ();
        }

        public override Transfer Delete (RepositoryItem request)
        {
            throw new NotImplementedException ();
        }

        public override Transfer SendLocalVersionToTrash (RepositoryItem request)
        {
            list.Remove (request);
            trash.Add (request);
           
            return new Transfer (request, TransferType.UPLOAD);
        }

        public override Transfer CreateFolder (RepositoryItem request)
        {
            throw new NotImplementedException ();
        }

        public override Transfer Copy (RepositoryItem source, RepositoryItem destination)
        {
            throw new NotImplementedException ();
        }

        public override bool ExistsFolder (RepositoryItem folder)
        {
            throw new NotImplementedException ();
        }

        public override bool Exists (RepositoryItem sqObject)
        {
            return list.Any (r=> r.Key.Name == sqObject.Name);
        }

        public override void DownloadFull (RepositoryItem file)
        {
            throw new NotImplementedException ();
        }

        public override void UpdateStorageQloud ()
        {
            throw new NotImplementedException ();
        }

        public override List<RepositoryItem> AllFiles {
            get {
                throw new NotImplementedException ();
            }
        }

        public override List<RepositoryItem> Files {
            get {
                List<RepositoryItem> files = new List<RepositoryItem>();
                files.AddRange(list.Keys.ToList());
                return files; 
            }
        }

        public override List<RepositoryItem> Folders {
            get {
                throw new NotImplementedException ();
            }
        }

        public override List<RepositoryItem> TrashFiles {
            get {
                throw new NotImplementedException ();
            }
        }

        public override TimeSpan DiffClocks {
            get {
                return new TimeSpan (0);
            }
        }

        #endregion
    }
}

