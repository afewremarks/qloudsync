using System;

namespace QloudSync
{
    public class BacklogController 
    {
        private static BacklogController instance;

        public static BacklogController GetInstance(){
            if(instance == null)
                instance = new BacklogController();
            return instance;
        }
    }
}

