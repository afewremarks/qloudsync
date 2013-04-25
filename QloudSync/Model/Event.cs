using System;

 namespace GreenQloud.Model
{
	public enum EventType{
        CREATE,
        UPDATE,
        DELETE,
        MOVE_OR_RENAME,
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

