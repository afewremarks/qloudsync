using System;
using System.Collections.Generic;
using GreenQloud.Repository;
using System.Linq;
using System.Xml;

 namespace GreenQloud.Synchrony
{
    public class BacklogSynchronizer : XmlDocument
    {
        RemoteRepo remoteRepo;
        private static BacklogSynchronizer instance;
        private const string id = "id";
        private const string name = "name";
        private const string relativePath = "relativePath";
        private const string modificationDate = "modificationDate";
        private const string hash = "hash";

        private BacklogSynchronizer ()
        {
            this.remoteRepo = new RemoteRepo();
            if (!System.IO.File.Exists (backlog_path))
                Create();
            Load(backlog_path);
        }
        
        public static BacklogSynchronizer GetInstance(){
            if(instance == null)
                instance = new BacklogSynchronizer();
            return instance;
        }
        
        public List<File> ChangesMade = new List<File>();
        
        /*      Remote          Local       Backlog
                X                           X               (foi apagado enquanto offline, envia o remoto para a lixeira)
                                X           X               (foi apagado remotamente, envia o local para a lixeira e apaga) 
                X               X           X               (verifica sincronia, se dessincronizado, ver qual o mais atual e atualizar)
                                            X               (foi apagado local e remotamente, enquanto estava offline, nao faz nada)
                X                                           (foi criado enquanto offline, faz o download)
                                X                           (foi criado enquanto offline, faz o upload)
                X               X                           (backlog desatualizado, remoto ganha)
        */
        
        public void Synchronize ()
        {
            try {
                Logger.LogInfo("Synchronizer", "Sync starting from backlog");

                List<File> filesInLocalRepo = LocalRepo.GetFiles();
                List<RemoteFile> filesInRemoteRepo = remoteRepo.Files;
                TimeSpan diffClocks = remoteRepo.DiffClocks;
                List<File> alreadyAnalyzed = new List<File>();

                foreach (RemoteFile remoteFile in filesInRemoteRepo) {
                    if(remoteFile.IsIgnoreFile)
                        continue;
                    if(DownloadSynchronizer.GetInstance().ChangesInLastSync.Any(c=>c.File.AbsolutePath==remoteFile.AbsolutePath))
                        continue;
                    File backlogFile = Get (remoteFile.AbsolutePath);
                    bool existsInBacklog = backlogFile != null;

                    if (remoteFile.IsAFolder){
                        //se eh remoto e nao existe no local, mas existe no backlog apaga o remoto
                        //else cria o local
                        if(!System.IO.Directory.Exists(remoteFile.AbsolutePath))
                        {
                            if (existsInBacklog){
                                remoteRepo.Delete (remoteFile);
                                RemoveFileByAbsolutePath (remoteFile);
                            }
                            else{
                                System.IO.Directory.CreateDirectory (remoteFile.AbsolutePath);
                            }
                            continue;
                        }
                    }
                       

                    LocalFile localFile = new LocalFile (LocalRepo.ResolveDecodingProblem(remoteFile.AbsolutePath));
                     
                    if (existsInBacklog)
                    {
                        //localfile was deleted when offline
                        if (!System.IO.File.Exists (localFile.FullLocalName)) 
                        {
                            remoteRepo.MoveToTrash (remoteFile);
                            RemoveFileByAbsolutePath (remoteFile);
                        }
                        //verify updates when offline
                        else 
                        {
                            if (!Synchronizer.FilesIsSync (localFile, remoteFile)) {
                                
                                DateTime localLastTime = backlogFile.TimeOfLastChange;
                                
                                if (localFile.TimeOfLastChange > backlogFile.TimeOfLastChange)
                                    localLastTime = localFile.TimeOfLastChange;
                                
                                DateTime referencialClock = localLastTime.Subtract (diffClocks);
                                //local version is more recent
                                if (referencialClock.Subtract (Convert.ToDateTime (remoteFile.AsS3Object.LastModified)).TotalSeconds > -1) {
                                    remoteRepo.MoveToTrash (remoteFile);
                                    remoteRepo.Upload (localFile);
                                }
                                //remote version is more recent
                                else {
                                    remoteRepo.SendToTrash (localFile);
                                    ChangesMade.Add (remoteFile);
                                    remoteRepo.Download (remoteFile);
                                    LocalRepo.Files.Add (new LocalFile(remoteFile.AbsolutePath));
                                }
                            }
                        }                            
                        alreadyAnalyzed.Add(localFile);
                    } 
                    else 
                    {
                        if(remoteFile.IsAFolder)
                        {
                            System.IO.Directory.CreateDirectory (remoteFile.FullLocalName);
                            alreadyAnalyzed.Add (localFile);
                        }else{
                            //remote file was create when offline
                            if(System.IO.File.Exists(localFile.FullLocalName)){
                                if(Synchronizer.FilesIsSync(localFile, remoteFile)){
                                    alreadyAnalyzed.Add (localFile);
                                    AddFile(remoteFile);
                                    continue;
                                }
                            } 
                            ChangesMade.Add (remoteFile);
                            remoteRepo.Download (remoteFile);
                            LocalRepo.Files.Add (new LocalFile(remoteFile.AbsolutePath));
                            if(localFile != null)
                                alreadyAnalyzed.Add (localFile);
                        }
                        AddFile(remoteFile);
                    }
                }
                //not exists remote file
                foreach(File localFile in filesInLocalRepo)
                {
                    string resolvedPath = LocalRepo.ResolveDecodingProblem(localFile.AbsolutePath);
                    if (alreadyAnalyzed.Any (lf => LocalRepo.ResolveDecodingProblem (lf.AbsolutePath) ==  resolvedPath || LocalRepo.ResolveDecodingProblem(lf.AbsolutePath).Contains(resolvedPath+"/")))
                        continue;

                    if(!System.IO.File.Exists (localFile.FullLocalName) && !localFile.IsAFolder)
                        continue;

                    File backlogFile = Get(localFile.AbsolutePath);
                    bool existsInBacklog = backlogFile != null;
                    //remote file was deleted when offline
                    if(existsInBacklog)
                    {
                        if (localFile.IsAFolder){
                            if(System.IO.Directory.Exists(localFile.FullLocalName))
                            {
                                System.IO.Directory.Delete (localFile.FullLocalName);
                                RemoveFileByAbsolutePath (localFile);
                            }
                            continue;            
                        }

                        LocalFile lf = new LocalFile(localFile.AbsolutePath);

                        if (remoteRepo.SendToTrash (lf)) 
                        {
                            ChangesMade.Add (lf);
                            System.IO.File.Delete(localFile.FullLocalName); 
                            RemoveFileByAbsolutePath (localFile);
                        }
                    }
                    // local file was created when offline
                    else{
                       if (localFile.IsAFolder){
                
                            remoteRepo.CreateFolder(new Folder(localFile.AbsolutePath));
                            continue;            
                        }
                        remoteRepo.Upload (localFile);
                        AddFile(localFile);
                    }
                    alreadyAnalyzed.Add (localFile);
                }
            
            Logger.LogInfo("Synchronizer", "Backlog Sync is finished");
            }
            catch (Exception e){
                Logger.LogInfo("Backlog", e);
            }
        }
        
        
 
        
        string backlog_path = RuntimeSettings.BacklogFile;



        public void Create (){
            if(!System.IO.File.Exists (backlog_path))
            System.IO.File.WriteAllText (backlog_path,
                                         "<?xml version=\"1.0\" encoding=\"UTF-8\" ?>\n<files>\n</files>");
        }
        
        private void Save ()
        {
            
            if (!System.IO.File.Exists (backlog_path))
                throw new System.IO.FileNotFoundException (backlog_path + " does not exist");
            
            Save (backlog_path);
            
        }
        
        public void AddFile (File file)
        {

            if (!EditFileByName(file))
            
            {
                     
                XmlNode node_root = SelectSingleNode ("files");
                
                
                XmlNode node_id = CreateElement (id);
                XmlNode node_name = CreateElement (name);
                XmlNode node_relativePath = CreateElement (relativePath);
                XmlNode node_modificationDate = CreateElement (modificationDate);
                XmlNode node_hash = CreateElement (hash);

               
                try {
                    node_id.InnerText = (int.Parse (node_root.LastChild [id].InnerText) + 1).ToString();
                } catch {
                    node_id.InnerText = "1";
                }
                node_name.InnerText       = file.Name;
                node_relativePath.InnerText = file.RelativePath;
                node_modificationDate.InnerText        = file.TimeOfLastChange.ToString();
                if(file.IsAFolder)
                    node_hash.InnerText = "";
                else
                    node_hash.InnerText    = file.MD5Hash.ToString();
                
                XmlNode node_file = CreateNode (XmlNodeType.Element, "file", null);
                
                node_file.AppendChild (node_id);
                node_file.AppendChild (node_name);
                node_file.AppendChild (node_relativePath);
                node_file.AppendChild (node_modificationDate);
                node_file.AppendChild (node_hash);
                
                node_root.AppendChild (node_file);
            }
            Save ();
        }

        public void RemoveAllFiles ()
        {
            foreach (XmlNode node_file in SelectNodes ("files/file")) {
                SelectSingleNode ("files").RemoveChild (node_file);
            }
            
            Save ();
        }
        
        public void RemoveFileById (File file)
        {
            foreach (XmlNode node_file in SelectNodes ("files/file")) {
                if (node_file [id].InnerText == file.Id.ToString())
                    SelectSingleNode ("files").RemoveChild (node_file);
            }
            
            Save ();
        }

        public void RemoveFileByAbsolutePath (File file)
        {
            foreach (XmlNode node_file in SelectNodes ("files/file")) {
                if (node_file [name].InnerText == file.Name && node_file [relativePath].InnerText == file.RelativePath)
                    SelectSingleNode ("files").RemoveChild (node_file);
            }
            
            Save ();
        }

        public void RemoveFileByHash (File file)
        {
            foreach (XmlNode node_file in SelectNodes ("files/file")) {
                if (node_file [hash].InnerText == file.MD5Hash.ToString())
                    SelectSingleNode ("files").RemoveChild (node_file);
            }
            
            Save ();
        }

        public void EditFileById (File file){
            foreach (XmlNode node_file in SelectNodes ("/files/file")) {
                if (node_file [id].InnerText == file.Id.ToString()){
                        UpdateFile (file, node_file);
                    continue;
                }
            }    
        }

        public void EditFileByHash (File file)
        {
            foreach (XmlNode node_file in SelectNodes ("/files/file")) {
                if (node_file [hash].InnerText == file.MD5Hash.ToString()){
                    UpdateFile (file, node_file);
                    continue;
                }
            }
        }

        public bool EditFileByName(File file)
        {
            foreach (XmlNode node_file in SelectNodes ("/files/file")) {
                if (node_file [name].InnerText == file.Name.ToString() && node_file[relativePath].InnerText == file.RelativePath){
                    UpdateFile (file, node_file);
                    return true;
                }
            }
            return false;
        }

        protected void UpdateFile(File file, XmlNode node_file)
        {
            XmlNode node_name = node_file.SelectSingleNode (name);
            node_name.InnerText = file.Name;
            XmlNode node_relativePath = node_file.SelectSingleNode (relativePath);
            node_relativePath.InnerText = file.RelativePath;
            XmlNode node_modificationDate = node_file.SelectSingleNode (modificationDate);
            node_modificationDate.InnerText = file.TimeOfLastChange.ToString();
            XmlNode node_hash = node_file.SelectSingleNode (hash);
            if(file.IsAFolder)
                node_hash.InnerText = "";
            else
                node_hash.InnerText = file.MD5Hash.ToString();
            
            Save ();
        }

        protected LocalFile Get (string absolutePath)
        {
            absolutePath = LocalRepo.ResolveDecodingProblem (absolutePath);

            foreach (XmlNode node_file in SelectNodes ("/files/file")) {
                string ap = string.Format("{0}{1}",node_file[relativePath].InnerText, node_file[name].InnerText);
                ap = LocalRepo.ResolveDecodingProblem (ap);
                if (absolutePath == ap || string.Format ("{0}{1}",absolutePath,System.IO.Path.PathSeparator.ToString ()) == ap)
                {
                    LocalFile file = new LocalFile (ap);
                    file.Id  = int.Parse(node_file[id].InnerText);
                    file.MD5Hash = node_file[hash].InnerText;
                    file.TimeOfLastChange = DateTime.Parse (node_file[modificationDate].InnerText);
                    return file;
                }
            }
            return null;
        }
        
    }
}


