using System;
using System.Collections.Generic;
using System.Linq;
using GreenQloud.Repository;
using GreenQloud.Repository.Model;
using GreenQloud.Repository.Local;
using System.IO;

namespace GreenQloud.Test.SimpleRepository
{
    public class SimpleLogicalRepositoryController : LogicalRepositoryController
    {
        Dictionary<RepoObject, string> list = new Dictionary<RepoObject, string>();
     
        public SimplePhysicalRepositoryController PhysicalController{
            set; get;
        }

        public SimpleLogicalRepositoryController (){
        }

        #region implemented abstract members of LogicalRepositoryController

        public override RepoObject CreateObjectInstance (string fullPath)
        {
            RepoObject repoObj = new RepoObject();
            repoObj.Name = "teste.html";
            repoObj.RelativePath = "home";
            repoObj.Repo = new LocalRepository("...");
            return repoObj;
        }

        public override void Solve (RepoObject remoteObj)
        {
            //se o arquivo nao existe
            if (!PhysicalController.Exists (remoteObj)){
                if (Exists(remoteObj))
                {
                    if(list.Any (o=>o.Key.FullLocalName==remoteObj.FullLocalName)){
                        RepoObject r = list.First (o=>o.Key.FullLocalName==remoteObj.FullLocalName).Key;
                        list.Remove (r);
                    }
                }
            }
            else{
                if (list.Any (o=> o.Key.FullLocalName == remoteObj.FullLocalName)){
                    RepoObject pk = PhysicalController.list.First (o=> o.Key.FullLocalName == remoteObj.FullLocalName).Key;
                    RepoObject lk = list.First (o=> o.Key.FullLocalName == remoteObj.FullLocalName).Key;
                    list[lk] = PhysicalController.list[pk];
                }
                else
                    Create (remoteObj);
            }
        }

        public override bool Exists (RepoObject repoObject)
        {
            return list.Any (r=> r.Key.Name == repoObject.Name);
        }

        public override List<string> FilesNames {
            get {
                throw new NotImplementedException ();
            }
        }

        public override List<RepoObject> Files {
            get {
                throw new NotImplementedException ();
            }
        }

        public override List<LocalRepository> LocalRepositories {
            get {
                throw new NotImplementedException ();
            }
            set {
                throw new NotImplementedException ();
            }
        }

        #endregion

        public void Create (RepoObject repoObj)
        {
            list.Add (repoObj, string.Empty);
        }

        public void Create (RepoObject repoObj, string value){
            list.Add (repoObj, value);
        }

        public object GetValue (string fullLocalName)
        {
            if (list.Any (o=> o.Key.FullLocalName==fullLocalName)){
                return list.First (o=> o.Key.FullLocalName==fullLocalName).Value;
            }
            return null;
        }
    }
}

