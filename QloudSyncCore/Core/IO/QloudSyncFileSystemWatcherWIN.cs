#if __MonoCS__
#else
using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using System.Threading;
using System.Text;
using System.Collections.Concurrent;
using GreenQloud.Model;
using GreenQloud.Persistence.SQLite;
using System.Collections;

namespace GreenQloud
{
    public class QloudSyncFileSystemWatcher
    {
        private SQLiteRepositoryDAO repositoryDAO = new SQLiteRepositoryDAO();
        private ArrayList ignoreBag;
        private object _bagLock = new object();

        public delegate void ChangedEventHandler(Event e);
        public event ChangedEventHandler Changed;

        public QloudSyncFileSystemWatcher(string pathwatcher)
        {
            ignoreBag = new ArrayList();
            FileSystemWatcher fw = new FileSystemWatcher(pathwatcher);
            fw.IncludeSubdirectories = true;
            fw.Changed += new FileSystemEventHandler(OnChanged);
            fw.Created += new FileSystemEventHandler(OnCreated);
            fw.Deleted += new FileSystemEventHandler(OnDeleted);
            fw.Renamed += new RenamedEventHandler(OnRenamed);
            fw.EnableRaisingEvents = true;

        }

        private void Callback(EventType type, FileSystemEventArgs fe)
        {
            ChangedEventHandler handler = Changed;

            bool ignore = false;
            lock (_bagLock)
            {
                if (ignoreBag.Contains(fe.FullPath))
                    ignore = true;
            }

            if (!ignore)
            {

                Event e = new Event();

                //TODO
                //LocalRepository repo = repositoryDAO.GetRepositoryByItemFullName(paths[i]);
                //e.Item = RepositoryItem.CreateInstance(repo, flags[i].HasFlag(FSEventStreamEventFlagItem.IsDir), key);
                handler(e);
            }
        }

        private static void OnChanged(object source, FileSystemEventArgs e)
        {
            Console.WriteLine("File: " + e.FullPath + " " + e.ChangeType);
        }

        private static void OnCreated(object source, FileSystemEventArgs e)
        {
            Console.WriteLine("File: " + e.FullPath + " " + e.ChangeType);
        }

        private static void OnDeleted(object source, FileSystemEventArgs e)
        {
            Console.WriteLine("File: " + e.FullPath + " " + e.ChangeType);
        }

        private static void OnRenamed(object source, RenamedEventArgs e)
        {
            Console.WriteLine("File: {0} renamed to {1}", e.OldFullPath, e.FullPath);
        }

        public void Block(string path)
        {
            lock (_bagLock)
            {
                ignoreBag.Add(path);
            }
        }

        public void Unblock(string path)
        {
            lock (_bagLock)
            {
                ignoreBag.Remove(path);
            }

        }

    }
}
#endif