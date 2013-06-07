using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using MonoMac.Foundation;

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
        private FSEventStreamCallback callback;
        Thread runLoop;
        IntPtr stream;
        public QloudSyncFileSystemWatcher(string pathwatcher)
        {
            ignoreBag = new ArrayList();
            string watchedFolder = pathwatcher   ;
            this.callback = this.Callback;

            IntPtr path = CFStringCreateWithCString (IntPtr.Zero, watchedFolder, 0);
            IntPtr paths = CFArrayCreate (IntPtr.Zero, new IntPtr [1] { path }, 1, IntPtr.Zero);
            
            stream = FSEventStreamCreate (IntPtr.Zero, this.callback, IntPtr.Zero, paths, FSEventStreamEventIdSinceNow, 0, FSEventStreamCreateFlags.WatchRoot | FSEventStreamCreateFlags.FileEvents);
            
            CFRelease (paths);
            CFRelease (path);
            
            runLoop = new Thread (delegate() {
                FSEventStreamScheduleWithRunLoop (stream, CFRunLoopGetCurrent (), kCFRunLoopDefaultMode);
                FSEventStreamStart (stream);
                CFRunLoopRun ();
            });
            runLoop.Name = "FSEventStream";
            runLoop.Start ();
        }
        
        public delegate void ChangedEventHandler (Event e);
        public event ChangedEventHandler Changed;

        public void Stop ()
        {
            runLoop.Abort();
            runLoop.Join(1000);
        }

        public void Block(string path){
            lock(_bagLock){
                ignoreBag.Add (path);
            }
        }

        public void Unblock(string path){
            lock(_bagLock){
                ignoreBag.Remove(path);
            }
        }

        private void Callback (IntPtr streamRef, IntPtr clientCallBackInfo, int numEvents, IntPtr eventPaths, IntPtr eventFlags, IntPtr eventIds)
        {

            string[] paths = new string[numEvents];
            FSEventStreamEventFlagItem[] flags = new FSEventStreamEventFlagItem[numEvents];
            UInt64[] ids = new UInt64[numEvents];
            unsafe {
                char** eventPathsPointer = (char**)eventPaths.ToPointer ();
                uint* eventFlagsPointer = (uint*)eventFlags.ToPointer ();
                ulong* eventIdsPointer = (ulong*)eventIds.ToPointer ();
                for (int i = 0; i < numEvents; i++) {
                    paths [i] = Marshal.PtrToStringAuto (new IntPtr (eventPathsPointer [i]));
                    flags [i] = (FSEventStreamEventFlagItem)eventFlagsPointer [i];
                    ids [i] = eventIdsPointer [i];
                }
            }
            ChangedEventHandler handler = Changed;

            for (int i = 0; i < numEvents; i++)
            {
                if(!paths[i].Substring(paths[i].LastIndexOf(Path.DirectorySeparatorChar)+1).StartsWith(".")){

                    string search = paths [i];
                    if(flags [i].HasFlag (FSEventStreamEventFlagItem.IsDir))
                        search += Path.DirectorySeparatorChar;

                    bool ignore = false;
                    lock(_bagLock){
                        ignore =  ignoreBag.Contains (search);
                    }

                    if (!ignore) {
                        if (//ignore
                            !flags [i].HasFlag (FSEventStreamEventFlagItem.InodeMetaMod) && //after create the event lister throw a chmod event
                            !(flags [i].HasFlag (FSEventStreamEventFlagItem.Created) && flags [i].HasFlag (FSEventStreamEventFlagItem.Modified)) //after create the event lister throw a create with update event
                        ) {
                            Event e = new Event ();
                            LocalRepository repo = repositoryDAO.GetRepositoryByItemFullName (paths[i]);
                            e.Item = RepositoryItem.CreateInstance (repo, paths [i], flags [i].HasFlag (FSEventStreamEventFlagItem.IsDir), 0, GlobalDateTime.NowUniversalString);

                            if (flags [i].HasFlag (FSEventStreamEventFlagItem.Created)) {
                                e.EventType = EventType.CREATE;
                            } else if (flags [i].HasFlag (FSEventStreamEventFlagItem.Removed)) {
                                e.EventType = EventType.DELETE;
                            }
                            if (flags [i].HasFlag (FSEventStreamEventFlagItem.Modified)) {
                                e.EventType = EventType.UPDATE;
                            } else if (flags [i].HasFlag (FSEventStreamEventFlagItem.Renamed)) {
                                if ((i + 1) < numEvents && (ids [i] == ids [i+1] - 1)) {
                                    e.EventType = EventType.MOVE;
                                    i++;
                                    e.Item.ResultObjectRelativePath = paths [i].Replace (e.Item.Repository.Path + Path.DirectorySeparatorChar, "");
                                } else if (flags [i].HasFlag (FSEventStreamEventFlagItem.IsDir) && !Directory.Exists (paths[i])) {
                                    e.EventType = EventType.DELETE;
                                } else if (flags [i].HasFlag (FSEventStreamEventFlagItem.IsFile) && !File.Exists (paths[i])) {
                                    e.EventType = EventType.DELETE;
                                } else {
                                    if (flags [i].HasFlag (FSEventStreamEventFlagItem.IsFile)) {
                                        Console.WriteLine (flags [i].ToString());
                                        e.EventType = EventType.UPDATE;
                                    }
                                }
                            }
                            Console.WriteLine (flags[i].ToString());
                            handler (e);
                        }
                    }
                }
            }
        }
        [DllImport ("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
        extern static IntPtr CFStringCreateWithCString (IntPtr allocator, string value, int encoding);
        
        [DllImport ("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
        extern static IntPtr CFArrayCreate (IntPtr allocator, IntPtr [] values, int numValues, IntPtr callBacks);
        
        [DllImport ("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
        extern static IntPtr CFArrayGetValueAtIndex (IntPtr array, int index);
        
        [DllImport ("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
        extern static void CFRelease (IntPtr cf);
        
        [DllImport ("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
        extern static IntPtr CFRunLoopGetCurrent ();
        
        [DllImport ("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
        extern static IntPtr CFRunLoopGetMain ();
        
        [DllImport ("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
        extern static void CFRunLoopRun ();
        
        [DllImport ("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
        extern static int CFRunLoopRunInMode (IntPtr mode, double seconds, int returnAfterSourceHandled);
        
        delegate void FSEventStreamCallback (IntPtr streamRef,IntPtr clientCallBackInfo,int numEvents,IntPtr eventPaths,IntPtr eventFlags,IntPtr eventIds);
        
        [DllImport ("/System/Library/Frameworks/CoreServices.framework/CoreServices")]
        extern static IntPtr FSEventStreamCreate (IntPtr allocator, FSEventStreamCallback callback, IntPtr context, IntPtr pathsToWatch, ulong sinceWhen, double latency, FSEventStreamCreateFlags flags);
        
        [DllImport ("/System/Library/Frameworks/CoreServices.framework/CoreServices")]
        extern static int FSEventStreamStart (IntPtr streamRef);
        
        [DllImport ("/System/Library/Frameworks/CoreServices.framework/CoreServices")]
        extern static void FSEventStreamStop (IntPtr streamRef);
        
        [DllImport ("/System/Library/Frameworks/CoreServices.framework/CoreServices")]
        extern static void FSEventStreamRelease (IntPtr streamRef);
        
        [DllImport ("/System/Library/Frameworks/CoreServices.framework/CoreServices")]
        extern static void FSEventStreamScheduleWithRunLoop (IntPtr streamRef, IntPtr runLoop, IntPtr runLoopMode);
        
        [DllImport ("/System/Library/Frameworks/CoreServices.framework/CoreServices")]
        extern static void FSEventStreamUnscheduleFromRunLoop (IntPtr streamRef, IntPtr runLoop, IntPtr runLoopMode);
        
        const ulong FSEventStreamEventIdSinceNow = ulong.MaxValue;
        private static IntPtr kCFRunLoopDefaultMode = CFStringCreateWithCString (IntPtr.Zero, "kCFRunLoopDefaultMode", 0);
        
        [Flags()]
        enum FSEventStreamCreateFlags : uint
        {
            None = 0x00000000,
            UseCFTypes = 0x00000001,
            NoDefer = 0x00000002,
            WatchRoot = 0x00000004,
            IgnoreSelf = 0x00000008,
            FileEvents = 0x00000010
        }
        
        [Flags()]
        enum FSEventStreamEventFlag : uint
        {
            None = 0x00000000,
            MustScanSubDirs = 0x00000001,
            UserDropped = 0x00000002,
            KernelDropped = 0x00000004,
            EventIdsWrapped = 0x00000008,
            HistoryDone = 0x00000010,
            RootChanged = 0x00000020,
            FlagMount  = 0x00000040,
            Unmount = 0x00000080
        }
        
        [Flags()]
        enum FSEventStreamEventFlagItem : uint
        {
            Created       = 0x00000100,
            Removed       = 0x00000200,
            InodeMetaMod  = 0x00000400,
            Renamed       = 0x00000800,
            Modified      = 0x00001000,
            FinderInfoMod = 0x00002000,
            ChangeOwner   = 0x00004000,
            XattrMod      = 0x00008000,
            IsFile        = 0x00010000,
            IsDir         = 0x00020000,
            IsSymlink     = 0x00040000
        }
    }
}