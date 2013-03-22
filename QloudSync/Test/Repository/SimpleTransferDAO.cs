using System;
using System.Collections.Generic;
using GreenQloud.Model;
using GreenQloud.Persistence;

namespace GreenQloud.Test.SimpleRepository
{
    public class SimpleTransferDAO : TransferDAO
    {
        List<Transfer> list = new List<Transfer>();
        public SimpleTransferDAO ()
        {
        }

        #region implemented abstract members of TransferDAO

        public override void Create (Transfer transfer)
        {
            list.Add (transfer);
        }

        #endregion
    }
}

