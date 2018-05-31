using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace undlc
{
    /// <summary>
    /// Basic Tools
    /// </summary>
    public static class Tools
    {
        /// <summary>
        /// Reads an Attribute
        /// </summary>
        /// <param name="A">Attribute</param>
        /// <returns>Value</returns>
        public static string Attr(XmlAttribute A)
        {
            return A == null ? string.Empty : A.Value;
        }

        /// <summary>
        /// Reads Content of a Node
        /// </summary>
        /// <param name="N">Node</param>
        /// <returns>Content</returns>
        public static string Val(XmlNode N)
        {
            return N == null ? string.Empty : N.InnerText;
        }

        /// <summary>
        /// Base64 Decode
        /// </summary>
        /// <param name="S">B64 String</param>
        /// <returns>Bytes</returns>
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

        /// <summary>
        /// Base64 Encode
        /// </summary>
        /// <param name="B">Bytes</param>
        /// <returns>B64 String</returns>
        public static string B64(byte[] B)
        {
            return (B == null || B.Length == 0) ? "" : Convert.ToBase64String(B);
        }

        /// <summary>
        /// String to Byte conversion using default codepage
        /// </summary>
        /// <param name="S">String</param>
        /// <returns>Byte</returns>
        public static byte[] ASC(string S)
        {
            return Encoding.Default.GetBytes(S);
        }

        /// <summary>
        /// Byte to string conversion using default codepage
        /// </summary>
        /// <param name="B">Bytes</param>
        /// <returns>String</returns>
        public static string ASC(byte[] B)
        {
            return Encoding.Default.GetString(B);
        }
    }
}
