using Amazon.S3.Model;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.Threading;

using GreenQloud.Model;
using GreenQloud.Repository;
using GreenQloud.Util;
using GreenQloud.Persistence;
using GreenQloud.Repository.Local;
using GreenQloud.Repository.Remote;

namespace GreenQloud.Synchrony
{
    
    public abstract class AbstractSynchronizer
    {

        protected TransferDAO transferDAO;
        protected EventDAO eventDAO;
        protected RepositoryItemDAO repositoryItemDAO;
        protected LogicalRepositoryController logicalLocalRepository;
        protected PhysicalRepositoryController physicalLocalRepository;
        protected RemoteRepositoryController remoteRepository;
        
        public bool Done {
            set; get;
        }

        public bool Working{
            set; get;
        }
        
        protected AbstractSynchronizer 
            (LogicalRepositoryController logicalLocalRepository, PhysicalRepositoryController physicalLocalRepository, 
             RemoteRepositoryController remoteRepository, TransferDAO transferDAO, EventDAO eventDAO, RepositoryItemDAO repositoryItemDAO)
        {
            this.transferDAO = transferDAO;
            this.eventDAO = eventDAO;
            this.logicalLocalRepository = logicalLocalRepository;
            this.physicalLocalRepository = physicalLocalRepository;
            this.remoteRepository = remoteRepository;
            this.repositoryItemDAO = repositoryItemDAO;
        }

        #region Abstract Methods

        public abstract void Start ();
        public abstract void Pause ();
        public abstract void Stop ();

        #endregion
    }
}