using System;
using System.IO;
using System.Collections.Generic;
using GreenQloud.Model;
using System.Linq;
using GreenQloud.Persistence.SQLite;

namespace GreenQloud.Repository.Local
{
    public class StorageQloudPhysicalRepositoryController : PhysicalRepositoryController
    {
        SQLiteRepositoryDAO repoDAO = new SQLiteRepositoryDAO();
        public StorageQloudPhysicalRepositoryController () : base ()
        {

        }

        #region implemented abstract members of PhysicalRepositoryController

        public override bool IsSync(RepositoryItem item){
            if(item.IsAFolder)
                return true;
            if(item.RemoteMD5Hash == string.Empty)
                throw new Exception ("Remote Hash not exists");
            item.LocalMD5Hash = CalculateMD5Hash(item);
            return item.RemoteMD5Hash == item.LocalMD5Hash;
        }

        public override bool Exists (RepositoryItem item)
        {
            bool exists = File.Exists (item.FullLocalName) || Directory.Exists (item.FullLocalName);

            return exists;
        }

        public override void Delete (RepositoryItem item)
        {
            try{
            string path = Path.Combine(RuntimeSettings.TrashPath, item.Name);
            if (!Exists(item))
                    return;
            if (item.IsAFolder)
            {
                if(Directory.Exists(path)){
                    if (path.EndsWith ("/"))
                        path = path.Substring (0, path.Length-1);
                    Directory.Move (path, path+" "+DateTime.Now.ToString("dd.mm.ss tt"));
                }else{
                    CreatePath (path);
                    Directory.Move (item.FullLocalName, path);                   
                }
            }
            else{
                if(File.Exists (path)){
                    string newpath =  path+" "+DateTime.Now.ToString ("dd.mm.ss tt");
                    File.Move (path, newpath);
                }
                else{
                    CreatePath (path);
                    File.Move(item.FullLocalName, path);
                }
            }
            }catch (IOException ioex){

            }
            catch (Exception e){
                Console.WriteLine (e.GetType() +" "+ e.Message);
            }
        }

        public override RepositoryItem GetCopy (RepositoryItem item)
        {
            if (item.IsAFolder)
                return null;

            if (Items.Any (i=> i.FullLocalName != item.FullLocalName && i.RemoteMD5Hash == item.LocalMD5Hash))
            {
                return Items.First (i=> i.FullLocalName != item.FullLocalName && i.RemoteMD5Hash == item.LocalMD5Hash);
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
            try {                
                System.IO.DirectoryInfo dir = new System.IO.DirectoryInfo (repo.Path);
                List<RepositoryItem> list = new List<RepositoryItem>();
                if(dir.Exists){
                    foreach (FileInfo fileInfo in dir.GetFiles ("*", System.IO.SearchOption.AllDirectories).ToList ()) {
                        RepositoryItem localFile = RepositoryItem.CreateInstance (repoDAO.GetRepositoryByItemFullName(fileInfo.FullName), fileInfo.FullName, false, fileInfo.Length, fileInfo.LastWriteTime);
                        if(!localFile.IsIgnoreFile)
                            list.Add (localFile);
                    }
                    
                    foreach (DirectoryInfo fileInfo in dir.GetDirectories ("*", System.IO.SearchOption.AllDirectories).ToList ()){
                        if (fileInfo.Name.Contains ("untitled folder")) 
                            continue;
                        RepositoryItem localFile = RepositoryItem.CreateInstance (repoDAO.GetRepositoryByItemFullName (fileInfo.FullName), fileInfo.FullName, true, 0, DateTime.Now);
                        list.Add (localFile);
                    }
                }
                return list;
            } catch (Exception e) {
                Logger.LogInfo ("Error", "Fail to load local files");
                Logger.LogInfo("Error", e);
                return null;
            }
        }

        public override RepositoryItem CreateItemInstance (string fullLocalName)
        {
            FileInfo file = new FileInfo (fullLocalName);
            if (file.Exists){
                return RepositoryItem.CreateInstance (repoDAO.GetRepositoryByItemFullName (fullLocalName), fullLocalName, false, file.Length, file.LastWriteTime);
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

        #endregion

        public void CreateFolder(RepositoryItem item){
            if (!Exists(item)){
                CreatePath (item.FullLocalName);
                Directory.CreateDirectory (item.FullLocalName);
            }
        }

        void CreatePath (string path)
        {
            string parent = path.Substring (0,path.LastIndexOf("/"));
            
            if (path.EndsWith ("/")) {
                parent = parent.Substring (0, parent.LastIndexOf("/"));
            }
            if (parent == string.Empty)
                return;
            
            CreatePath(parent);
            
            if(!Directory.Exists(parent))
                Directory.CreateDirectory(parent);
        }

        public string CalculateMD5Hash (RepositoryItem item)
        {
            string md5hash;
            try {       
                FileStream fs = System.IO.File.Open (item.FullLocalName, FileMode.Open);
                System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create ();
                md5hash = BitConverter.ToString (md5.ComputeHash (fs)).Replace (@"-", @"").ToLower ();
                fs.Close ();
            } catch{
                md5hash = string.Empty;
            }
            return md5hash;
        }
    }
}

