using System;
using NUnit.Framework;
using GreenQloud.Synchrony;
using System.IO;
using GreenQloud.Test.SimpleRepository;
using GreenQloud.Persistence;
using GreenQloud.Repository.Local;
using GreenQloud.Model;
using GreenQloud.Repository;
using System.Linq;
using System.Collections.Generic;

namespace GreenQloud.Test.SimpleRepository
{
    public class SimplePhysicalRepositoryController : PhysicalRepositoryController
	{
        public Dictionary<RepositoryItem, string> list = new Dictionary<RepositoryItem, string>();
       
        public SimplePhysicalRepositoryController (SimpleLogicalRepositoryController logical)
        {
            throw new NotImplementedException ();
        }

        public SimplePhysicalRepositoryController (){
        }
       
        #region implemented abstract members of PhysicalRepositoryController
        public override void CreateOrUpdate (RepositoryItem remoteObj)
        {
            throw new NotImplementedException ();
        }

        public override bool Exists (RepositoryItem repoObject)
        {
            bool exists = list.Any(r=> r.Key.Name == repoObject.Name);
            return exists;
        }

        public override RepositoryItem CreateObjectInstance (string fullLocalName)
        {
           if (list.Any (o=> o.Key.FullLocalName == fullLocalName)){
                return list.First (o=> o.Key.FullLocalName == fullLocalName).Key;
           }
           return null;
        }

        public override List<RepositoryItem> Files {
            get {
                List<RepositoryItem> templist = new List<RepositoryItem>();
                templist.AddRange (list.Keys);
                return templist;
            }
        }

        public override void Delete (RepositoryItem repoObj)
        {
            if (list.Any (o=> o.Key.FullLocalName == repoObj.FullLocalName))
            {
                RepositoryItem temp = list.First (o=> o.Key.FullLocalName == repoObj.FullLocalName).Key;
                list.Remove (temp);
            }
        }

        #endregion

        public void Create (RepositoryItem repoObj)
        {
            Create (repoObj, "");
        }

        public void Create (RepositoryItem repoObj, string value){
            list.Add (repoObj, value);
        }

        public string GetValue (string fullLocalName)
        {
            if (list.Any (o=> o.Key.FullLocalName==fullLocalName)){
                string value = list.First (o=> o.Key.FullLocalName==fullLocalName).Value;
                return value;
            }
            return null;
        }
    }

}

