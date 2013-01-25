using System;
using System.Threading;
using QloudSync.Repository;
using System.IO;
using System.Collections.Generic;
using QloudSync.Synchrony;

namespace QloudSync
{
    public class DownloadController : SynchronizerController
    {
       
        private static DownloadController instance;
        private System.Timers.Timer remote_timer         = new System.Timers.Timer () { Interval = GlobalSettings.IntervalBetweenChecksRemoteRepository };
        DownloadSynchronizer synchronizer = DownloadSynchronizer.GetInstance();

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

        public void FirstLoad()
        {
            ThreadController(new Thread(DownloadSynchronizer.GetInstance().FullLoad));
        }

        
        public override void Synchronize ()
        {
            if(!remote_timer.Enabled)
                remote_timer.Start();
            ThreadController(new Thread(DownloadSynchronizer.GetInstance().Synchronize));
        }
       
        public void ThreadController (Thread downThread)
        {
            try
            {
                ClearDownloadIndexes ();
                downThread.Start ();
               
                while (Percent < 100)
                {
                    if (synchronizer.Done)
                        break;

                    if (synchronizer.Size != 0)
                        Percent = (synchronizer.BytesTransferred / synchronizer.Size) * 100;
                    
                    ProgressChanged (Percent);
                    Console.WriteLine ("Debug "+Percent);
                    Thread.Sleep (1000);
                }
            }
            catch (Exception e)
            {
                Logger.LogInfo("DownloadController", e);
            }
        }



     
        public void Stop ()
        {
            DownloadSynchronizer.GetInstance().Done = true;
            remote_timer.Stop();
        }


    }
}

