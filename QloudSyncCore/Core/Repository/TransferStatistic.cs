using System;

namespace GreenQloud.Repository
{
    public enum TransferType{
        UP,
        DOWN
    }
    public class TransferStatistic
    {
        public string Key {
            get; set;
        }
        public TransferType TransferType {
            get; set;
        }

        public long BytesTotal {
            get;
            set;
        }

        public long BytesTransferred {
            get;
            set;
        }

        public int ProgressPercentage {
            get;
            set;
        }

        public TransferStatistic (string key, TransferType type)
        {
            Key = key;
            TransferType = type;
        }

        public override string ToString()
        {
            return "(" + ProgressPercentage + "%) " + Key;  
        }
    }
}

