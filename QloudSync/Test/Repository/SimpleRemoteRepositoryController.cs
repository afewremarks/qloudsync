using System;
using System.IO;
using System.Collections.Generic;
using GreenQloud.Repository.Remote;
using GreenQloud.Repository.Model;
using GreenQloud.Repository.Local;
using System.Linq;

namespace GreenQloud.Test.SimpleRepository
{
    public class SimpleRemoteRepositoryController : RemoteRepositoryController
    {
        public Dictionary<RepoObject, string> list = new Dictionary<RepoObject, string>();
        public List<RepoObject> trash = new List<RepoObject>();
        SimplePhysicalRepositoryController physicalController;

        public SimpleRemoteRepositoryController (){
        }

        public SimpleRemoteRepositoryController (SimplePhysicalRepositoryController physicalController){
            this.physicalController = physicalController;
        }


        #region implemented abstract members of RemoteRepositoryController

        public override List<RepoObject> GetCopys (RepoObject file)
        {
            throw new NotImplementedException ();
        }

        public override bool ExistsVersion (RepoObject file)
        {
            throw new NotImplementedException ();
        }

        public override TransferResponse Download (RepoObject request)
        {
            if (physicalController.list.Any (o=>o.Key.FullLocalName == request.FullLocalName)){
                RepoObject key = physicalController.list.First (o=>o.Key.FullLocalName == request.FullLocalName).Key;
                physicalController.list[key] = list.First (o=>o.Key.FullLocalName==request.FullLocalName).Value;
                foreach (RepoObject k in physicalController.list.Keys)
                {
                    Console.Write (k.FullLocalName+" "+physicalController.list[k]);
                }
            }else{
                physicalController.Create (request);
            }
            return new TransferResponse (request, TransferType.DOWNLOAD);
        }

        public override TransferResponse Upload (RepoObject request)
        {
            if (list.Any (o=> o.Key.FullLocalName == request.FullLocalName))
            {
                RepoObject key = physicalController.list.First (o=>o.Key.FullLocalName == request.FullLocalName).Key;

                return Upload (request, physicalController.list[key]);
            }
            else
                return Upload (request, string.Empty);
        }

        public TransferResponse Upload (RepoObject request, string value){
            if (list.Any (o=>o.Key.FullLocalName==request.FullLocalName)){
                list [list.First (o=>o.Key.FullLocalName==request.FullLocalName).Key] = value;
            }else{
                list.Add (request, value);
            }
            return new TransferResponse(request, TransferType.UPLOAD);
        }

        public override TransferResponse MoveFileToTrash (RepoObject request)
        {
            list.Remove (request);
            trash.Add (request);
            return new TransferResponse (request, TransferType.UPLOAD);
        }

        public override TransferResponse MoveFolderToTrash (RepoObject folder)
        {
            throw new NotImplementedException ();
        }

        public override TransferResponse Delete (RepoObject request)
        {
            throw new NotImplementedException ();
        }

        public override TransferResponse SendLocalVersionToTrash (RepoObject request)
        {
            list.Remove (request);
            trash.Add (request);
           
            return new TransferResponse (request, TransferType.UPLOAD);
        }

        public override TransferResponse CreateFolder (RepoObject request)
        {
            throw new NotImplementedException ();
        }

        public override TransferResponse Copy (RepoObject source, RepoObject destination)
        {
            throw new NotImplementedException ();
        }

        public override bool ExistsFolder (RepoObject folder)
        {
            throw new NotImplementedException ();
        }

        public override bool Exists (RepoObject sqObject)
        {
            return list.Any (r=> r.Key.Name == sqObject.Name);
        }

        public override void DownloadFull (RepoObject file)
        {
            throw new NotImplementedException ();
        }

        public override void UpdateStorageQloud ()
        {
            throw new NotImplementedException ();
        }

        public override List<RepoObject> AllFiles {
            get {
                throw new NotImplementedException ();
            }
        }

        public override List<RepoObject> Files {
            get {
                List<RepoObject> files = new List<RepoObject>();
                files.AddRange(list.Keys.ToList());
                return files; 
            }
        }

        public override List<RepoObject> Folders {
            get {
                throw new NotImplementedException ();
            }
        }

        public override List<RepoObject> TrashFiles {
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

