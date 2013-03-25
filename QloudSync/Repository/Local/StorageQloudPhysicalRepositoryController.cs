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

        public override bool Exists (RepositoryItem item)
        {
            return File.Exists (item.FullLocalName) || Directory.Exists (item.FullLocalName);
        }

        public override RepositoryItem CreateObjectInstance (string itemFullName)
        {
            RepositoryItem item = new RepositoryItem ();
            item.Repository = repoDAO.GetRepositoryByItemFullName(itemFullName);
            if (File.Exists(itemFullName)){
                item.Name = Path.GetFileName(itemFullName);
                item.IsAFolder = false;
            }
            else if(Directory.Exists (itemFullName)){
                item.Name = Path.GetDirectoryName(itemFullName)+"/";
                item.IsAFolder = false;
            }
            item.RelativePath = itemFullName.Replace (item.Repository.Path, string.Empty).Replace(item.Name, string.Empty);
            return item;
        }

        public RepositoryItem CreateObjectInstance (string itemFullName, string name, LocalRepository repo, bool isAFolder)
        {
            RepositoryItem item = new RepositoryItem ();
            item.Repository = repo;
            item.Name = name;
            item.IsAFolder = isAFolder;
            item.RelativePath = itemFullName.Replace (item.Repository.Path, string.Empty).Replace(item.Name, string.Empty);
            return item;
        }

        public override void Delete (RepositoryItem item)
        {
            string path = Path.Combine(RuntimeSettings.TrashPath, item.Name);

            if (item.IsAFolder)
            {
                if(Directory.Exists(path)){
                    Directory.Move (path, path+" "+DateTime.Now.ToString("dd.mm.ss tt"));
                }else{
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
        }

        public override RepositoryItem GetCopy (RepositoryItem item)
        {
            if (item.IsAFolder)
                return null;

            if (Items.Any (i=> i.FullLocalName != item.FullLocalName && i.MD5Hash == item.MD5Hash))
            {
                return Items.First (i=> i.FullLocalName != item.FullLocalName && i.MD5Hash == item.MD5Hash);
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
                        RepositoryItem localFile = CreateObjectInstance(fileInfo.FullName, fileInfo.Name, repo, false);
                        if(!localFile.IsIgnoreFile)
                            list.Add (localFile);
                    }
                    
                    foreach (DirectoryInfo fileInfo in dir.GetDirectories ("*", System.IO.SearchOption.AllDirectories).ToList ())
                        list.Add (CreateObjectInstance(fileInfo.FullName, fileInfo.Name, repo, true));
                }
                return list;
            } catch (Exception e) {
                Logger.LogInfo ("Error", "Fail to load local files");
                Logger.LogInfo("Error", e);
                return null;
            }
        }
        #endregion

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
    }
}

