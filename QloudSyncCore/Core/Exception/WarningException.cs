using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GreenQloud
{
    public class WarningException : Exception
    {
        public WarningException(string p) : base(p)
        {
            
        }
    }
}
