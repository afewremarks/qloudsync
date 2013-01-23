using System;
using System.Collections.Generic;
using QloudSync.Repository;
using System.Linq;

 namespace QloudSync.Synchrony
{
    public class BacklogSynchronizer : Synchronizer
    {
        private static BacklogSynchronizer instance;
        
        private BacklogSynchronizer ()
        {
        }
        
        public static BacklogSynchronizer GetInstance()
        {
            if (instance == null)
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
        
        public override bool Synchronize ()
        {
            Synchronized = false;
            try {
                Logger.LogInfo("Synchronizer", "Sync starting from backlog");
                List<string[]> filesInBackLog = Read ();
                if (filesInBackLog != null)
                {
                    List<File> filesInLocalRepo = LocalRepo.Files;
                    List<RemoteFile> filesInRemoteRepo = RemoteRepo.Files;
                    TimeSpan diffClocks = RemoteRepo.DiffClocks;
                    List<File> alreadyAnalyzed = new List<File>();
                    
                    foreach (RemoteFile remoteFile in filesInRemoteRepo) {
                        if(remoteFile.IsIgnoreFile)
                            continue;
                        bool backlogFile = false;
                        string date = "";
                        if(filesInBackLog != null)
                        {
                            if(filesInBackLog.Count > 0)
                            {
                                if (filesInBackLog.Where (fibl => LocalRepo.ResolveDecodingProblem(string.Format("{0}{1}",fibl [4], fibl [3])) == LocalRepo.ResolveDecodingProblem(remoteFile.AbsolutePath)).Any()){
                                    date = filesInBackLog.Where (fibl => LocalRepo.ResolveDecodingProblem(string.Format("{0}{1}",fibl [4], fibl [3])) == LocalRepo.ResolveDecodingProblem(remoteFile.AbsolutePath)).First() [2];
                                    backlogFile = true;
                                }
                            }
                        }
                        
                        LocalFile localFile = null;
                        if (remoteFile.IsAFolder){
                            System.IO.DirectoryInfo dir = new System.IO.DirectoryInfo(remoteFile.FullLocalName);
                            if(!dir.Exists){
                                ChangesMade.Add (remoteFile);
                                dir.Create();
                            }
                            continue;
                        }
                        if (filesInLocalRepo.Where (filr => LocalRepo.ResolveDecodingProblem(filr.AbsolutePath) == LocalRepo.ResolveDecodingProblem(remoteFile.AbsolutePath)).Any ())
                        {
                            File temp = filesInLocalRepo.Where (filr => LocalRepo.ResolveDecodingProblem(filr.AbsolutePath) == LocalRepo.ResolveDecodingProblem(remoteFile.AbsolutePath)).First ();
                            localFile = new LocalFile(LocalRepo.ResolveDecodingProblem(temp.AbsolutePath), temp.TimeOfLastChange);
                        }
                        
                        if (localFile == null && backlogFile) 
                        {
                            if(RemoteRepo.SendToTrash (remoteFile))
                            {
                                ChangesMade.Add (remoteFile);
                                RemoteRepo.Delete (remoteFile);
                            }
                        } 
                        else if (localFile != null && backlogFile) 
                        {
                            if (!FilesIsSync (localFile, remoteFile)) {
                                
                                DateTime localLastTime = Convert.ToDateTime (date);
                                
                                if (localFile.TimeOfLastChange > localLastTime)
                                    localLastTime = localFile.TimeOfLastChange;
                                
                                DateTime referencialClock = localLastTime.Subtract (diffClocks);
                                if (referencialClock.Subtract (Convert.ToDateTime (remoteFile.AsS3Object.LastModified)).TotalSeconds > 0) {
                                    Console.WriteLine ("2");
                                    if(RemoteRepo.SendToTrash (remoteFile))
                                        RemoteRepo.Upload (localFile);
                                } else {
                                    Console.WriteLine ("3");
                                    if(RemoteRepo.SendToTrash (localFile))
                                    {
                                        ChangesMade.Add (remoteFile);
                                        RemoteRepo.Download (remoteFile);
                                        LocalRepo.Files.Add (new LocalFile(remoteFile.AbsolutePath));
                                    }
                                }
                            }
                            alreadyAnalyzed.Add(localFile);
                        } 
                        else if (!backlogFile) 
                        {
                            Console.WriteLine ("4");
                            ChangesMade.Add (remoteFile);
                            RemoteRepo.Download (remoteFile);
                            LocalRepo.Files.Add (new LocalFile(remoteFile.AbsolutePath));
                            if(localFile != null)
                                alreadyAnalyzed.Add (localFile);
                        }
                    }

                    foreach(File localFile in filesInLocalRepo){
                        string localAbsolutePath = LocalRepo.ResolveDecodingProblem (localFile.AbsolutePath);
                        if (alreadyAnalyzed.Where (lf => LocalRepo.ResolveDecodingProblem (lf.AbsolutePath) == localAbsolutePath).Any())
                            continue;
                        if (localFile.IsAFolder)
                        {
                            continue;
                        }
                        bool backlogFile = false;
                        if(filesInBackLog!=null)
                            if(filesInBackLog.Count>0)
                                backlogFile = filesInBackLog.Where (fibl => LocalRepo.ResolveDecodingProblem (string.Format("{0}{1}",fibl [4] , fibl [3])) == localAbsolutePath).Any ();
                        
                        
                        if(backlogFile)
                        {
                            Console.WriteLine ("5");
                            LocalFile lf = new LocalFile(localAbsolutePath);
                            if (RemoteRepo.SendToTrash (lf)) 
                            {
                                ChangesMade.Add (lf);
                                System.IO.File.Delete(localFile.FullLocalName);   
                            }
                            
                        }
                        else{
                            Console.WriteLine ("6");
                            RemoteRepo.Upload (localFile);
                        }
                        alreadyAnalyzed.Add (localFile);
                    }
                }
                Write();
                Repo.LastSyncTime = DateTime.Now;
                Logger.LogInfo("Synchronizer", "Backlog Sync is finished");
                Synchronized = true;
                return true;
            } catch (Exception e){
                Logger.LogInfo("Backlog", e);
                return false;
            }
        }
        
        
        
        public void Write ()
        {
            
            try {
                
                if (System.IO.File.Exists(QloudSync.Util.Constant.BACKLOG_FILE))
                    System.IO.File.Delete (QloudSync.Util.Constant.BACKLOG_FILE);
                
                
                int i = 1;
                
                using (System.IO.FileStream fs = System.IO.File.Create(QloudSync.Util.Constant.BACKLOG_FILE))
                {
                    
                    foreach (QloudSync.Repository.File f in LocalRepo.Files) {
                        if (!f.IsIgnoreFile && !f.Deleted) {
                            string text = String.Format ("{0}\t{1}\t{2}\t{3}\t{4}\n", i, f.MD5Hash, f.TimeOfLastChange, f.Name, f.RelativePath);
                            byte[] info = new System.Text.UTF8Encoding(true).GetBytes(text);
                            fs.Write(info, 0, info.Length);
                            i++;
                            
                        }
                    }
                    
                    
                }
                
                
            } catch (InvalidOperationException) {
                Logger.LogInfo ("Debug", "Collection was modified; Write");
                //Write ();
            } catch (System.IO.IOException e) {
                Logger.LogInfo ("Debug", e);
                //Write ();
            }
            
            
        }
        
        public List<string[]> Read ()
        {
            try {
                System.IO.FileInfo backlogFile = new System.IO.FileInfo(QloudSync.Util.Constant.BACKLOG_FILE);
                if (!backlogFile.Exists){
                    backlogFile.Create();
                    return new List<string[]>();
                }
                
                System.IO.StreamReader sr = new System.IO.StreamReader (QloudSync.Util.Constant.BACKLOG_FILE);
                Repo.LastSyncTime = backlogFile.LastWriteTime;
                List<string[]> filesInBackLog = new List<string[]>();
                int i = 1;
                while (!sr.EndOfStream) {
                    string [] info = BreakLine (sr.ReadLine (), 5);
                    filesInBackLog.Add (info);
                    i++;
                }
                sr.Dispose();
                sr.Close ();
                return filesInBackLog;
            } catch (Exception e) {
                Logger.LogInfo("Sync", e);
                return null;
            }
        }
        
        
        private string [] BreakLine (string line, int columns)
        {
            try {
                if (line=="")
                    return null;
                
                string[] breakLine = new string[columns];
                
                int last_index = line.LastIndexOf ("\t");
                int first_index = line.IndexOf ("\t");
                
                int init_index = 0;
                int end_index = first_index;
                
                int i = 0;
                while (init_index != last_index) {
                    int size = end_index - init_index;
                    breakLine [i] = line.Substring (init_index, size);
                    init_index = end_index + 1;
                    if (init_index > line.Length)
                        break;
                    end_index = line.IndexOf ("\t", init_index);
                    if (end_index == -1)
                        end_index = line.Length;
                    i++;
                }
                return breakLine;
            } catch (Exception e){
                Logger.LogInfo("Sync", e);
                return null;
            }
        }
    }
}

