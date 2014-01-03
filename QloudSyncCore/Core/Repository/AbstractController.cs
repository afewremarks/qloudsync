using System;
using GreenQloud.Synchrony;
using System.Threading;
using GreenQloud.Model;
using System.Collections.Generic;
using LitS3;

namespace GreenQloud.Repository
{
    public abstract class AbstractController
    {

        private static IDictionary<string, TransferStatistic> statistics = new Dictionary<string, TransferStatistic>();
        private static List<TransferStatistic> finishedStatistics = new List<TransferStatistic>();
        private static List<TransferStatistic> unfinishedStatistics = new List<TransferStatistic>();

        protected LocalRepository repo;

        public AbstractController(LocalRepository repo){
            this.repo = repo;
        }

        private object statisticsLock = new object ();
        protected void AddStatistics(string key, TransferStatistic statistic){
            lock (statisticsLock) {
                statistics.Add (key, statistic);
            }
        }
        protected void UpdateStatistics(string key, S3ProgressEventArgs args){
            lock (statistics) {
                TransferStatistic statistic;
                statistics.TryGetValue (key, out statistic);
                if (statistic != null) {
                    statistic.BytesTotal = args.BytesTotal;
                    statistic.BytesTransferred = args.BytesTransferred;
                    statistic.ProgressPercentage = args.ProgressPercentage;
                    if (statistic.ProgressPercentage < 100 && unfinishedStatistics.IndexOf (statistic) == -1) {
                        unfinishedStatistics.Add (statistic);
                        finishedStatistics.Remove (statistic);
                    }
                    if (statistic.ProgressPercentage >= 100 && finishedStatistics.IndexOf (statistic) == -1) {
                        finishedStatistics.Add (statistic);
                        unfinishedStatistics.Remove (statistic);
                    }
                }
            }
        }

        public static ICollection<TransferStatistic> Statistics {
            get { 
                return statistics.Values;
            }
        }

        public static List<TransferStatistic> UnfinishedStatistics {
            get { 
                return unfinishedStatistics;
            }
        }

        public static List<TransferStatistic> FinishedStatistics {
            get { 
                return finishedStatistics;
            }
        }

        public void PrettyPrintStatiscs(){
            Console.WriteLine ("Unfinished");
            foreach (TransferStatistic statistic in unfinishedStatistics) {
                Console.WriteLine (statistic.Key);
                Console.WriteLine ("Bytes Total: " + statistic.BytesTotal);
                Console.WriteLine ("Bytes Transferred: " + statistic.BytesTransferred);
                Console.WriteLine ("%: " + statistic.ProgressPercentage);
            }

            Console.WriteLine ("Finished");
            foreach (TransferStatistic statistic in finishedStatistics) {
                Console.WriteLine (statistic.Key);
                Console.WriteLine ("Bytes Total: " + statistic.BytesTotal);
                Console.WriteLine ("Bytes Transferred: " + statistic.BytesTransferred);
                Console.WriteLine ("%: " + statistic.ProgressPercentage);
            }
        }

        protected void BlockWatcher (string path)
        {
            SynchronizerUnit unit = SynchronizerUnit.GetByRepo(repo);
            if (unit != null)
            {
                QloudSyncFileSystemWatcher watcher = unit.LocalEventsSynchronizer.GetWatcher();
                if (watcher != null)
                {
                    watcher.Block(path);
                }
            }

        }

        protected void UnblockWatcher (string path)
        {
            SynchronizerUnit unit = SynchronizerUnit.GetByRepo(repo);
            if (unit != null)
            {
                QloudSyncFileSystemWatcher watcher = unit.LocalEventsSynchronizer.GetWatcher();
                if (watcher != null)
                {
                    Thread.Sleep(2000);
                    watcher.Unblock(path);
                }
            }
        }

    }
}

