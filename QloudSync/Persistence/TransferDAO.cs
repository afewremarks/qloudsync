using System;
using System.Collections.Generic;
using GreenQloud.Repository;
using System.Linq;
using System.Xml;
using System.Threading;
using GreenQloud.Model;

namespace GreenQloud.Persistence
{
    public abstract class TransferDAO
    {
        public abstract void Create (Transfer transfer);
    }
}


