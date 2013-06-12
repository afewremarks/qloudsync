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
    public class StorageQloudPhysicalRepositoryController : AbstractController, IPhysicalRepositoryController
    {
        SQLiteRepositoryDAO repoDAO = new SQLiteRepositoryDAO();
        public StorageQloudPhysicalRepositoryController () : base ()
        {

        }

        //TODO understand and refactor
        public bool IsSync(RepositoryItem item){
            //if(item.IsFolder)
            //    return true;
            //if(item.ETag == string.Empty)
            //    throw new Exception ("Remote Hash not exists");
            //item.LocalMD5Hash = CalculateMD5Hash(item);
            //return item.RemoteMD5Hash == item.LocalMD5Hash;
            return true;
        }
        


        public bool Exists (RepositoryItem item)
        {
            return Exists(item.LocalAbsolutePath);
        }
        public bool Exists (string path)
        {
            return File.Exists (path) || Directory.Exists (path);
        }

        public void Delete (RepositoryItem item)
        {
            if(Directory.Exists(item.LocalAbsolutePath)){
                var dir = new DirectoryInfo(item.LocalAbsolutePath);
                DeleteDir (dir);
            }
            if(File.Exists(item.LocalAbsolutePath)){
                DeleteFile(item.LocalAbsolutePath);
            }
        }

        public void Copy (RepositoryItem item)
        {
            string path = item.ResultItem.LocalAbsolutePath;

            if (!Exists(item))
                return;
            if (item.IsFolder)
            {
                if(!Directory.Exists(path)){
                    var dir = new DirectoryInfo(item.LocalAbsolutePath);
                    CopyDir (dir, path);
                }
            }else{
                CopyFile(item.LocalAbsolutePath, path);
            }
        }

        public void Move (RepositoryItem item)
        {
            string path = item.ResultItem.LocalAbsolutePath;

            if (!Exists(item))
                return;
            if (item.IsFolder)
            {
                MoveDir(item.LocalAbsolutePath, path);
            }else{
                MoveFile(item.LocalAbsolutePath, path);
            }
        }

        public RepositoryItem GetCopy (RepositoryItem item)
        {
            if (item.IsFolder)
                return null;

            if (Items.Any (i=> i.LocalAbsolutePath != item.LocalAbsolutePath && i.ETag == item.LocalETag))
            {
                return Items.First (i=> i.LocalAbsolutePath != item.LocalAbsolutePath && i.ETag == item.LocalETag);
            }
            else
                return null;
        }

        public List<RepositoryItem> Items {
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
                    string key = fileInfo.FullName.Substring(repo.Path.Length);
                    RepositoryItem localFile = RepositoryItem.CreateInstance (repo, false, key);
                    list.Add (localFile);
                }
                
                foreach (DirectoryInfo fileInfo in dir.GetDirectories ("*", System.IO.SearchOption.AllDirectories).ToList ()){
                    string key = fileInfo.FullName.Substring(repo.Path.Length);
                    if (!key.EndsWith (Path.DirectorySeparatorChar.ToString()))
                        key += Path.DirectorySeparatorChar;
                    RepositoryItem localFile = RepositoryItem.CreateInstance (repo, true, key);
                    list.Add (localFile);
                }
            }
            return list;
        }

        public RepositoryItem CreateItemInstance (string fullLocalName)
        {
            FileInfo file = new FileInfo (fullLocalName);
            if (file.Exists){
                LocalRepository repo = repoDAO.GetRepositoryByItemFullName (fullLocalName);
                string key = fullLocalName.Substring (repo.Path.Length);
                return RepositoryItem.CreateInstance(repo,false, key);
            }
            return null;
        }

        public List<RepositoryItem> GetSubRepositoyItems (RepositoryItem item)
        {
            List<RepositoryItem> list = new List<RepositoryItem>();
            if (item.IsFolder){
                if (Directory.Exists (item.LocalAbsolutePath)){
                    LocalRepository repo = item.Repository;
                    list = GetItens (repo);
                }
            }
            return list;
        }

        public string CalculateMD5Hash (RepositoryItem item)
        {
            var md5hash = new Crypto().md5hash(item.LocalAbsolutePath);
            return md5hash;
        }




        #region Core Management

        public void CreateFolder(RepositoryItem item){
            if (!Exists(item)){
                CreatePath (item.LocalAbsolutePath);

                BlockWatcher (item.LocalAbsolutePath);
                Directory.CreateDirectory (item.LocalAbsolutePath);
                UnblockWatcher (item.LocalAbsolutePath);   
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
        #endregion
    }
}

