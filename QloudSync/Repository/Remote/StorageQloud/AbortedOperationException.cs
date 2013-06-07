using System;
using GreenQloud.Model;
using System.Collections.Generic;
using Amazon.S3.Model;
using Amazon.S3;
using System.Net;
using System.IO;
using System.Text;
using System.Linq;
using GreenQloud.Util;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;

namespace GreenQloud
{
    //TODO refactor md5 to eTag.. missmatch names
	class AbortedOperationException : Exception
	{
        public AbortedOperationException(string cause){
            super ();
            cause = cause;
        }
	}
}

