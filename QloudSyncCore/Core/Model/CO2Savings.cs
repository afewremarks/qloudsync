using System;
using System.Net;
using System.IO;
using System.Text;
using System.Threading;
using Newtonsoft.Json.Linq;

namespace GreenQloud.Model
{
    public class CO2Savings
    {
        string saved;
        public string Saved {
            get {
                return saved;
            }
            set {
                int v;
                v = int.Parse(value.Split('.')[0]);
                if (v <= 1000) {
                    saved = v.ToString () + "g";
                } else {
                    saved = ((float)v/1000).ToString("F1") + "Kg";
                }
            }
        }

        public string Used {
            get;
            set;
        }
    }

}

