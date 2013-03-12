using System;
using System.Collections.Generic;
using System.Linq;
using Amazon.S3.Model;
using GreenQloud.Net.S3;
using GreenQloud.Util;

namespace GreenQloud.Repository
{
    public class RemoteRepo
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
        public List<StorageQloudObject> Files{ 
            get {
                return AllFiles.Where(sobj => !IsTrashFile (sobj) && sobj.Name != Constant.CLOCK_TIME && !sobj.Name.EndsWith("/")).ToList();
            } 
        }

        
        List<StorageQloudObject> immerseFolderList;


        void ImmerseFolder (string path)
        {
            immerseFolderList.Add(new StorageQloudObject(path));
            int lastIndex = path.LastIndexOf ("/");
            path = path.Substring (0, lastIndex);

            if(path != RuntimeSettings.HomePath)
                ImmerseFolder(path);

        }

        public List<StorageQloudObject> Folders{ 
            get {
                return AllFiles.Where(sobj => !IsTrashFile (sobj) && sobj.Name.EndsWith("/")).ToList();
            } 
        }

        public List<StorageQloudObject> AllFiles{ 
            get {
                return GetStorageQloudObjects (connection.GetFiles());
            } 
        }

		protected List<StorageQloudObject> GetStorageQloudObjects (List<S3Object> files)
		{
			List <StorageQloudObject> remoteFiles = new List <StorageQloudObject> ();
			
			foreach (S3Object file in files) {
                remoteFiles.Add (new StorageQloudObject (file));
			}
			return remoteFiles;
		}

        public List<StorageQloudObject> TrashFiles {
            get{
                return AllFiles.Where(f => IsTrashFile(f)).ToList();
            } 
        }


		#endregion

		#region Auxiliar

        public bool HasChanges{ set; get; }

        public  bool ExistsTrashFolder {
            get {
                return  TrashFiles.Any(rm => IsTrashFile(rm));
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

        public void Download (StorageQloudObject remoteFile)
        {
            connection.Download (remoteFile);
        }


        public void Upload (StorageQloudObject file)
        {
            if (!System.IO.File.Exists(file.FullLocalName))
            {
                Logger.LogInfo ("Upload", string.Format("Could not upload {0} because it does not exist in local repository.",file.AbsolutePath));
                return;
            }
            
            connection.GenericUpload ( file.RelativePathInBucket,  file.Name,  file.FullLocalName);
        }

        public void CreateFolder (StorageQloudObject folder)
        {
            connection.CreateFolder (folder);
        }

        public void Copy (StorageQloudObject source, StorageQloudObject destination)
        {
            if (source.InTrash)
                connection.GenericCopy (DefaultBucketName, source.TrashAbsolutePath, destination.RelativePathInBucket, destination.Name);
            else
                connection.GenericCopy (DefaultBucketName, source.AbsolutePath, destination.RelativePathInBucket, destination.Name);
            Logger.LogInfo ("Connection", "Copy is done");
        }

        public void CopyToTrashFolder (StorageQloudObject source, StorageQloudObject destination)
        {
            connection.GenericCopy (DefaultBucketName, source.AbsolutePath, destination.TrashRelativePath, destination.Name);
        }

        public void Move (StorageQloudObject source, StorageQloudObject destination)
        {
            string destinationName;
            if (source.IsAFolder)
                destinationName = source.Name;
            else
                destinationName = source.Name+"(0)";
            
            connection.GenericCopy (DefaultBucketName, source.AbsolutePath, source.TrashRelativePath, destinationName);
            if (Files.Any (rf => rf.Name == source.Name))
                connection.GenericDelete (source.AbsolutePath);
        }
        
        public void MoveFileToTrash (StorageQloudObject  file)
        {
            string destinationName;         
            destinationName = file.Name + "(1)";

            UpdateTrashFolder (file);


            connection.GenericCopy (DefaultBucketName, file.AbsolutePath, file.TrashRelativePath, destinationName);           


            connection.GenericDelete (file.AbsolutePath);
        }

        public void MoveFolderToTrash (StorageQloudObject folder)
        {

            connection.GenericCopy (DefaultBucketName, folder.AbsolutePath+"/", folder.TrashRelativePath, folder.Name+"/");           
            
            
            connection.GenericDelete (folder.AbsolutePath+"/");
        }

        public bool FolderExistsInBucket (StorageQloudObject folder)
        {
           return Folders.Any (rf => rf.AbsolutePath==folder.AbsolutePath || rf.AbsolutePath.Contains(folder.AbsolutePath));
        }

        public bool ExistsInBucket (StorageQloudObject file)
        {
            return Files.Any (rf => rf.AbsolutePath == file.AbsolutePath);
        }
        
        public bool IsTrashFile (StorageQloudObject file)
        {
            return file.InTrash;
        }

        public bool SendLocalVersionToTrash (StorageQloudObject file)
        {
            if (!System.IO.File.Exists(file.FullLocalName))
                return false;
            string key;
            if ( file.IsAFolder) {
                key =  file.Name;
            } else {
                key =  file.Name + "(0)";
            }
            Logger.LogInfo ("Connection","Uploading the file "+key+" to trash.");
            connection.GenericUpload ( file.TrashRelativePath, key,  file.FullLocalName);
            Logger.LogInfo ("Connection","File "+file.Name+" was sent to trash folder.");
            UpdateTrashFolder (file);

            return true;
        }
        
        public bool SendRemoteVersionToTrash (StorageQloudObject file)
        {
            string destinationName;
            if (file.IsAFolder)
                destinationName = file.Name;
            else
                destinationName = file.Name+"(0)";
            
            connection.GenericCopy (DefaultBucketName, file.AbsolutePath, file.TrashRelativePath, destinationName);
            bool copySucessfull =  TrashFiles.Any (rm => file.AbsolutePath+"(0)" == rm.AbsolutePath);
            if (copySucessfull)
                UpdateTrashFolder (file);
            
            return copySucessfull;
        }
        
        public void UpdateTrashFolder (StorageQloudObject  file)
        {
            if (file.IsAFolder)
                return;
            List<StorageQloudObject> versions = TrashFiles.Where (tf => tf.AbsolutePath != string.Empty && tf.AbsolutePath.Substring(0, tf.AbsolutePath.Length-3)== file.AbsolutePath).OrderByDescending(t => t.AbsolutePath).ToList<StorageQloudObject> ();

            int overload = versions.Count-2;
            for (int i=0; i<overload; i++) {
                StorageQloudObject v = versions[i];
                connection.GenericDelete(v.TrashAbsolutePath);
                versions.Remove(v);
            }

            foreach (StorageQloudObject version in versions) {
                if(version.AbsolutePath == string.Empty)
                    continue;
                int lastOpenParenthesis = version.AbsolutePath.LastIndexOf ("(")+1;
                int lastCloseParenthesis = version.AbsolutePath.LastIndexOf (")");
                int versionNumber = int.Parse (version.AbsolutePath.Substring (lastOpenParenthesis, lastCloseParenthesis - lastOpenParenthesis));
                versionNumber++;
                string newName = string.Format ("{0}{1})", version.AbsolutePath.Substring (0, lastOpenParenthesis), versionNumber);
                StorageQloudObject newVersion = new StorageQloudObject (newName);
                connection.GenericCopy (DefaultBucketName, version.TrashAbsolutePath, newVersion.TrashRelativePath, newVersion.Name);
                connection.GenericDelete (version.TrashAbsolutePath);
            }

        }

		public void Delete (StorageQloudObject file)
		{
			connection.GenericDelete (file.AbsolutePath);
		}
        
        private List<StorageQloudObject> GetVersionsOrderByLastModified (StorageQloudObject  file)  {
            return  TrashFiles.Where (ft => ft.AbsolutePath.Contains (file.AbsolutePath)).OrderBy(ft => ft.AsS3Object.LastModified).ToList<StorageQloudObject>();
        }

        public void DeleteAllFilesInBucket(){
			connection.DeleteAllFilesInBucket();
		}       
    }
}

