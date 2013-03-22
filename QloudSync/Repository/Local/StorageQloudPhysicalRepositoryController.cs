using System;
using System.IO;
using System.Collections.Generic;
using GreenQloud.Repository.Model;
using System.Linq;

namespace GreenQloud.Repository.Local
{
    public class StorageQloudPhysicalRepositoryController : PhysicalRepositoryController
    {

        public StorageQloudPhysicalRepositoryController (LogicalRepositoryController logicalController) : base (logicalController)
        {

        }

        #region Repo implementation
        public override void CreateOrUpdate (RepoObject remoteObj)
        {
            throw new NotImplementedException ();
        }

        public override List<RepoObject> Files {
            get {
                throw new NotImplementedException ();
            }
        }
        #endregion

        public override RepoObject CreateObjectInstance (string fullLocalName)
        {      
            if (logicalController.LocalRepositories == null)                
                throw new PhysicalLocalRepositoryException ("No repository registered.");
            if (!logicalController.LocalRepositories.Any (f=> fullLocalName.Contains(f.Path)))
                throw new PhysicalLocalRepositoryException ("File is not in a repository registered.");
            
            LocalRepository repo = logicalController.LocalRepositories.First (f=> fullLocalName.Contains(f.Path));
            
            RepoObject repoObj = new RepoObject();
            
            if (File.Exists (fullLocalName)) {
                FileInfo file = new FileInfo (fullLocalName);
                repoObj.TimeOfLastChange = file.LastWriteTime;
                repoObj.Size = file.Length;
            } else if (Directory.Exists (fullLocalName)) {
                repoObj.IsAFolder = true;
            } else if (fullLocalName.EndsWith ("/")) {
                repoObj.IsAFolder = true;
            }
            
            repoObj.Repo = repo;
            if (repoObj.IsAFolder)
                repoObj.Name = Path.GetDirectoryName (fullLocalName);
            else
                repoObj.Name = Path.GetFileName (fullLocalName);
            repoObj.RelativePath = fullLocalName.Replace (repo.Path,string.Empty).Replace (repoObj.Name, string.Empty);
            
            return repoObj;
        }

        public override bool Exists (RepoObject remoteObj)
        {
            if (File.Exists (remoteObj.FullLocalName))
                return true;
            if (Directory.Exists (remoteObj.FullLocalName))
                return true;
            return false;
        }

        public override List<string> FilesNames {
            get{
                try {
                //TODO PEGAR OS ARQUIVOS DE TODOS OS OBJETOS DE REPOSITORIO CADASTRADOS NO BANCO DE DADOS
                    System.IO.DirectoryInfo dir = new System.IO.DirectoryInfo (RuntimeSettings.HomePath);
                    List<string> list = new List<string> ();
                    if (dir.Exists) {
                        foreach (FileInfo fileInfo in dir.GetFiles ("*", System.IO.SearchOption.AllDirectories))
                            list.Add (fileInfo.FullName);

                        foreach (DirectoryInfo fileInfo in dir.GetDirectories ("*", System.IO.SearchOption.AllDirectories))
                            list.Add (fileInfo.FullName+"/");
                    }
                    return list;
                } catch (Exception e) {
                    Logger.LogInfo ("ERROR", "FAIL TO LOAD LOCAL FILES");
                    Logger.LogInfo ("ERROR", e);
                    return null;
                }
            }
        }

        public void Create (RepoObject repoObj)
        {
            throw new NotImplementedException ();
        }

        public override void Delete (RepoObject repoObj)
        {
            /*if(Directory.Exists (sqObj.FullLocalName)){
                try{
                    List<RepoObject> sqObjectsInFolder = GetSQObjects(sqObj.FullLocalName);
                    string path = Path.Combine(RuntimeSettings.TrashPath, sqObj.Name);
                    
                    if(Directory.Exists(path)){
                        Directory.Move (path, path+" "+DateTime.Now.ToString("dd.mm.ss tt"));
                    }else{
                        Directory.Move (sqObj.FullLocalName, path);
                        RemoveFromLists (sqObj);
                    }
                    
                    foreach (RepoObject s in sqObjectsInFolder)
                    {
                        RemoveFromLists (s);
                    }
                }
                catch {
                    Logger.LogInfo ("Error", string.Format("Fail to delete folder \"{0}\" in local repo.", sqObj.FullLocalName));
                }
            }
            else if (File.Exists (sqObj.FullLocalName)){
                try{
                    string path = Path.Combine(RuntimeSettings.TrashPath, sqObj.Name);
                    
                    if(File.Exists (path)){
                        string newpath =  path+" "+DateTime.Now.ToString ("dd.mm.ss tt");
                        File.Move (path, newpath);
                    }
                    
                    CreatePath (path);
                    File.Move(sqObj.FullLocalName, path);
                    RemoveFromLists (sqObj);
                }
                catch (Exception e){
                    Logger.LogInfo ("Error", string.Format("Fail to delete file \"{0}\" in local repo.", sqObj.FullLocalName));
                    Logger.LogInfo ("Error", e);
                }
            }*/
        }
    }
}

