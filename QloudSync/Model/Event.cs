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
		}

        public string ResultObject {
            get;
            set;
        }
        /*public string RelativeResultObject{
            get {
                return Path.Combine(Item.RelativePath, ResultObject);
            }
        }*/
        public string FullLocalResultObject{
            get {
                return Path.Combine(Item.Repository.Path, ResultObject);
            }
        }

        public RepositoryItem Item{
			set; get;
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

