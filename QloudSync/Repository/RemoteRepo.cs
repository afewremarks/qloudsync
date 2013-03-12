using System;
using System.Collections.Generic;
using System.Linq;
using Amazon.S3.Model;
using GreenQloud.Util;

namespace GreenQloud
{
    public abstract class RemoteRepo
    {


        public RemoteRepo ()
        {
            CurrentTransfer = new TransferResponse();
        }

        public long FullSizeTransfer {
            get;
            set;
        }

        public TransferResponse CurrentTransfer{
            set; get;
        }

        public abstract List<StorageQloudObject> AllFiles {
            get;
        }

        public abstract List<StorageQloudObject> Files{
            get;
        }

        public abstract List<StorageQloudObject> Folders{
            get;
        }

        public abstract List<StorageQloudObject> TrashFiles {
            get;
        }

        public abstract List<StorageQloudObject> GetCopys (StorageQloudObject file);

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

        public abstract bool ExistsVersion (StorageQloudObject file);
        public abstract TransferResponse Download (StorageQloudObject request);
        public abstract TransferResponse Upload (StorageQloudObject request);
        public abstract TransferResponse MoveFileToTrash (StorageQloudObject request);
        public abstract TransferResponse MoveFolderToTrash (StorageQloudObject folder);
        public abstract TransferResponse Delete(StorageQloudObject request);
        public abstract TransferResponse SendLocalVersionToTrash (StorageQloudObject request);
        public abstract TransferResponse CreateFolder (StorageQloudObject request);
        public abstract TransferResponse Copy (StorageQloudObject source, StorageQloudObject destination);
        public abstract bool ExistsFolder (StorageQloudObject folder);
        public abstract bool Exists (StorageQloudObject sqObject);
        public abstract void DownloadFull (StorageQloudObject  file);

        public abstract void UpdateStorageQloud ();
    }
}

