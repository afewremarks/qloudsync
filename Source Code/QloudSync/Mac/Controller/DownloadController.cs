using System;
using System.Threading;
using GreenQloud.Repository;
using System.IO;
using System.Collections.Generic;
using GreenQloud.Synchrony;

namespace GreenQloud
{
    //Controla as threads responsaveis pelas sincronizacoes de download
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
        public delegate void ProgressChangedEventHandler (double percentage, double time);

        public event FinishedEventHandler Finished = delegate { };
        public delegate void FinishedEventHandler ();

       #endregion

        protected DownloadController ()
        {
            Status = SyncStatus.Idle;
            remote_timer.Elapsed += SynchronizeOnTime;
            remote_timer.Disposed += (object sender, EventArgs e) => Console.WriteLine("Dispose");
        }

        public static DownloadController GetInstance ()
        {
            if (instance == null)
                instance = new DownloadController();
            return instance;
        }


        public void FirstLoad()
        {
            ThreadController(new Thread(DownloadSynchronizer.GetInstance().FullLoad));
            Status = SyncStatus.Sync;
            while (!synchronizer.Done);
            Status = SyncStatus.Idle;
            remote_timer.Start();
        }

        int controller = 0;

        //Disparado pelo Timer
        void SynchronizeOnTime (object sender, System.Timers.ElapsedEventArgs e)
        {
            Synchronize ();
        }

        public override void Synchronize ()
        {
            if(!remote_timer.Enabled)
                remote_timer.Start();
            ThreadController(new Thread(DownloadSynchronizer.GetInstance().Synchronize));
        }
       
        public void ThreadController (Thread downThread)
        {
            ++controller;
            if (Status == SyncStatus.Idle && controller == 1)
            {

                try
                {
                    ClearDownloadIndexes ();
                    downThread.Start ();
                    double lastSize = 0;
                    DateTime lastTime = DateTime.Now;
                    TimeRemaining = 0;

                    while (Percent < 100)
                    {
                       if (synchronizer.Done)
                            break;
                        DateTime time = DateTime.Now;
                        double size = synchronizer.Size;
                        double transferred = synchronizer.BytesTransferred;
                        if (size != 0)
                        {    
                            if(Status != SyncStatus.Sync)
                                Status = SyncStatus.Sync;
                            Percent = (transferred / size) * 100;
                            double diffSeconds = time.Subtract(lastTime).TotalMilliseconds;
                            if(diffSeconds!=0){
                                double diffSize = transferred - lastSize;
                                double sizeRemaining = size - transferred;
                                double dTimeRemaninig = (sizeRemaining/diffSize)/(diffSeconds/1000);
                                dTimeRemaninig = Math.Round(dTimeRemaninig, 0);
                                if (TimeRemaining == 0 || (TimeRemaining>dTimeRemaninig && dTimeRemaninig>0)){
                                        TimeRemaining = dTimeRemaninig;
                                }
                            }
                        }
                        lastSize = transferred;
                        lastTime = time;
                        ProgressChanged (Percent, TimeRemaining);
                        Thread.Sleep (1000);
                    }
                }
                catch (Exception e)
                {
                    Logger.LogInfo("DownloadController", e);
                }

                Status = SyncStatus.Idle;
                controller = 0;
            }
        }

        public void Stop ()
        {
            DownloadSynchronizer.GetInstance().Done = true;
            remote_timer.Stop();
        }


    }
}

