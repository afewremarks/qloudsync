using System;
using NUnit.Framework;
using GreenQloud.Synchrony;
using System.IO;
using GreenQloud.Test.SimpleRepository;
using GreenQloud.Persistence;
using GreenQloud.Repository.Local;
using GreenQloud.Repository.Model;
using GreenQloud.Repository;
using System.Linq;
using System.Collections.Generic;

namespace GreenQloud.Test.SimpleRepository
{
    public class SimplePhysicalRepositoryController : PhysicalRepositoryController
	{
        public Dictionary<RepoObject, string> list = new Dictionary<RepoObject, string>();
        public List<string> names = new List<string>();

        public SimplePhysicalRepositoryController (SimpleLogicalRepositoryController logical)
        {
            throw new NotImplementedException ();
        }

        public SimplePhysicalRepositoryController (){
        }
       
        #region implemented abstract members of PhysicalRepositoryController
        public override void CreateOrUpdate (RepoObject remoteObj)
        {
            throw new NotImplementedException ();
        }

        public override bool Exists (RepoObject repoObject)
        {
            bool exists = list.Any(r=> r.Key.Name == repoObject.Name);
            return exists;
        }

        public override RepoObject CreateObjectInstance (string fullLocalName)
        {
           if (list.Any (o=> o.Key.FullLocalName == fullLocalName)){
                return list.First (o=> o.Key.FullLocalName == fullLocalName).Key;
           }
           return null;
        }

        public override List<string> FilesNames {
            get {
                List<string> templist = new List<string>();
                templist.AddRange (names);
                return templist;
            }
        }
        public override List<RepoObject> Files {
            get {
                throw new NotImplementedException ();
            }
        }

        public override void Delete (RepoObject repoObj)
        {
            if (list.Any (o=> o.Key.FullLocalName == repoObj.FullLocalName))
            {
                RepoObject temp = list.First (o=> o.Key.FullLocalName == repoObj.FullLocalName).Key;
                list.Remove (temp);
            }
            names.Remove (repoObj.FullLocalName);


        }

        #endregion

        public void Create (RepoObject repoObj)
        {
            Create (repoObj, "");
        }

        public void Create (RepoObject repoObj, string value){
            list.Add (repoObj, value);
            names.Add (repoObj.FullLocalName);
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

