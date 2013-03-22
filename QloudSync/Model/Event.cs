using System;

 namespace GreenQloud.Model
{
	public enum EventType{
        CREATE,
        UPDATE,
        DELETE,
        MOVE_OR_RENAME
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

		public string User {
			set; get;
		}
	}
}

