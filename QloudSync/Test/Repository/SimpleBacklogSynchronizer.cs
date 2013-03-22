using System;
using GreenQloud.Repository.Remote;
using GreenQloud.Repository;
using GreenQloud.Persistence;
using GreenQloud.Repository.Local;
using GreenQloud.Repository.Model;
using System.Collections.Generic;

namespace GreenQloud.Test.SimpleRepository
{
    public class SimpleBacklogSynchronizer : GreenQloud.Synchrony.BacklogSynchronizer
    {
        public List<RepoObject> list = new List<RepoObject>();

        public SimpleBacklogSynchronizer (LogicalRepositoryController logical, PhysicalRepositoryController physical, RemoteRepositoryController remote, TransferDAO transfers) : base (logical, physical, remote, transfers)
        {
        }

        #region implemented abstract members of Synchronizer

        public override void Start ()
        {
            throw new NotImplementedException ();
        }

        public override void Pause ()
        {
            throw new NotImplementedException ();
        }

        public override void Stop ()
        {
            throw new NotImplementedException ();
        }

        #endregion
    }
}

