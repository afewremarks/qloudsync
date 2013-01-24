using System;
using System.Threading;
using QloudSync.Repository;
using System.IO;
using System.Collections.Generic;

namespace QloudSync
{
    public class DownloadController
    {
        private DownloadController ()
        {
        }

        private static DownloadController instance;
        private bool finished = false;
        private double downSize = 0;

        
        
        protected List<string> warnings = new List<string> ();
        protected List<string> errors   = new List<string> ();


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

        #region Events

        public event Action Started = delegate { };
        public event Action Failed = delegate { };


        public event ProgressChangedEventHandler ProgressChanged = delegate { };
        public delegate void ProgressChangedEventHandler (double percentage);

        public event FinishedEventHandler Finished = delegate { };
        public delegate void FinishedEventHandler ();



        #endregion

        public static DownloadController GetInstance ()
        {
            if (instance == null)
                instance = new DownloadController();
            return instance;
        }

        public bool FirstLoad()
        {
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
                    ProgressChanged (percentage);
                    Thread.Sleep (1000);
                }
            }
            catch (Exception e)
            {
                Logger.LogInfo("First Load", e);
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
                            Directory.CreateDirectory (remoteFile.FullLocalName);
                        else
                        {
                            if (!remoteFile.IsIgnoreFile)
                                RemoteRepo.Download (remoteFile);
                        }
                    }
                    //BacklogSynchronizer.GetInstance().Write();
                }
            }
            finished = true;
        }


        
        public void Stop ()
        {
            finished = true;
        }
    }
}

