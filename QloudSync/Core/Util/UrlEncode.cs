using System;
using System.Text;
using System.Security.Cryptography;
using System.Web;

namespace GreenQloud
{
	public class UrlEncode
	{
        private readonly static string reservedCharacters = "!*'();:@&=+$,/?%#[]";
        public string Encode (string value)
        {
            if (String.IsNullOrEmpty(value))
                return String.Empty;

            var sb = new StringBuilder();

            foreach (char @char in value)
                {
                    if (reservedCharacters.IndexOf(@char) == -1)
                        sb.Append(@char);
                    else
                        sb.AppendFormat("%{0:X2}", (int)@char);
                }
                return sb.ToString();
        }
    }
}

