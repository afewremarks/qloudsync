using SparkleLib;


using System;
using System.IO;
using System.Windows.Forms;
using System.Threading;
using SQ;
using SQ.Security;
using SQ.Synchrony;
using SQ.Repository;
using SQ.Util;

namespace SparkleLib.SQ
{
	public class SparkleFetcher : SparkleFetcherBase
	{

        bool finished = false;
        double downSize = 0;

		public SparkleFetcher (string server, string required_fingerprint, string remote_path,
            string target_folder, bool fetch_prior_history) : base (server, required_fingerprint,
                remote_path, target_folder, fetch_prior_history)
		{
            QloudSyncPlugin.InitRepo = false;
            LocalRepo.LocalFolder = TargetFolder;

			Credential.URLConnection = RemoteUrl.Host;
            LocalRepo.PendingChanges = new System.Collections.Generic.List<Change>();
            FileInfo backlog = new FileInfo(Constant.BACKLOG_FILE);
            if(backlog.Exists)
                backlog.Delete();
            //Console.WriteLine (server+" "+required_fingerprint+" "+remote_path+" "+target_folder+" "+ fetch_prior_history+" "+TargetFolder);
		}



		public override bool Fetch ()
		{
            Directory.CreateDirectory(LocalRepo.LocalFolder);

			try
			{
                Thread downThread = new Thread(DownThreadMethod);
				double percentage = 0;
				downThread.Start ();

				while (percentage < 100)
				{
                  if (finished)
                        break;
					
                    double repoSize = LocalRepo.Size;

					if (downSize != 0)
						percentage = (repoSize / downSize)*100;
					base.OnProgressChanged (percentage);
					Thread.Sleep (1000);
				}
				QloudSyncPlugin.WriteKeys();
               
			}
			catch(Exception e)
			{
                Logger.LogInfo("Fetcher", e);
				return false;
			}
			return true;

		}


        void DownThreadMethod ()
        {
            finished = false;
            if (RemoteRepo.InitBucket ()) {
                if(RemoteRepo.InitTrashFolder ()){
                    System.Collections.Generic.List<RemoteFile> remoteFiles = RemoteRepo.Files;
                    foreach (RemoteFile remoteFile in remoteFiles) {
                        if (!remoteFile.IsIgnoreFile)
                            downSize += remoteFile.AsS3Object.Size;
                    }
                    foreach (RemoteFile remoteFile in remoteFiles) {
                        if(remoteFile.IsAFolder)
                            new DirectoryInfo (remoteFile.FullLocalName).Create();
                        else
                        {
                            if (!remoteFile.IsIgnoreFile)
                                RemoteRepo.Download (remoteFile);
                        }
                    }
                    BacklogSynchronizer.GetInstance().Write();
                }
            }
            finished = true;
        }

		public override void Stop ()
        {
            finished = true;
		}

		protected new void OnProgressChanged (double percentage)
		{
			// TODO
		}

		public override bool IsFetchedRepoEmpty 
		{
			get {
                return false;
            }
        }


        public override void EnableFetchedRepoCrypto (string password)
        {
            
        }


        public override bool IsFetchedRepoPasswordCorrect (string password)
		{
			return false;
		}


        public override void Complete ()
        {
            base.Complete();
        }


        
        private void AddWarnings ()
        {
            /*
            SparkleGit git = new SparkleGit (TargetFolder,
                "config --global core.excludesfile");

            git.Start ();

            // Reading the standard output HAS to go before
            // WaitForExit, or it will hang forever on output > 4096 bytes
            string output = git.StandardOutput.ReadToEnd ().Trim ();
            git.WaitForExit ();
			
            if (string.IsNullOrEmpty (output))
                return;
            else
                this.warnings.Add ("You seem to have a system wide ‘gitignore’ file, this may affect SparkleShare files.");
            */
        }

		//public override string [] Warnings { get; }

		void RenameFolderToDefault ()
		{
			string newTargetFolder = TargetFolder.Replace("\\default", "\\"+Credential.User+"-default");
			 try{
				Directory.Move(TargetFolder, newTargetFolder);
				TargetFolder = newTargetFolder;
			}
			catch
			{

			}

		}
	}
}

