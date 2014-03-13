using System;
using System.Net;
using System.Configuration;
using System.IO;
using System.Collections.Specialized;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using GreenQloud.Util;
using GreenQloud.Repository;
using System.Text;

namespace GreenQloud
{
	class DisconnectionException : Exception
	{
        public DisconnectionException (): base("Lost connection"){
            Logger.LogInfo ("ERROR ON CONNECTION", this.Message);
        }
	}

}