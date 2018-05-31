using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Xml;

namespace undlc
{
    public class DlcContainer
    {
        private const string GETKEY_URL = "http://service.jdownloader.org/dlcrypt/service.php?srcType=dlc&destType=pylo&data=";
        private const string KEY_REGEX = @"<rc>([^<]+)<\/rc>";
        private const string AES_KEY = "cb99b5cbc24db398";
        private const string AES_IV = "9bc24cb995cb8db3";

        private string SourceKey;
        private string SourceData;
        private byte[] RealKey;
        private byte[] RealIV;
        private string DlcContent;

        public DlcHeader Header;
        public DlcContent Content;

        public DlcContainer(string FileContent)
        {
            if (FileContent != null && FileContent.Length > 88)
            {
                SourceKey = FileContent.Substring(FileContent.Length - 88);
                SourceData = FileContent.Substring(0, FileContent.Length - 88);
            }
            else
            {
                throw new ArgumentException("Invalid DLC Content");
            }
            RealKey = Tools.B64(GetKey(SourceKey));
            RealIV = AesDecrypt(RealKey, Tools.ASC(AES_KEY), Tools.ASC(AES_IV));
            DlcContent = Tools.ASC(Tools.B64(Tools.ASC(AesDecrypt(Tools.B64(SourceData), RealIV, RealIV))));
            var D = new XmlDocument();
            D.LoadXml(DlcContent);
            ReadHeader(D.DocumentElement["header"]);
            ReadBody(D.DocumentElement["content"]);
        }

        private void ReadHeader(XmlNode XmlHeader)
        {
            Header = new DlcHeader(XmlHeader);
        }

        private void ReadBody(XmlNode XmlContent)
        {
            Content = new DlcContent(XmlContent);
        }

        private static byte[] AesDecrypt(byte[] Content, byte[] Key, byte[] IV)
        {
            using (var AES = Rijndael.Create())
            {
                AES.BlockSize = 128;
                AES.KeySize = 128;
                AES.Mode = CipherMode.CBC;
                AES.Padding = PaddingMode.Zeros;
                AES.IV = IV;
                AES.Key = Key;
                using (var DEC = AES.CreateDecryptor())
                {
                    return DEC.TransformFinalBlock(Content, 0, Content.Length);
                }
            }
        }

        private static string GetKey(string DlcKey)
        {
#if DEBUG
            if (File.Exists(@"C:\temp\dlc.xml"))
            {
                return Regex.Match(File.ReadAllText(@"C:\temp\dlc.xml"), KEY_REGEX).Groups[1].Value;
            }
#endif
            using (var WC = new WebClient())
            {
                var K = WC.DownloadString(GETKEY_URL + DlcKey);
                var Match = Regex.Match(K, KEY_REGEX);
                if (Match != null)
                {

#if DEBUG
                    File.WriteAllText(@"C:\temp\dlc.xml", K);
#endif
                    return Match.Groups[1].Value;
                }
                throw new Exception("Invalid Key: " + K);
            }
        }
    }
}
