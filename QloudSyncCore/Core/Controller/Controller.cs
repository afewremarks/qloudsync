using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using GreenQloud.Synchrony;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using GreenQloud.Model;

 

namespace GreenQloud {
    public interface ApplicationController {
        void Initialize ();
        void HandleDisconnection();
        void HandleError(LocalRepository repo);
        void HandleSyncStatusChanged();
        bool DatabaseLoaded();
        void HandleDownloading(RepositoryItem item);
    }
}
