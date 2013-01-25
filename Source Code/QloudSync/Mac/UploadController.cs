using System;

namespace QloudSync
{
    public class UploadController : SynchronizerController
    {
     
        private static UploadController instance;
        
        public static UploadController GetInstance(){
            if(instance == null)
                instance = new UploadController();
            return instance;
        }  


        #region implemented abstract members of SynchronizerController
        public override void Synchronize ()
        {

        }
        #endregion
    }
}

