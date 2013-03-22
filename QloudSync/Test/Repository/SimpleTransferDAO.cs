using System;
using System.Collections.Generic;
using GreenQloud.Repository.Model;
using GreenQloud.Persistence;

namespace GreenQloud.Test.SimpleRepository
{
    public class SimpleTransferDAO : TransferDAO
    {
        List<TransferResponse> list = new List<TransferResponse>();
        public SimpleTransferDAO ()
        {
        }

        #region implemented abstract members of TransferDAO

        public override void Create (TransferResponse transfer)
        {
            list.Add (transfer);
        }

        #endregion
    }
}

