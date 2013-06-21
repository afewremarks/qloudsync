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

        public RepositoryItem Item{
			set; get;
		}

        public bool HaveResultItem {
            get {
                if (Item != null && Item.ResultItem != null && 
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
	}
}

