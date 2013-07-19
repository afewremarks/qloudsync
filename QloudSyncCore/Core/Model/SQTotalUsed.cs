using System;
using System.Net;
using System.IO;
using System.Text;
using System.Threading;
using Newtonsoft.Json.Linq;

namespace GreenQloud.Model
{
    public class SQTotalUsed
    {
        string saved;
        public string Spent {
            get {
                return saved;
            }
            set {
                int v;
                v = int.Parse(value.Split('.')[0]);
                if (v < Math.Pow(1024,2)) {
                    saved = ((float)v / 1024).ToString ("F1") + "KB";
                } else if (v < Math.Pow(1024,3)) {
                    saved = ((float)v / Math.Pow(1024,2)).ToString ("F1") + "MB";
                } else {
                    saved = ((float)v / Math.Pow(1024,3)).ToString ("F1") + "GB";
                }
             }
        }

        public string Used {
            get;
            set;
        }
    }

}
