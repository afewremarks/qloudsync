using System;
using System.Text;
using System.Security.Cryptography;
using GreenQloud.Model;
using System.IO;

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

        public string md5hash (string input)
        {
            System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();
            byte[] data = md5.ComputeHash(System.Text.Encoding.Default.GetBytes(input));
            System.Text.StringBuilder sbString = new System.Text.StringBuilder();
            for (int i = 0; i < data.Length; i++)
                sbString.Append(data[i].ToString("x2"));
            return sbString.ToString();
        }

        
        public string md5hash (RepositoryItem item)
        {
            string md5hash;
            try {
                FileStream fs = System.IO.File.Open (item.FullLocalName, FileMode.Open);
                System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create ();
                md5hash = BitConverter.ToString (md5.ComputeHash (fs)).Replace (@"-", @"").ToLower ();
                fs.Close ();
            }
            catch (Exception e){
                md5hash = string.Empty;
            }
            return md5hash;
        }
    }
}

