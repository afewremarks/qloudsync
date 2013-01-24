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
        private static ConnectionManager connection = new ConnectionManager(DefaultBucketName);

		#region Files
        public static List<RemoteFile> Files{ 
            get {
                return AllFiles.Where(sobj => !RemoteRepo.IsTrashFile (sobj) && sobj.Name != Constant.CLOCK_TIME ).ToList();
            } 
        }

        public static List<RemoteFile> AllFiles{ 
            get {
                return GetRemoteFiles (connection.GetFiles());
            } 
        }

		private static List<RemoteFile> GetRemoteFiles (List<S3Object> files)
		{
			List <RemoteFile> remoteFiles = new List <RemoteFile> ();
			
			foreach (S3Object file in files) {
				remoteFiles.Add (new RemoteFile (file));
			}
			return remoteFiles;
		}

        public static List<RemoteFile> TrashFiles {
            get{ return AllFiles.Where(f => IsTrashFile(f)).ToList();} 
        }

		public static List<File> FilesChanged{ set; get;}

		#endregion

		#region Auxiliar

        public static bool HasChanges{ set; get; }

        public static bool ExistsTrashFolder {
            get {
                return  RemoteRepo.TrashFiles.Where(rm => IsTrashFile(rm)).Any();
            }
        }

        public static TimeSpan DiffClocks 
        {
            get{
                return connection.CalculateDiffClocks();
            }
        }

        public static bool Connected {
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

        public static bool InitBucket ()
        {
            if (!connection.ExistsBucket)
                return connection.CreateBucket();
            
            else return true; 
        }

        public static bool InitTrashFolder ()
        {
            if (!RemoteRepo.ExistsTrashFolder) {
                return connection.CreateTrashFolder ();
            }
            else return true;
        }

        public static void Download (RemoteFile remoteFile)
        {
            Console.WriteLine (remoteFile.FullLocalName);
            //TODO observar aqui
            if (!RemoteRepo.IsTrashFile (remoteFile) && !LocalRepo.PendingChanges.Where (c => c.File.FullLocalName == remoteFile.FullLocalName && c.Event == System.IO.WatcherChangeTypes.Deleted).Any())
                connection.Download (remoteFile);
        }


        public static void Upload (File file)
        {
            connection.Upload(file);
        }

        public static void CreateFolder (Folder folder)
        {
            connection.CreateFolder (folder);
        }

        public static void Move (File old, File newO)
        {
            connection.Copy (old, newO);
            connection.CopyToTrash (old);
            if (RemoteRepo.Files.Where (rf => rf.Name == old.Name).Any())
                connection.Delete (old);
        }
        
        public static void MoveToTrash (File  SQObject){
            connection.CopyToTrash ( SQObject);
            UpdateTrashFolder ( SQObject);
            connection.Delete ( SQObject);
        }

        public static bool ExistsInBucket (File file)
        {
            return Files.Where (rf => rf.AbsolutePath == file.AbsolutePath 
                                || rf.AbsolutePath.Contains (file.AbsolutePath)).Any ();
        }
        
        public static bool IsTrashFile (RemoteFile file)
        {
            return file.InTrash;
        }

        public static bool SendToTrash (LocalFile file)
        {
            connection.UploadToTrash (file);
            Logger.LogInfo ("Connection","File "+file.Name+" was sent to trash folder.");
            UpdateTrashFolder (file);

            return RemoteRepo.TrashFiles.Where (rf => rf.TrashFullName == file.TrashFullName+"(1)").Any ();
        }
        
        public static bool SendToTrash (RemoteFile remoteFile)
        {
            connection.CopyToTrash (remoteFile);
            bool copySucessfull =  RemoteRepo.TrashFiles.Where (rm => remoteFile.AbsolutePath+"(0)" == rm.AbsolutePath).Any();
            if (copySucessfull)
                UpdateTrashFolder (remoteFile);
            
            return copySucessfull;
        }
        
        public static void UpdateTrashFolder (File  SQObject)
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

		public static void Delete (RemoteFile file)
		{
			connection.Delete (file);
		}
        
        private static List<RemoteFile> GetVersionsOrderByLastModified (File  SQObject)  {
            return  RemoteRepo.TrashFiles.Where (ft => ft.AbsolutePath.Contains ( SQObject.AbsolutePath)).OrderBy(ft => ft.AsS3Object.LastModified).ToList<RemoteFile>();
        }

        public static void DeleteAllFilesInBucket(){
			connection.DeleteAllFilesInBucket();
		}
    }
}

