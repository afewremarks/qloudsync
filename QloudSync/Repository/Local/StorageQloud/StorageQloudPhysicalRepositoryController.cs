using System;
using System.IO;
using System.Collections.Generic;
using GreenQloud.Model;
using System.Linq;
using GreenQloud.Persistence.SQLite;
using GreenQloud.Synchrony;
using System.Threading;

namespace GreenQloud.Repository.Local
{
    //TODO Refactor... only create folders if dir doesnt exists
    public class StorageQloudPhysicalRepositoryController : PhysicalRepositoryController
    {
        SQLiteRepositoryDAO repoDAO = new SQLiteRepositoryDAO();
        public StorageQloudPhysicalRepositoryController () : base ()
        {

        }

        //TODO understand and refactor
        public override bool IsSync(RepositoryItem item){
            if(item.IsAFolder)
                return true;
            if(item.RemoteETAG == string.Empty)
                throw new Exception ("Remote Hash not exists");
            //item.LocalMD5Hash = CalculateMD5Hash(item);
            //return item.RemoteMD5Hash == item.LocalMD5Hash;
            return true;
        }
        


        public override bool Exists (RepositoryItem item)
        {
            return Exists(item.FullLocalName);
        }
        public override bool Exists (string path)
        {
            return File.Exists (path) || Directory.Exists (path);
        }

        public override void Delete (RepositoryItem item)
        {
            if(Directory.Exists(item.FullLocalName)){
                var dir = new DirectoryInfo(item.FullLocalName);
                DeleteDir (dir);
            }
            if(File.Exists(item.FullLocalName)){
                DeleteFile(item.FullLocalName);
            }
        }

        public override void Copy (RepositoryItem item)
        {
            string path = RuntimeSettings.HomePath +Path.DirectorySeparatorChar+ item.ResultObjectRelativePath;

            if (!Exists(item))
                return;
            if (item.IsAFolder)
            {
                if(!Directory.Exists(path)){
                    var dir = new DirectoryInfo(item.FullLocalName);
                    CopyDir (dir, path);
                }
            }else{
                CopyFile(item.FullLocalName, path);
            }
        }

        public override void Move (RepositoryItem item)
        {
            string path = RuntimeSettings.HomePath +Path.VolumeSeparatorChar+ item.ResultObjectRelativePath;

            if (!Exists(item))
                return;
            if (item.IsAFolder)
            {
                MoveDir(item.FullLocalName, path);
            }else{
                MoveFile(item.FullLocalName, path);
            }
        }

        public override RepositoryItem GetCopy (RepositoryItem item)
        {
            if (item.IsAFolder)
                return null;

            if (Items.Any (i=> i.FullLocalName != item.FullLocalName && i.RemoteETAG == item.RemoteETAG))
            {
                return Items.First (i=> i.FullLocalName != item.FullLocalName && i.RemoteETAG == item.RemoteETAG);
            }
            else
                return null;
        }

        public override List<RepositoryItem> Items {
            get {
                List<RepositoryItem> items = new List<RepositoryItem>();
                foreach (LocalRepository repo in repoDAO.All)
                {
                    items.AddRange (GetItens (repo));
                }
                return items;
            }
        }

        public List<RepositoryItem> GetItens (LocalRepository repo)
        {              
            System.IO.DirectoryInfo dir = new System.IO.DirectoryInfo (repo.Path);
            List<RepositoryItem> list = new List<RepositoryItem>();
            if(dir.Exists){
                foreach (FileInfo fileInfo in dir.GetFiles ("*", System.IO.SearchOption.AllDirectories).ToList ()) {
                    RepositoryItem localFile = RepositoryItem.CreateInstance (repoDAO.GetRepositoryByItemFullName(fileInfo.FullName), fileInfo.FullName, false, fileInfo.Length, fileInfo.LastWriteTime.ToString());
                    if(!localFile.IsIgnoreFile)
                        list.Add (localFile);
                }
                
                foreach (DirectoryInfo fileInfo in dir.GetDirectories ("*", System.IO.SearchOption.AllDirectories).ToList ()){
                    if (fileInfo.Name.Contains ("untitled folder")) 
                        continue;
                    RepositoryItem localFile = RepositoryItem.CreateInstance (repoDAO.GetRepositoryByItemFullName (fileInfo.FullName), fileInfo.FullName, true, 0, GlobalDateTime.NowUniversalString);
                    list.Add (localFile);
                }
            }
            return list;
        }

        public override RepositoryItem CreateItemInstance (string fullLocalName)
        {
            FileInfo file = new FileInfo (fullLocalName);
            if (file.Exists){
                return RepositoryItem.CreateInstance (repoDAO.GetRepositoryByItemFullName (fullLocalName), fullLocalName, false, file.Length, file.LastWriteTime.ToString());
            }
            throw new NotImplementedException ();
        }

        public override List<RepositoryItem> GetSubRepositoyItems (RepositoryItem item)
        {
            List<RepositoryItem> list = new List<RepositoryItem>();
            if (item.IsAFolder){
                if (Directory.Exists (item.FullLocalName)){
                    LocalRepository repo = new LocalRepository (item.FullLocalName);
                    list = GetItens (repo);
                }
            }
            return list;
        }

        public string CalculateMD5Hash (RepositoryItem item)
        {
            var md5hash = new Crypto().md5hash(item.FullLocalName);
            return md5hash;
        }




        #region Core Management

        public void CreateFolder(RepositoryItem item){
            if (!Exists(item)){
                CreatePath (item.FullLocalName);

                BlockWatcher (item.FullLocalName);
                DirectoryInfo di = Directory.CreateDirectory (item.FullLocalName);
                UnblockWatcher (item.FullLocalName);   
            }
        }
        
        void CreatePath (string path)
        {
            string parent = path.Substring (0,path.LastIndexOf("/"));
            
            if (parent == string.Empty)
                return;
            
            CreatePath(parent);

            if (!path.EndsWith (Path.VolumeSeparatorChar.ToString()))
                path += Path.VolumeSeparatorChar;

            if(!Directory.Exists(path)){
                BlockWatcher (path);
                DirectoryInfo di = Directory.CreateDirectory(path);
                UnblockWatcher (path);
            }
        }

        private void MoveFile(string path, string toPath){
            BlockWatcher (path);
            BlockWatcher (toPath);
            File.Move(path, toPath);
            UnblockWatcher (path);
            UnblockWatcher (toPath);
        }
        private void MoveDir(string path, string toPath){
            BlockWatcher (path);
            BlockWatcher (toPath);
            Directory.Move(path, toPath);
            UnblockWatcher (path);
            UnblockWatcher (toPath);
        }

        private void CopyFile(string path, string toPath){
            BlockWatcher (toPath);
            File.Copy(path, toPath);
            UnblockWatcher (toPath);
        }
        private void CopyDir(DirectoryInfo dir, string to){
            if(!Directory.Exists(to)){
                BlockWatcher (to);
                DirectoryInfo di = Directory.CreateDirectory (to);
                UnblockWatcher (to);
            }
            dir.GetDirectories().ToList().ForEach(directory=>CopyDir(directory, to+"/"+directory.Name));

            List<FileInfo> files = dir.GetFiles ("*", SearchOption.AllDirectories).ToList ();
            foreach (FileInfo file in files) {
                CopyFile(file.FullName, to+"/"+file.Name);
            }
        }

        private void DeleteFile(string path){
            BlockWatcher (path);
            File.Delete (path);
            UnblockWatcher (path);
        }
        private void DeleteDir(DirectoryInfo dir){
            dir.GetDirectories().ToList().ForEach(directory=>DeleteDir(directory));

            List<FileInfo> files = dir.GetFiles ("*", SearchOption.AllDirectories).ToList ();
            foreach (FileInfo file in files) {
                DeleteFile (file.FullName);
            }

            BlockWatcher (dir.FullName);
            Directory.Delete (dir.FullName);
            UnblockWatcher (dir.FullName);
        }

        private static void BlockWatcher (string path)
        {
            QloudSyncFileSystemWatcher watcher = StorageQloudLocalEventsSynchronizer.GetInstance ().GetWatcher (path);
            if(watcher != null){
                watcher.Block (path);
            }

        }

        private static void UnblockWatcher (string path)
        {
            QloudSyncFileSystemWatcher watcher = StorageQloudLocalEventsSynchronizer.GetInstance ().GetWatcher (path);
            if(watcher != null){
                Thread.Sleep (500);
                watcher.Unblock (path);
            }

        }
        #endregion
    }
}

