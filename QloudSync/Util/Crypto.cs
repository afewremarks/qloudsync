using System;
using System.Text;
using System.Security.Cryptography;

namespace GreenQloud
{
    public class Crypto
    {
        public static string GetHMACbase64(string secretkey, string url, bool urlEncode)
        {
            byte[] key = new Byte[64];
            string b64 = null;
            key = Encoding.UTF8.GetBytes(secretkey);
            HMACSHA1 myhash1 = new HMACSHA1(key);
            byte[] urlbytes = Encoding.UTF8.GetBytes(url);         
            byte[] hashValue = myhash1.ComputeHash(urlbytes);
            b64 = Convert.ToBase64String(hashValue);
            
            if (urlEncode)            
                return new UrlEncode().Encode(b64);
            else                            
                return b64;
        }

        public static string Getbase64(string value)
        {
            byte[] key = new Byte[64];
            key = Encoding.UTF8.GetBytes(value);
            return Convert.ToBase64String(key);
        }
    }
}

