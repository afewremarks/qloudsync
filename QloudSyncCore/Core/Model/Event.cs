using System;
using System.IO;

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
		public Event ()
		{
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

        public RepositoryItem Item{
			set; get;
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

        public string InsertTime {
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
            string s = String.Format ("[{3}] {0} {1} {2}", EventType, RepositoryType, Item.LocalAbsolutePath, Id);
            if(HaveResultItem){
                s += String.Format ("  Result Object: {0} \n",Item.ResultItem.LocalAbsolutePath);
            }

            return s;
        }

        public string ShortString ()
        {
            return String.Format ("{0} - {1}", EventType,  (HaveResultItem ? Item.ResultItem.Key : Item.Key));
        }
	}
}

