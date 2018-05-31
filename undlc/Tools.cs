using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace undlc
{
    public static class Tools
    {
        public static string Attr(XmlAttribute A)
        {
            return A == null ? string.Empty : A.Value;
        }

        public static string Val(XmlNode N)
        {
            return N == null ? string.Empty : N.InnerText;
        }

        public static byte[] B64(string S)
        {
            if (string.IsNullOrEmpty(S))
            {
                return new byte[0];
            }
            S = S.Trim().Trim('\0');
            if (string.IsNullOrEmpty(S))
            {
                return new byte[0];
            }
            try
            {
                //Fast method
                return Convert.FromBase64String(S);
            }
            catch
            {
                //Slow method. Ignores errors
                var LB = new List<byte>();
                for (var i = 0; i < S.Length; i += 4)
                {
                    if (S.Length - i > 3)
                    {
                        LB.AddRange(Convert.FromBase64String(S.Substring(i, 4)));
                    }
                }
                return LB.ToArray();
            }
        }

        public static string B64(byte[] B)
        {
            return (B == null || B.Length == 0) ? "" : Convert.ToBase64String(B);
        }

        public static byte[] ASC(string S)
        {
            return Encoding.Default.GetBytes(S);
        }

        public static string ASC(byte[] B)
        {
            return Encoding.Default.GetString(B);
        }
    }
}
