using System;
using System.Collections.Generic;
using System.Linq;
using Amazon.S3.Model;
using QloudSync.Net.S3;
using QloudSync.Util;

namespace  QloudSync.Repository
{
    public class RemoteRepo : Repo
    {
        private ConnectionManager connection; 

        public RemoteRepo ()
        {
            this.connection =  new ConnectionManager (DefaultBucketName);
        }

        public ConnectionManager Connection {
            get {
                return connection;
            }
        }

		#region Files
        public List<RemoteFile> Files{ 
            get {
                return AllFiles.Where(sobj => !IsTrashFile (sobj) && sobj.Name != Constant.CLOCK_TIME ).ToList();
            } 
        }

        public List<RemoteFile> AllFiles{ 
            get {
                return GetRemoteFiles (connection.GetFiles());
            } 
        }

		private List<RemoteFile> GetRemoteFiles (List<S3Object> files)
		{
			List <RemoteFile> remoteFiles = new List <RemoteFile> ();
			
			foreach (S3Object file in files) {
				remoteFiles.Add (new RemoteFile (file));
			}
			return remoteFiles;
		}

        public List<RemoteFile> TrashFiles {
            get{ return AllFiles.Where(f => IsTrashFile(f)).ToList();} 
        }

		public List<File> FilesChanged{ set; get;}

		#endregion

		#region Auxiliar

        public bool HasChanges{ set; get; }

        public  bool ExistsTrashFolder {
            get {
                return  TrashFiles.Where(rm => IsTrashFile(rm)).Any();
            }
        }

        public TimeSpan DiffClocks 
        {
            get{
                return connection.CalculateDiffClocks();
            }
        }

        public bool Connected {
            get {
                return connection.Reconnect () != null;
            }
        }

        public static string DefaultBucketName {
            get 
            {
                return string.Concat(Credential.Username, GlobalSettings.SuffixNameBucket);
            }
        }

		#endregion

        public bool InitBucket ()
        {
            if (!connection.ExistsBucket)
                return connection.CreateBucket();
            
            else return true; 
        }

        public bool InitTrashFolder ()
        {
            if (!ExistsTrashFolder) {
                return connection.CreateTrashFolder ();
            }
            else return true;
        }

        public void Download (RemoteFile remoteFile)
        {
            //TODO observar aqui
            if (!IsTrashFile (remoteFile) && !LocalRepo.PendingChanges.Where (c => c.File.FullLocalName == remoteFile.FullLocalName && c.Event == System.IO.WatcherChangeTypes.Deleted).Any())
                connection.Download (remoteFile);
        }


        public void Upload (File file)
        {
            connection.Upload(file);
        }

        public void CreateFolder (Folder folder)
        {
            connection.CreateFolder (folder);
        }

        public void Move (File old, File newO)
        {
            connection.Copy (old, newO);
            connection.CopyToTrash (old);
            if (Files.Where (rf => rf.Name == old.Name).Any())
                connection.Delete (old);
        }
        
        public void MoveToTrash (File  SQObject){
            connection.CopyToTrash ( SQObject);
            UpdateTrashFolder ( SQObject);
            connection.Delete ( SQObject);
        }

        public bool ExistsInBucket (File file)
        {
            return Files.Where (rf => rf.AbsolutePath == file.AbsolutePath 
                                || rf.AbsolutePath.Contains (file.AbsolutePath)).Any ();
        }
        
        public bool IsTrashFile (RemoteFile file)
        {
            return file.InTrash;
        }

        public bool SendToTrash (LocalFile file)
        {
            connection.UploadToTrash (file);
            Logger.LogInfo ("Connection","File "+file.Name+" was sent to trash folder.");
            UpdateTrashFolder (file);

            return TrashFiles.Where (rf => rf.TrashFullName == file.TrashFullName+"(1)").Any ();
        }
        
        public bool SendToTrash (RemoteFile remoteFile)
        {
            connection.CopyToTrash (remoteFile);
            bool copySucessfull =  TrashFiles.Where (rm => remoteFile.AbsolutePath+"(0)" == rm.AbsolutePath).Any();
            if (copySucessfull)
                UpdateTrashFolder (remoteFile);
            
            return copySucessfull;
        }
        
        public void UpdateTrashFolder (File  SQObject)
        {
            if ( SQObject.IsAFolder)
                return;
            List<RemoteFile> versions = GetVersionsOrderByLastModified ( SQObject);
           
            foreach (RemoteFile version in versions) {
                //if (version.IsAFolder)
                //  continue;
                //Console.WriteLine ("Incrementa versao");
                string newName = "";
                string oldName = version.FullLocalName;
                int sizeOldName = oldName.Length;
                int v = int.Parse(oldName[sizeOldName-2].ToString())+1;
                newName = oldName.Substring (0, sizeOldName - 3) +"("+v+")";
                
                connection.CopyInTrash (version, new RemoteFile(newName));
                connection.DeleteInTrash (version);
            }
            versions = GetVersionsOrderByLastModified ( SQObject);
            while (versions.Count > 3) {
                connection.DeleteInTrash (versions.First());
                versions = GetVersionsOrderByLastModified( SQObject);                
            }
        }

		public void Delete (RemoteFile file)
		{
			connection.Delete (file);
		}
        
        private List<RemoteFile> GetVersionsOrderByLastModified (File  SQObject)  {
            return  TrashFiles.Where (ft => ft.AbsolutePath.Contains ( SQObject.AbsolutePath)).OrderBy(ft => ft.AsS3Object.LastModified).ToList<RemoteFile>();
        }

        public void DeleteAllFilesInBucket(){
			connection.DeleteAllFilesInBucket();
		}

        public bool Initialized ()
        {
            if (InitBucket())
                return InitTrashFolder();
            return false;
        }
    }
}

