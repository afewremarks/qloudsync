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
        private ArrayList ignoreBag;
        private object _bagLock = new object();
        SQLiteRepositoryDAO repositoryDAO = new SQLiteRepositoryDAO();
        public delegate void ChangedEventHandler(Event e);
        public event ChangedEventHandler Changed;
        FileSystemWatcher watcherFile, watcherFolder;  

        public QloudSyncFileSystemWatcher(string pathwatcher)
        {
            ignoreBag = new ArrayList();
            watcherFile = new FileSystemWatcher(pathwatcher);
            watcherFile.NotifyFilter = NotifyFilters.FileName;
            watcherFile.IncludeSubdirectories = true;
            watcherFile.Changed += new FileSystemEventHandler(OnChanged);
            watcherFile.Created += new FileSystemEventHandler(OnCreated);
            watcherFile.Deleted += new FileSystemEventHandler(OnDeleted);
            watcherFile.Renamed += new RenamedEventHandler(OnRenamed);
            watcherFile.EnableRaisingEvents = true;

            watcherFolder = new FileSystemWatcher(pathwatcher);
            watcherFolder.NotifyFilter = NotifyFilters.DirectoryName;
            watcherFolder.IncludeSubdirectories = true;
            watcherFolder.Changed += new FileSystemEventHandler(OnChanged);
            watcherFolder.Created += new FileSystemEventHandler(OnCreated);
            watcherFolder.Deleted += new FileSystemEventHandler(OnDeleted);
            watcherFolder.Renamed += new RenamedEventHandler(OnRenamed);
            watcherFolder.EnableRaisingEvents = true;

        }

        private void Callback(EventType type, FileSystemEventArgs fe, FileSystemWatcher source)
        {
            if (type == EventType.UPDATE && fe.FullPath.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                return;
            }
            
            lock (_bagLock) {
                ChangedEventHandler handler = Changed;

                bool ignore = false;
                if (ignoreBag.Contains (fe.FullPath))
                    ignore = true;
            

                if (!ignore) {
                    Event e = new Event ();
                    e.EventType = type;
                    LocalRepository repo = repositoryDAO.GetRepositoryByItemFullName (fe.FullPath);

                    if (e.EventType == EventType.MOVE && ((RenamedEventArgs)fe).OldFullPath.Length > 0)
                    {
                        string key = ((RenamedEventArgs)fe).OldFullPath.Substring(repo.Path.Length);
                        if ((FileSystemWatcher)source == watcherFolder && !key.EndsWith(Path.DirectorySeparatorChar.ToString()))
                            key += Path.DirectorySeparatorChar;
                        e.Item = RepositoryItem.CreateInstance(repo, key);

                        string keyResult = ((RenamedEventArgs)fe).FullPath.Substring(repo.Path.Length);
                        if ((FileSystemWatcher)source == watcherFolder && !keyResult.EndsWith(Path.DirectorySeparatorChar.ToString()))
                            keyResult += Path.DirectorySeparatorChar;
                        e.Item.ResultItem = RepositoryItem.CreateInstance(repo, keyResult);
                     } else {
                        string key = fe.FullPath.Substring(repo.Path.Length);
                        if ((FileSystemWatcher)source == watcherFolder && !key.EndsWith(Path.DirectorySeparatorChar.ToString()))
                            key += Path.DirectorySeparatorChar;

                        e.Item = RepositoryItem.CreateInstance(repo, key);
                    }


                    handler (e);
                }
            }
        }

        private void OnChanged(object source, FileSystemEventArgs e)
        {
            Callback(EventType.UPDATE, e, (FileSystemWatcher) source);
        }

        private void OnCreated(object source, FileSystemEventArgs e)
        {
            Callback(EventType.CREATE, e, (FileSystemWatcher) source);
        }

        private void OnDeleted(object source, FileSystemEventArgs e)
        {
            Callback(EventType.DELETE, e, (FileSystemWatcher)source);
        }

        private void OnRenamed(object source, RenamedEventArgs e)
        {
            Callback(EventType.MOVE, e, (FileSystemWatcher)source);
        }

        public void Block(string path)
        {
            lock (_bagLock)
            {
                //string parent = path.Substring(0, path.LastIndexOf(Path.DirectorySeparatorChar.ToString()));
                //ignoreBag.Add(parent);
                ignoreBag.Add(path);
            }
        }

        public void Unblock(string path)
        {
            lock (_bagLock)
            {
                //string parent = path.Substring(0, path.LastIndexOf(Path.DirectorySeparatorChar.ToString()));
                //ignoreBag.Add(parent);
                ignoreBag.Remove(path);
            }

        }

    }
}