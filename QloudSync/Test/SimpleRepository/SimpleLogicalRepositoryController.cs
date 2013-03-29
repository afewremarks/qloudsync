using System;
using System.Collections.Generic;
using System.Linq;
using GreenQloud.Repository;
using GreenQloud.Model;
using GreenQloud.Repository.Local;
using System.IO;

namespace GreenQloud.Test.SimpleRepository
{
    public class SimpleLogicalRepositoryController : LogicalRepositoryController
    {
        Dictionary<RepositoryItem, string> list = new Dictionary<RepositoryItem, string>();
     
        public SimplePhysicalRepositoryController PhysicalController{
            set; get;
        }

        public SimpleLogicalRepositoryController (){
        }

        #region implemented abstract members of LogicalRepositoryController


        public override void Solve (RepositoryItem remoteObj)
        {
            //se o arquivo nao existe
            if (!PhysicalController.Exists (remoteObj)){
                if (Exists(remoteObj))
                {
                    if(list.Any (o=>o.Key.FullLocalName==remoteObj.FullLocalName)){
                        RepositoryItem r = list.First (o=>o.Key.FullLocalName==remoteObj.FullLocalName).Key;
                        list.Remove (r);
                    }
                }
            }
            else{
                if (list.Any (o=> o.Key.FullLocalName == remoteObj.FullLocalName)){
                    RepositoryItem pk = PhysicalController.list.First (o=> o.Key.FullLocalName == remoteObj.FullLocalName).Key;
                    RepositoryItem lk = list.First (o=> o.Key.FullLocalName == remoteObj.FullLocalName).Key;
                    list[lk] = PhysicalController.list[pk];
                }
                else
                    Create (remoteObj);
            }
        }

        public override bool Exists (RepositoryItem repoObject)
        {
            return list.Any (r=> r.Key.Name == repoObject.Name);
        }


        public override List<RepositoryItem> Items {
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

        public void Create (RepositoryItem  item)
        {
            list.Add ( item, string.Empty);
        }

        public void Create (RepositoryItem  item, string value){
            list.Add ( item, value);
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

