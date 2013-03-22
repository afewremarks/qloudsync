using System;
using System.Collections.Generic;
using System.Linq;
using Amazon.S3.Model;
using GreenQloud.Util;
using GreenQloud.Repository.Model;
using GreenQloud.Repository.Local;

namespace GreenQloud.Repository.Remote
{
    public abstract class RemoteRepositoryController : RepositoryController
    {
        protected LogicalRepositoryController logicalController;

        public RemoteRepositoryController (){
        }

        #region Repo implementation

        public void CreateOrUpdate (RepoObject remoteObj)
        {
            throw new NotImplementedException ();
        }

        public List<string> FilesNames {
            get {
                throw new NotImplementedException ();
            }
        }

        #endregion

        public RemoteRepositoryController (LogicalRepositoryController logicalController)
        {
            CurrentTransfer = new TransferResponse();
            this.logicalController = logicalController;
        }

        public long FullSizeTransfer {
            get;
            set;
        }

        public TransferResponse CurrentTransfer{
            set; get;
        }

        public abstract List<RepoObject> AllFiles {
            get;
        }

        public abstract List<RepoObject> Files{
            get;
        }

        public abstract List<RepoObject> Folders{
            get;
        }

        public abstract List<RepoObject> TrashFiles {
            get;
        }

        public abstract List<RepoObject> GetCopys (RepoObject file);

        public abstract TimeSpan DiffClocks{
            get;              
        }

        public static void Authenticate (string username, string password){
        }

        public static string DefaultBucketName {
            get 
            {
                return string.Concat(Credential.Username, GlobalSettings.SuffixNameBucket).ToLower();
            }
        }

        public abstract bool ExistsVersion (RepoObject file);
        public abstract TransferResponse Download (RepoObject request);
        public abstract TransferResponse Upload (RepoObject request);
        public abstract TransferResponse MoveFileToTrash (RepoObject request);
        public abstract TransferResponse MoveFolderToTrash (RepoObject folder);
        public abstract TransferResponse Delete(RepoObject request);
        public abstract TransferResponse SendLocalVersionToTrash (RepoObject request);
        public abstract TransferResponse CreateFolder (RepoObject request);
        public abstract TransferResponse Copy (RepoObject source, RepoObject destination);
        public abstract bool ExistsFolder (RepoObject folder);
        public abstract bool Exists (RepoObject sqObject);
        public abstract void DownloadFull (RepoObject  file);

        public abstract void UpdateStorageQloud ();
    }
}

