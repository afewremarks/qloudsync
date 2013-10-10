using System;
using System.IO;
using System.Globalization;
using System.Threading;

 namespace GreenQloud.Model
{
	public enum EventType{
        CREATE,
        UPDATE,
        DELETE,
        MOVE,
        COPY,
        NULL
    }

    public enum RepositoryType{
        LOCAL,
        REMOTE
    }

    public enum RESPONSE{
        NULL,
        OK,
        IGNORED,
        FAILURE
    }

	public class Event
	{
        public Event (LocalRepository repo)
		{
            this.Repository = repo;
            User = Credential.Username;
            Application = GlobalSettings.FullApplicationName;
            ApplicationVersion = GlobalSettings.RunningVersion;
            DeviceId = GlobalSettings.MachineName;
            OS = GlobalSettings.OSVersion;
            Bucket = RuntimeSettings.DefaultBucketName;
		}

        public int Id {
            get;
            set;
        }

        public int TryQnt {
            get;
            set;
        }

        public RESPONSE Response {
            get;
            set;
        }

        public int ItemId
        {
            get;
            set;
        }


        private RepositoryItem item;
        public RepositoryItem Item{
			set{
               item = value;
               if (repo != null)
                   ItemId = item.Id;
            } 
            get{
                return item;   
            }
		}

        public int RepositoryId
        {
            set;
            get;
        }

        private LocalRepository repo;
        public LocalRepository Repository{
            set {
                repo = value;
                if(repo != null)
                    RepositoryId = repo.Id;
            }
            get{
                return repo;
            }
        }

        public bool HaveResultItem {
            get {
                if (Item != null && Item.ResultItemId > 0 && 
                    (this.EventType != EventType.CREATE && this.EventType != EventType.UPDATE && this.EventType != EventType.NULL && this.EventType != EventType.DELETE))//TODO MOVE TO TRASH WILL GENERATE RESULT ITEM
                    return true;

                return false;
            }
        }

        public EventType EventType{
			set; get;
		}

        public RepositoryType RepositoryType{
            set; get;
        }

        public bool Synchronized{
            set; get;
        }

        public DateTime InsertTime {
            get;
            set;
        }

		public string User {
			set; get;
		}

        public string Application {
            get;
            set;
        }

        public string ApplicationVersion {
            get;
            set;
        }

        public string DeviceId {
            get;
            set;
        }

        public string OS {
            get;
            set;
        }

        public string Bucket {
            get;
            set;
        }

        public override string ToString(){
            string s = String.Format ("[{3}] {0} {1} {2}", EventType, RepositoryType, Item.LocalAbsolutePath,  Id);
            if(HaveResultItem){
                s += String.Format ("  Result Object: {0} \n",Item.ResultItem.LocalAbsolutePath);
            }

            return s;
        }

        public string ShortString
        {
            get {
                CultureInfo cultureInfo = Thread.CurrentThread.CurrentCulture;
                TextInfo textInfo = cultureInfo.TextInfo;
                return String.Format ("{0} - {1}", textInfo.ToTitleCase (EventType.ToString ().ToLower ()), (HaveResultItem ? Item.ResultItem.Key : Item.Key));
            }
        }
        public string ItemName
        {
            get{
                try{
                    return (HaveResultItem ? Item.ResultItem.Name : Item.Name);
                } catch {
                    return "";
                }
            }
        }

        public string ItemLocalFolderPath
        {
            get{
                try{
                    return (HaveResultItem ? Item.ResultItem.LocalFolderPath : Item.LocalFolderPath);
                } catch {
                    return "";
                }
            }
        }

        public string ItemUpdatedAt
        {
            get{
                try{
                    DateTime updatedAt = DateTime.ParseExact((HaveResultItem ? Item.ResultItem.UpdatedAt : Item.UpdatedAt), "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'", System.Globalization.CultureInfo.InvariantCulture);
                    String s = GlobalDateTime.ToHumanReadableString(GlobalDateTime.Now.Subtract(updatedAt));
                    return s;
                } catch {
                    return "";
                }
            }
        }

        public ItemType ItemType
        {
            get{
               return (HaveResultItem ? Item.ResultItem.Type : Item.Type);
            }
        }
	}
}

