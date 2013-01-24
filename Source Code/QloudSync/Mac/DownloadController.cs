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
        private bool downloadFinished = false;
        private double downloadSize = 0;
        private double downloadPercent = 0;
        private double downloadSpeed = 0;
        private int secondsRemaining = 0;
        private int bytesTransferred = 0;
        private RemoteRepo remoteRepo = new RemoteRepo();
        
        
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
                ClearDownloadIndexes ();
                Thread downThread = new Thread(FullLoad);
                downThread.Start ();

                while (downloadPercent < 100)
                {
                    if (downloadFinished)
                        break;
                    
                    bytesTransferred += remoteRepo.Connection.TransferSize;
                    remoteRepo.Connection.TransferSize = 0;
                    if (downloadSize != 0)
                        downloadPercent = (bytesTransferred / downloadSize) * 100;

                    ProgressChanged (downloadPercent);
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

        void FullLoad ()
        {
            downloadFinished = false;
            if (remoteRepo.Initialized ()) {
                    List<RemoteFile> remoteFiles = remoteRepo.Files;
                    CalculateDownloadSize(remoteFiles);
                    foreach (RemoteFile remoteFile in remoteFiles) {
                        if(remoteFile.IsAFolder)
                            Directory.CreateDirectory (remoteFile.FullLocalName);
                        else
                        {
                            if (!remoteFile.IsIgnoreFile)
                                remoteRepo.Download (remoteFile);
                        }
                    }
                    //BacklogSynchronizer.GetInstance().Write();
            }
            downloadFinished = true;
        }

        public void Stop ()
        {
            downloadFinished = true;
        }

        void CalculateDownloadSize (List<RemoteFile> remoteFiles)
        {
            foreach (RemoteFile remoteFile in remoteFiles) {
                if (!remoteFile.IsIgnoreFile)
                    downloadSize += remoteFile.AsS3Object.Size;
            }
        }

        void ClearDownloadIndexes()
        {
            downloadPercent = 0;
            downloadSpeed = 0;
            downloadSize = 0;
            secondsRemaining = 0;
            bytesTransferred = 0;
        }
    }
}

