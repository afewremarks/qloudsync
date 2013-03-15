using System;
using System.Collections.Generic;
using GreenQloud.Repository;
using System.Linq;
using System.Xml;
using System.Threading;

 namespace GreenQloud.Synchrony
{
    public class BacklogSynchronizer : Synchronizer
    {
        private static BacklogSynchronizer instance;
        private BacklogDocument backlogDocument;
        Thread threadSynchronize;
       

        private BacklogSynchronizer ()
        {
            Status = SyncStatus.IDLE;
            this.remoteRepo = new StorageQloudRepo();
            this.backlogDocument = new BacklogDocument();
            threadSynchronize = new Thread(Synchronize);

        }
        
        public static BacklogSynchronizer GetInstance(){
            if(instance == null)
                instance = new BacklogSynchronizer();
            return instance;
        }
        
        public List<StorageQloudObject> ChangesMade = new List<StorageQloudObject>();



        public override void Start ()
        {
            threadSynchronize.Start();
        }

        public override void Pause ()
        {
            threadSynchronize.Join();
        }

        public override void Stop ()
        {

        }


        /*      Remote          Local       Backlog
                X                           X               (foi apagado enquanto offline, envia o remoto para a lixeira)
                                X           X               (foi apagado remotamente, envia o local para a lixeira e apaga) 
                X               X           X               (verifica sincronia, se dessincronizado, ver qual o mais atual e atualizar)
                                            X               (foi apagado local e remotamente, enquanto estava offline, nao faz nada)
                X                                           (foi criado enquanto offline, faz o download)
                                X                           (foi criado enquanto offline, faz o upload)
                X               X                           (backlog desatualizado, remoto ganha)
        */
        
        public override void Synchronize ()
        {
            try {
                ChangesInLastSync = new List<Change>();
                Logger.LogInfo ("Synchronizer", "Sync starting from backlog");

                List<StorageQloudObject> filesInLocalRepo = LocalRepo.GetSQObjects (RuntimeSettings.HomePath);

                List<StorageQloudObject> filesInRemoteRepo = remoteRepo.Files;
                filesInRemoteRepo.AddRange(remoteRepo.Folders);

                TimeSpan diffClocks = remoteRepo.DiffClocks;
                List<StorageQloudObject> alreadyAnalyzed = new List<StorageQloudObject> ();

                foreach (StorageQloudObject remoteFile in filesInRemoteRepo) {
                    if (remoteFile.IsIgnoreFile)
                        continue;
                    if (LocalSynchronizer.GetInstance ().ChangesInLastSync.Any (c => c.File.AbsolutePath == remoteFile.AbsolutePath))
                        continue;


                    StorageQloudObject backlogFile = backlogDocument.Get (remoteFile.AbsolutePath);
                    bool existsInBacklog = backlogFile != null;

                    if (remoteFile.IsAFolder) {
                        //se eh remoto e nao existe no local, mas existe no backlog apaga o remoto
                        //else cria o local
                        if (!System.IO.Directory.Exists (remoteFile.FullLocalName)) {
                            if (existsInBacklog) {
                                ChangesInLastSync.Add(new Change(remoteFile, System.IO.WatcherChangeTypes.Deleted));
                                remoteRepo.Delete (remoteFile);
                                RemoveFileByAbsolutePath (remoteFile);
                            } else {
                                ChangesInLastSync.Add(new Change(remoteFile, System.IO.WatcherChangeTypes.Created));

                                System.IO.Directory.CreateDirectory (remoteFile.FullLocalName);
                                Logger.LogInfo ("BacklogSynchronizer","Creating folder "+remoteFile.FullLocalName);
                                AddFile (remoteFile);
                            }


                        }
                        alreadyAnalyzed.Add(remoteFile);
                        continue;
                    }
                       

                    StorageQloudObject localFile = new StorageQloudObject (LocalRepo.ResolveDecodingProblem (remoteFile.AbsolutePath));
                     
                    if (existsInBacklog) {
                        //localfile was deleted when offline
                        if (!System.IO.File.Exists (localFile.FullLocalName)) {
                            remoteRepo.MoveFileToTrash(remoteFile);
                            RemoveFileByAbsolutePath (remoteFile);
                        }
                        //verify updates when offline
                        else {
                            if (!remoteFile.IsSync) {
                                
                                DateTime localLastTime = backlogFile.TimeOfLastChange;
                                
                                if (localFile.TimeOfLastChange > backlogFile.TimeOfLastChange)
                                    localLastTime = localFile.TimeOfLastChange;
                                
                                DateTime referencialClock = localLastTime.Subtract (diffClocks);
                                //local version is more recent
                                if (referencialClock.Subtract (Convert.ToDateTime (remoteFile.AsS3Object.LastModified)).TotalSeconds > -1) {
                                    remoteRepo.MoveFileToTrash (remoteFile);
                                    remoteRepo.Upload (localFile);
                                }
                                //remote version is more recent
                                else {
                                    remoteRepo.SendLocalVersionToTrash (localFile);
                                    ChangesMade.Add (remoteFile);
                                    ChangesInLastSync.Add(new Change(remoteFile, System.IO.WatcherChangeTypes.Deleted));
                                    remoteRepo.Download (remoteFile);
                                    LocalRepo.Files.Add (new StorageQloudObject (remoteFile.AbsolutePath));
                                }
                            }
                        }                            
                        alreadyAnalyzed.Add (localFile);
                    } else {
                        if (remoteFile.IsAFolder) {
                            ChangesInLastSync.Add(new Change(remoteFile, System.IO.WatcherChangeTypes.Created));
                            System.IO.Directory.CreateDirectory (remoteFile.FullLocalName);
                            Logger.LogInfo ("BacklogSynchronizer","Creating folder "+remoteFile.FullLocalName);
                            AddFile(remoteFile);
                            alreadyAnalyzed.Add (localFile);
                        } else {
                            //remote file was create when offline
                            if (System.IO.File.Exists (localFile.FullLocalName)) {
                                if (remoteFile.IsSync) {
                                    alreadyAnalyzed.Add (localFile);
                                    AddFile (remoteFile);
                                    continue;
                                }
                            } 
                            ChangesMade.Add (remoteFile);
                            ChangesInLastSync.Add(new Change(remoteFile, System.IO.WatcherChangeTypes.Created));
                            remoteRepo.Download (remoteFile);
                            LocalRepo.Files.Add (new StorageQloudObject (remoteFile.AbsolutePath));
                            if (localFile != null)
                                alreadyAnalyzed.Add (localFile);
                        }
                        AddFile (remoteFile);
                    }
                }
                //not exists remote file
                foreach (StorageQloudObject localFile in filesInLocalRepo) {
                    string resolvedPath = LocalRepo.ResolveDecodingProblem (localFile.AbsolutePath);
                    if (alreadyAnalyzed.Any (lf => LocalRepo.ResolveDecodingProblem (lf.AbsolutePath) == resolvedPath || LocalRepo.ResolveDecodingProblem (lf.AbsolutePath).Contains (resolvedPath + "/")))
                        continue;

                    if (!System.IO.File.Exists (localFile.FullLocalName) && !localFile.IsAFolder)
                        continue;

                    StorageQloudObject backlogFile = backlogDocument.Get (localFile.AbsolutePath);
                    bool existsInBacklog = backlogFile != null;
                    //remote file was deleted when offline
                    if (existsInBacklog) {
                        if (localFile.IsAFolder) {
                            if (System.IO.Directory.Exists (localFile.FullLocalName)) {
                                ChangesInLastSync.Add(new Change(localFile, System.IO.WatcherChangeTypes.Deleted));
                                LocalRepo.Delete(localFile);
                            }
                            continue;            
                        }

                        StorageQloudObject lf = new StorageQloudObject (localFile.AbsolutePath);

                        if (remoteRepo.SendLocalVersionToTrash (lf).Status == TransferStatus.DONE) {
                            ChangesMade.Add (lf);
                            ChangesInLastSync.Add(new Change(localFile, System.IO.WatcherChangeTypes.Deleted));
                            LocalRepo.Delete (localFile);
                        }
                    }
                    // local file was created when offline
                    else {
                        if (localFile.IsAFolder) {    
                            remoteRepo.CreateFolder (localFile);
                            AddFile (localFile);
                            continue;            
                        }
                        remoteRepo.Upload (localFile);
                        AddFile (localFile);
                    }
                    alreadyAnalyzed.Add (localFile);
                }
            
                Logger.LogInfo ("Synchronizer", "Backlog Sync is finished");
            }catch (DisconnectionException )
            {
                Program.Controller.HandleDisconnection();
            }
            catch (AccessDeniedException)
            {
                Program.Controller.HandleAccessDenied();
            }
            catch (Exception e)
            {
            }
            finally {
                Status = SyncStatus.IDLE;
            }
        }         

        public void RemoveFileByAbsolutePath (StorageQloudObject sqObj)
        {
            this.backlogDocument.RemoveFileByAbsolutePath (sqObj);
        }

        public void AddFile (StorageQloudObject folder)
        {

            this.backlogDocument.AddFile(folder);
        }

        public void EditFileByName (StorageQloudObject remoteFile)
        {
            this.backlogDocument.EditFileByName(remoteFile);
        }

        public void EditFileByHash (StorageQloudObject file)
        {
            this.backlogDocument.EditFileByHash(file);
        }


    }
}


