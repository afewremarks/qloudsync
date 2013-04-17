using System;
using GreenQloud.Repository.Local;
using GreenQloud.Repository.Remote;
using GreenQloud.Persistence;
using System.Threading;
using GreenQloud.Persistence.SQLite;

namespace GreenQloud.Synchrony
{
    public class StorageQloudRemoteEventsSynchronizer : RemoteEventsSynchronizer
    {
        static StorageQloudRemoteEventsSynchronizer instance;

        Thread threadTimer;

        System.Timers.Timer remote_timer;

        public new event ProgressChangedEventHandler ProgressChanged = delegate { };
        public new delegate void ProgressChangedEventHandler (double percentage, double time);

        public StorageQloudRemoteEventsSynchronizer (LogicalRepositoryController logicalLocalRepository, PhysicalRepositoryController physicalLocalRepository, 
                                                     RemoteRepositoryController remoteRepository, TransferDAO transferDAO, EventDAO eventDAO) :
            base (logicalLocalRepository, physicalLocalRepository, remoteRepository, transferDAO, eventDAO)
        {
            threadTimer = new Thread( ()=>{
                try{
                    remote_timer =  new System.Timers.Timer () { Interval = GlobalSettings.IntervalBetweenChecksRemoteRepository };        
                    
                    remote_timer.Elapsed += (object sender, System.Timers.ElapsedEventArgs e)=>{
                        base.AddEvents();               
                    };
                    remote_timer.Disposed += (object sender, EventArgs e) => Logger.LogInfo("Synchronizer","Disposing timer.");
                }catch (DisconnectionException)
                {
                    SyncStatus = SyncStatus.IDLE;
                    Program.Controller.HandleDisconnection();
                }
            });
        }

        public static StorageQloudRemoteEventsSynchronizer GetInstance(){
            if (instance == null)
                instance = new StorageQloudRemoteEventsSynchronizer (new StorageQloudLogicalRepositoryController(), 
                                                                    new StorageQloudPhysicalRepositoryController(),
                                                                    new StorageQloudRemoteRepositoryController(),
                                                                    new SQLiteTransferDAO (),
                                                                    new SQLiteEventDAO ());
            return instance;
        }

        public new void Start ()
        {
            try{
                if(remote_timer==null)
                {   
                    threadTimer.Start();
                    while (remote_timer==null);
                }
                if (!remote_timer.Enabled)
                    remote_timer.Start ();
                base.Start();
            }catch{
                // do nothing
            }           
        }
        
        public override void Pause ()
        {
            remote_timer.Stop();
        }
        
        public new void Stop ()
        {
            remote_timer.Stop();
            base.Stop();
            threadTimer.Join();
        }
        
        public ThreadState ControllerStatus{
            get{
                return threadTimer.ThreadState;
            }
        }

        public void FirstLoad()
        {
            try {
                Done = false;
                new Thread(()=>{
                    Size = InitFirstLoad();
                }).Start();
                double lastSize = 0;
                DateTime lastTime = DateTime.Now;
                TimeRemaining = 0;
                Model.Transfer transfer = new GreenQloud.Model.Transfer();
                transfer.Item = new Model.RepositoryItem();
                transfer.Item.FullLocalName = string.Empty;
                remoteRepository.CurrentTransfer =transfer; 
                OldCurrentTransfer = transfer;
                while (Percent < 100) {
                    GreenQloud.Model.Transfer Current = remoteRepository.CurrentTransfer;

                    if (Done)
                        break;
                    if(OldCurrentTransfer.Item.FullLocalName == Current.Item.FullLocalName){
                        DateTime time = DateTime.Now;
                        double size = Size;
                        double transferred = BytesTransferred + Current.TransferredBits;
                        
                        if (size != 0) {   
                            Percent = (transferred / size) * 100;
                            double diffSeconds = time.Subtract (lastTime).TotalMilliseconds;
                            if (diffSeconds != 0) {
                                double diffSize = transferred - lastSize;
                                double sizeRemaining = size - transferred;
                                double dTimeRemaninig = (sizeRemaining / diffSize) / (diffSeconds / 1000);
                                dTimeRemaninig = Math.Round (dTimeRemaninig, 0);
                                TimeRemaining = dTimeRemaninig;
                            }
                        }
                        lastSize = transferred;
                        lastTime = time;
                        ProgressChanged (Percent, TimeRemaining);
                    }
                    else{
                        BytesTransferred += OldCurrentTransfer.TotalSize;

                        OldCurrentTransfer = remoteRepository.CurrentTransfer;
                    }

                    Thread.Sleep (1000);
                }
            }catch (Exception e) {                
                Logger.LogInfo ("RemoteEventSync", e.Message+"\n "+e.StackTrace);
            }
        }
    
        protected GreenQloud.Model.Transfer OldCurrentTransfer{
            set; get;
        }

        protected double Percent {
            set; get;
        }
        
        protected double Speed {
            set; get;
        }
        
        protected double TimeRemaining {
            set; get;
        }

        protected double BytesTransferred {
            set; get;
        }

        protected void ClearDownloadIndexes()
        {
            Percent = 0;
            Speed = 0;
            TimeRemaining = 0;
        }
        
        public double Size {
            set; get;
        }
    }
        
 }

