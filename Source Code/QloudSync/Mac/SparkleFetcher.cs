 


using System;
using System.IO;
using System.Windows.Forms;
using System.Threading;
using QloudSync.Repository;
using QloudSync.Security;
using QloudSync.Util;
using QloudSync.Synchrony;
using System.Collections.Generic;


namespace QloudSync
{
	public class SparkleFetcher
	{

        public event Action Started = delegate { };
        public event Action Failed = delegate { };
        
        public event FinishedEventHandler Finished = delegate { };
        public delegate void FinishedEventHandler ();
        
        public event ProgressChangedEventHandler ProgressChanged = delegate { };
        public delegate void ProgressChangedEventHandler (double percentage);
        
        public Uri RemoteUrl { get; protected set; }
        public string RequiredFingerprint { get; protected set; }
        public readonly bool FetchPriorHistory = false;
        public string TargetFolder { get; protected set; }
        public bool IsActive { get; private set; }
        public string Identifier;
        
        public string [] Warnings {
            get {
                return this.warnings.ToArray ();
            }
        }
        
        public string [] Errors {
            get {
                return this.errors.ToArray ();
            }
        }
        
        
        protected List<string> warnings = new List<string> ();
        protected List<string> errors   = new List<string> ();
        
        protected string [] ExcludeRules = new string [] {
            "*.autosave", // Various autosaving apps
            "*~", // gedit and emacs
            ".~lock.*", // LibreOffice
            "*.part", "*.crdownload", // Firefox and Chromium temporary download files
            ".*.sw[a-z]", "*.un~", "*.swp", "*.swo", // vi(m)
            ".directory", // KDE
            ".DS_Store", "Icon\r\r", "._*", ".Spotlight-V100", ".Trashes", // Mac OS X
            "*(Autosaved).graffle", // Omnigraffle
            "Thumbs.db", "Desktop.ini", // Windows
            "~*.tmp", "~*.TMP", "*~*.tmp", "*~*.TMP", // MS Office
            "~*.ppt", "~*.PPT", "~*.pptx", "~*.PPTX",
            "~*.xls", "~*.XLS", "~*.xlsx", "~*.XLSX",
            "~*.doc", "~*.DOC", "~*.docx", "~*.DOCX",
            "*/CVS/*", ".cvsignore", "*/.cvsignore", // CVS
            "/.svn/*", "*/.svn/*", // Subversion
            "/.hg/*", "*/.hg/*", "*/.hgignore", // Mercurial
            "/.bzr/*", "*/.bzr/*", "*/.bzrignore" // Bazaar
        };
        
        
        private Thread thread;
        
        
        public SparkleFetcher (string server, string remote_path)
        {
            remote_path         = remote_path.Trim ("/".ToCharArray ());

            RemoteUrl    = new Uri (server + remote_path);
            IsActive     = false;

            LocalRepo.LocalFolder = TargetFolder;
            Console.WriteLine ("LocalRepo.LocalFolder "+LocalRepo.LocalFolder);
            Credential.URLConnection = RemoteUrl.Host;
            LocalRepo.PendingChanges = new System.Collections.Generic.List<Change>();
            FileInfo backlog = new FileInfo(Constant.BACKLOG_FILE);
            if (backlog.Exists)
                backlog.Delete();

        }
        
        
        public void Start ()
        {
            IsActive = true;
            Started ();
            
            Logger.LogInfo ("Fetcher", TargetFolder + " | Fetching folder: " + RemoteUrl);
            
            if (Directory.Exists (TargetFolder))
                Directory.Delete (TargetFolder, true);

            
            this.thread = new Thread (() => {
                if (Fetch ()) {
                    
                    Thread.Sleep (500);
                    Logger.LogInfo ("Fetcher", "Finished");
                    
                    IsActive = false;
                    
                    Finished ();
                    
                } else {
                    
                    Thread.Sleep (500);
                    Logger.LogInfo ("Fetcher", "Failed");
                    
                    IsActive = false;
                    Failed ();
                }
            });
            
            this.thread.Start ();
        }
        
        
        public virtual void Complete ()
        {
        }
        
        
        public void Dispose ()
        {
            if (this.thread != null)
                this.thread.Abort ();
        }
        
        
        protected void OnProgressChanged (double percentage) {
            ProgressChanged (percentage);
        }
        

        bool finished = false;
        double downSize = 0;

		public bool Fetch ()
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
					OnProgressChanged (percentage);
					Thread.Sleep (1000);
				}
				
               
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

		public void Stop ()
        {
            finished = true;
		}

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

