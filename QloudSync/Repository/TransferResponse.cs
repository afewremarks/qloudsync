using System;
using GreenQloud.Repository;

namespace GreenQloud
{

    public enum TransferStatus{
        PENDING, DONE_WITH_ERROR, DONE
    }

    public enum TransferType{
        DOWNLOAD,
        UPLOAD,
        REMOVE,
        CREATEFOLDER
    }

    public class TransferResponse
    {

        public TransferResponse ()
        {

        }

        public TransferResponse (StorageQloudObject sqObject, TransferType type)
        {
            StorageQloudObject = sqObject;
            Type = type;
        }

        public StorageQloudObject StorageQloudObject{
            set; get;
        }
        
        public TransferType Type{
            set; get;
        }
       
        public long TotalSize {
            get {
                try{
                    if (Type == TransferType.UPLOAD)
                    {
                        System.IO.FileInfo f = new System.IO.FileInfo(StorageQloudObject.FullLocalName);
                        if (f.Exists)                    
                            return f.Length; 
                    }
                    else if(Type == TransferType.DOWNLOAD){
                        if(StorageQloudObject.AsS3Object != null)
                            return StorageQloudObject.AsS3Object.Size;

                    }
                }
                catch{
                    return 0;
                }
                return 0;
            }
        }

        public DateTime InitialTime{
            set; get;
        }

        public DateTime EndTime{
            set; get;
        }

        public double TransferredBits {
            set;get;
        }

        public double Percentage {
            get {
                if(TotalSize == 0){
                    return 100;
                }
                if (TransferredBits != 0){
                    return (TransferredBits / TotalSize)*100;
                }
                return 0;
            }
        }

        public double RemainingBits{
            get{
                if(TotalSize == 0)
                    return 0;
                return TotalSize - TransferredBits;
            }
        }

        public double RemainingTime {
            get {
                if(Speed == 0)
                    return 0;
                return RemainingBits/Speed;
            }
        }

        public double Speed {
            get {
                if(TimeElapsed.TotalMilliseconds==0)
                    return 0;
                return TransferredBits/TimeElapsed.TotalSeconds; 
            }
        }

        public TimeSpan TimeElapsed {
            get {
                if(Status == TransferStatus.DONE_WITH_ERROR)
                    return new TimeSpan (0);
                if (InitialTime == new DateTime())
                    return new TimeSpan (0);
                if (EndTime ==  new DateTime())
                    return DateTime.Now.Subtract(InitialTime);
                return EndTime.Subtract(InitialTime);
            }
        }

        public TransferStatus Status {
            set; get;
        }
    }
}

