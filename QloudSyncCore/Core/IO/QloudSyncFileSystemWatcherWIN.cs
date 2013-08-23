#if __MonoCS__
#else
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Timers;
using System.Collections;
using GreenQloud.Model;
using GreenQloud.Persistence.SQLite;

namespace GreenQloud
{
    public class QloudSyncFileSystemWatcher
    {
        private ArrayList ignoreBag;
        private object _bagLock = new object();
        public delegate void ChangedEventHandler(Event e);
        public event ChangedEventHandler Changed;
        private SQLiteRepositoryDAO repositoryDAO = new SQLiteRepositoryDAO();
        private SQLiteRepositoryItemDAO repositoryItemDAO = new SQLiteRepositoryItemDAO();

        private FileSystemWatcher parentFolderWatcher = null, subfolderWatcher = null;
        private System.Object lockThis = new System.Object(), processChangesLock = new System.Object();
        private Timer changeNotifier;

        private List<FSOPCreateVO> createList = new List<FSOPCreateVO>();
        private List<FSOPDeleteVO> deleteList = new List<FSOPDeleteVO>();
        private List<FSOPRenameVO> renameList = new List<FSOPRenameVO>();
        private List<FSOPChangeVO> changeList = new List<FSOPChangeVO>();

        private String watchPath = null;

        public QloudSyncFileSystemWatcher(String path)
        {
            this.watchPath = path;
            this.ignoreBag = new ArrayList();
            Start();
        }


        public bool Start()
        {
            if (!String.IsNullOrEmpty(watchPath))
            {
                subfolderWatcher = new FileSystemWatcher();
                subfolderWatcher.Path = watchPath;
                subfolderWatcher.IncludeSubdirectories = true;
                subfolderWatcher.NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.FileName |
                NotifyFilters.DirectoryName;
                // Add event handlers.            
                subfolderWatcher.Created += new FileSystemEventHandler(handleCreateEvent);
                subfolderWatcher.Deleted += new FileSystemEventHandler(handleDeleteEvent);
                subfolderWatcher.Renamed += new RenamedEventHandler(handleRenameEvent);
                //Begin watching
                subfolderWatcher.EnableRaisingEvents = true;

                parentFolderWatcher = new FileSystemWatcher();
                parentFolderWatcher.Path = Path.GetDirectoryName(watchPath); ;
                parentFolderWatcher.IncludeSubdirectories = true;
                parentFolderWatcher.NotifyFilter = NotifyFilters.Size;
                // Add event handlers.
                parentFolderWatcher.Changed += new FileSystemEventHandler(handleChangeEvent);
                //Begin watching
                parentFolderWatcher.EnableRaisingEvents = true;

                changeNotifier = new Timer(1000);
                changeNotifier.Elapsed += new ElapsedEventHandler(handleFileSystemChange);

                return true;
            }
            return false;
        }

        public bool Stop()
        {
            try
            {
                parentFolderWatcher.EnableRaisingEvents = false;
                subfolderWatcher.EnableRaisingEvents = false;
                
                parentFolderWatcher.Dispose();
                subfolderWatcher.Dispose();
                
                parentFolderWatcher = null;
                subfolderWatcher = null;
                return true;
            }
            catch (Exception e)
            {
                Logger.LogInfo("ERROR", "Cannot stop watcher");
                Logger.LogInfo("ERROR", e);
            }
            return false;
        }

        private void handleChangeEvent(Object sender, FileSystemEventArgs args)
        {

            lock (lockThis)
            {
                if (args.FullPath.StartsWith(watchPath))
                {
                    changeNotifier.Enabled = false;
                    if (Path.GetExtension(args.FullPath) != ".tmp" && Path.GetExtension(args.FullPath) != ".TMP"
                        && File.Exists(args.FullPath))
                        changeList.Add(new FSOPChangeVO(args.FullPath));
                    changeNotifier.Enabled = true;
                }
            }
        }

        private void handleCreateEvent(Object sender, FileSystemEventArgs args)
        {
            lock (lockThis)
            {
                changeNotifier.Enabled = false;
                createList.Add(new FSOPCreateVO(args.FullPath));
                changeNotifier.Enabled = true;
            }
        }

        private void handleDeleteEvent(Object sender, FileSystemEventArgs args)
        {
            lock (lockThis)
            {
                changeNotifier.Enabled = false;

                //Detect if have a file with the name on database, if its true, so its a file.
                //The need of this line is because the watcher cannot catch if the delete is a file or folder.
                bool isFolder = true;
                LocalRepository repo = repositoryDAO.GetRepositoryByItemFullName(args.FullPath);
                if (args.FullPath != null)
                {
                    string key = args.FullPath.Substring(repo.Path.Length);
                    if (repositoryItemDAO.ExistsUnmoved(key, repo)) {
                        isFolder = false;
                    }
                }

                deleteList.Add(new FSOPDeleteVO(args.FullPath, isFolder));
                changeNotifier.Enabled = true;
            }
        }

        private void handleRenameEvent(Object sender, RenamedEventArgs args)
        {
            lock (lockThis)
            {
                changeNotifier.Enabled = false;
                renameList.Add(new FSOPRenameVO(args.OldFullPath, args.FullPath));
                changeNotifier.Enabled = true;
            }
        }

        private void handleFileSystemChange(Object sender, ElapsedEventArgs args)
        {
            changeNotifier.Enabled = false;
            processChanges();
        }

        private Event BuildEvent(EventType type, bool isDirectory, string path, string newPath = null) {
            Event e = new Event();
            LocalRepository repo = repositoryDAO.GetRepositoryByItemFullName(path);
            e.EventType = type;
            if (path != null)
            {
                string key = path.Substring(repo.Path.Length);
                if (isDirectory && !key.EndsWith(Path.DirectorySeparatorChar.ToString()))
                    key += Path.DirectorySeparatorChar;
                e.Item = RepositoryItem.CreateInstance(repo, key);
            }

            if (newPath != null)
            {
                string key = newPath.Substring(repo.Path.Length);
                if (isDirectory && !key.EndsWith(Path.DirectorySeparatorChar.ToString()))
                    key += Path.DirectorySeparatorChar;

                e.Item.ResultItem = RepositoryItem.CreateInstance(repo, key);
            }
            return e;
        }
        private void processChanges()
        {
            lock (processChangesLock)
            {
                lock (_bagLock) {
                    ChangedEventHandler handler = Changed;
                    
                    try
                    {
                        while (true)
                        {
                            FSOPCreateVO createItem = null;
                            FSOPDeleteVO deleteItem = null;
                            if (createList.Count > 0)
                            {
                                if ((createItem = getFirsttmpCreateItem()) != null)
                                {
                                    FSOPRenameVO renameItem = getRenameItem(createItem.path);
                                    if (renameItem != null)
                                    {
                                        deleteItemFromDeleteList(renameItem.newPath);
                                        String TMPfilePath = deleteTMPItemFromCreateList(renameItem.newPath);
                                        deleteItemFromDeleteList(TMPfilePath);
                                        renameList.Remove(renameItem);
                                        
                                        if (!ignoreBag.Contains(renameItem.newPath))
                                        {
                                            Event e = BuildEvent(EventType.UPDATE, false, renameItem.newPath);
                                            Changed(e);
                                            
                                            //Console.WriteLine("***************************************File Tag Updated*****************************************\n");
                                            //Console.WriteLine("Path: " + renameItem.newPath);
                                        }
                                    }
                                    deleteItemFromCreateList(createItem.path);
                                    deleteItemFromDeleteList(createItem.path);
                                }
                                else if ((createItem = createList[0]) != null && deleteList.Count > 0 && (deleteItem = getDeleteItemByName(Path.GetFileName(createItem.path))) != null)
                                {
                                    bool isDirectory = true;
                                    if (createItem.type == "file")
                                    {
                                        isDirectory = false;
                                    }

                                    if (!ignoreBag.Contains(deleteItem.path))
                                    {
                                        Event e = BuildEvent(EventType.MOVE, isDirectory, deleteItem.path, createItem.path);
                                        Changed(e);

                                        //Console.WriteLine("***************************************" + message + "*****************************************\n");
                                        //Console.WriteLine("From: " + deleteItem.path + "\nTo: " + createItem.path);
                                    }
                                    
                                    createList.Remove(createItem);
                                    deleteList.Remove(deleteItem);
                                }
                                else
                                {
                                    if (Path.GetExtension(createItem.path) != ".tmp" && Path.GetExtension(createItem.path) != ".TMP")
                                    {
                                        bool isDirectory = true;
                                        if (createItem.type == "file")
                                        {
                                            isDirectory = false;
                                        }

                                        if (!ignoreBag.Contains(createItem.path))
                                        {
                                            Event e = BuildEvent(EventType.CREATE, isDirectory, createItem.path);
                                            Changed(e);

                                            //Console.WriteLine("***************************************" + message + "*****************************************\n");
                                            //Console.WriteLine("Path: " + createItem.path);
                                        }
                                        
                                        createList.Remove(createItem);
                                        deleteItemFromChangeList(createItem.path);
                                    }
                                }
                            }
                            else if (deleteList.Count > 0)
                            {
                                deleteItem = getFirstValidDeleteItem();

                                if (deleteItem != null)
                                {
                                    if (!ignoreBag.Contains(deleteItem.path))
                                    {
                                        Event e = BuildEvent(EventType.DELETE, deleteItem.isFolder, deleteItem.path);
                                        Changed(e);

                                        //Console.WriteLine("***************************************Delete*****************************************\n");
                                        //Console.WriteLine("Path: " + deleteItem.path);
                                    }
                                    deleteList.Remove(deleteItem);
                                }
                                else
                                {
                                    clearDeleteList();
                                }
                            }
                            else if (renameList.Count > 0)
                            {
                                FSOPRenameVO renameItem = getFirstValidRenameItem();
                                if (renameItem != null)
                                {
                                    bool isDirectory = true;
                                    if (renameItem.type == "file")
                                    {
                                        isDirectory = false;
                                    }

                                    if (!ignoreBag.Contains(renameItem.oldPath))
                                    {
                                        Event e = BuildEvent(EventType.MOVE, isDirectory, renameItem.oldPath, renameItem.newPath);
                                        Changed(e);

                                        //Console.WriteLine("***************************************" + message + "*****************************************\n");
                                        //Console.WriteLine("From: " + renameItem.oldPath + "\nTo: " + renameItem.newPath);
                                    }

                                    renameList.Remove(renameItem);
                                }
                                else
                                {
                                    clearRenameList();
                                }
                            }
                            else if (changeList.Count > 0)
                            {
                                FSOPChangeVO changeItem = changeList[0];

                                if (!ignoreBag.Contains(changeItem.path))
                                {
                                    Event e = BuildEvent(EventType.UPDATE, false, changeItem.path);
                                    Changed(e);

                                    //Console.WriteLine("***************************************File Data Changed*****************************************\n");
                                    //Console.WriteLine("Path: " + changeItem.path);
                                }

                                changeList.Remove(changeItem);
                            }
                            else
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogInfo("ERROR", ex.Message);
                        clearDeleteList();
                        clearRenameList();
                        clearCreateList();
                        clearChangeList();
                    }
                }
            }
        }

        private void clearCreateList()
        {
            createList = null;
            createList = new List<FSOPCreateVO>();
        }

        private void clearChangeList()
        {
            changeList = null;
            changeList = new List<FSOPChangeVO>();
        }



        private void getFirstValidChangeItem()
        {
            foreach (FSOPChangeVO changeItem in changeList)
            {
                if (Path.GetExtension(changeItem.path) != ".tmp" && Path.GetExtension(changeItem.path) != ".TMP")
                {
                }
            }
        }

        private void deleteItemFromChangeList(String path)
        {
            if (!String.IsNullOrEmpty(path))
            {
                FSOPChangeVO changeVO = null;
                foreach (FSOPChangeVO changeItem in changeList)
                {
                    if (changeItem.path == path)
                    {
                        changeVO = changeItem;
                        break;
                    }
                }
                if (changeVO != null)
                    changeList.Remove(changeVO);
            }
        }
        private void clearRenameList()
        {
            renameList = null;
            renameList = new List<FSOPRenameVO>();
        }

        private FSOPRenameVO getFirstValidRenameItem()
        {
            foreach (FSOPRenameVO renameItem in renameList)
            {
                if (Path.GetExtension(renameItem.oldPath) != ".tmp")
                    return renameItem;
            }
            return null;
        }

        private void clearDeleteList()
        {
            deleteList = null;
            deleteList = new List<FSOPDeleteVO>();
        }

        private FSOPDeleteVO getDeleteItem(String path)
        {
            if (!String.IsNullOrEmpty(path))
            {
                foreach (FSOPDeleteVO deleteItem in deleteList)
                {
                    if (deleteItem.path == path)
                        return deleteItem;
                }
            }
            return null;
        }

        private FSOPDeleteVO getFirstValidDeleteItem()
        {
            foreach (FSOPDeleteVO deleteItem in deleteList)
            {
                if (Path.GetExtension(deleteItem.path) != ".tmp" && Path.GetExtension(deleteItem.path) != ".TMP")
                    return deleteItem;
            }
            return null;
        }

        private FSOPCreateVO getFirsttmpCreateItem()
        {
            foreach (FSOPCreateVO createItem in createList)
            {
                if (createItem.type == "file" && Path.GetExtension(createItem.path) == ".tmp")
                    return createItem;
            }
            return null;
        }

        private FSOPDeleteVO getDeleteItemByName(String name)
        {
            if (!String.IsNullOrEmpty(name))
            {
                foreach (FSOPDeleteVO deleteItem in deleteList)
                {
                    String itemName = Path.GetFileName(deleteItem.path);
                    if (itemName == name)
                        return deleteItem;
                }
            }
            return null;
        }

        private FSOPRenameVO getRenameItem(String oldPath)
        {
            if (!String.IsNullOrEmpty(oldPath))
            {
                foreach (FSOPRenameVO renameItem in renameList)
                {
                    if (renameItem.oldPath == oldPath)
                        return renameItem;
                }
            }
            return null;
        }

        private FSOPCreateVO getCreateTMPItem(String path)
        {
            if (!String.IsNullOrEmpty(path))
            {
                foreach (FSOPCreateVO createItem in createList)
                {
                    if (createItem.path.StartsWith(path) && Path.GetExtension(createItem.path) == ".TMP")
                        return createItem;
                }
            }
            return null;
        }

        private String deleteTMPItemFromCreateList(String path)
        {
            if (!String.IsNullOrEmpty(path))
            {
                FSOPCreateVO createVO = getCreateTMPItem(path);
                if (createVO != null)
                {
                    createList.Remove(createVO);
                    return createVO.path;
                }
            }
            return null;
        }

        private void deleteItemFromCreateList(String path)
        {
            if (!String.IsNullOrEmpty(path))
            {
                FSOPCreateVO createVO = null;
                foreach (FSOPCreateVO createItem in createList)
                {
                    if (createItem.path == path)
                    {
                        createVO = createItem;
                        break;
                    }
                }
                if (createVO != null)
                    createList.Remove(createVO);
            }
        }

        private void deleteItemFromDeleteList(String path)
        {
            if (!String.IsNullOrEmpty(path))
            {
                FSOPDeleteVO deleteVO = null;
                foreach (FSOPDeleteVO deleteItem in deleteList)
                {
                    if (deleteItem.path == path)
                    {
                        deleteVO = deleteItem;
                        break;
                    }
                }
                if (deleteVO != null)
                    deleteList.Remove(deleteVO);
            }
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


    class FSOPCreateVO
    {
        public String type;
        private String _path;

        public FSOPCreateVO(String path)
        {
            this.path = path;
        }

        public String path
        {
            set
            {
                if (File.Exists(value))
                    type = "file";
                else if (Directory.Exists(value))
                    type = "dir";
                else if (Path.GetExtension(value) == ".tmp" || Path.GetExtension(value) == ".TMP")
                    type = "file";
                _path = value;
            }
            get
            {
                return _path;
            }
        }

        public void printData()
        {
            Console.WriteLine("******************************Create*********************************\n");
            Console.WriteLine("Type: " + type + "\n Path: " + path);
        }
    }

    class FSOPRenameVO
    {
        public String type;
        public String oldPath;
        private String _newPath;

        public FSOPRenameVO(String oldPath, String newPath)
        {
            this.newPath = newPath;
            this.oldPath = oldPath;
        }

        public String newPath
        {
            set
            {
                if (File.Exists(value))
                    type = "file";
                else
                    type = "dir";
                _newPath = value;
            }
            get
            {
                return _newPath;
            }
        }

        public void printData()
        {
            Console.WriteLine("***************************Rename**************************************\n");
            Console.WriteLine("Type: " + type + "\n Old Path: " + oldPath + "\n New Path: " + newPath);
        }
    }

    class FSOPDeleteVO
    {
        public String path;
        public bool isFolder;

        public FSOPDeleteVO(String path, bool isFolder)
        {
            this.path = path;
            this.isFolder = isFolder;
        }

        public void printData()
        {
            Console.WriteLine("**********************Delete****************************************\n");
            Console.WriteLine("Delete Path: " + path);
            String itemName = Path.GetFileName(path);
            Console.WriteLine("Delete Name: " + itemName);
        }
    }

    class FSOPChangeVO
    {
        public String path;
        public FSOPChangeVO(String path)
        {
            this.path = path;
        }
    }
}
#endif