using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Xml;

namespace undlc
{
    /// <summary>
    /// DLC file Decrypter
    /// </summary>
    public class DlcContainer
    {
        /// <summary>
        /// URL of the service that gives us the proper URL
        /// </summary>
        /// <remarks>Yes, if this URL is ever unavailable, all DLC containers will break</remarks>
        private const string GETKEY_URL = "http://service.jdownloader.org/dlcrypt/service.php?srcType=dlc&destType=pylo&data=";
        /// <summary>
        /// Regular expression to extract the key.
        /// </summary>
        /// <remarks>The key is a single XML node "rc" with the B64 key as content</remarks>
        private const string KEY_REGEX = @"<rc>([^<]+)<\/rc>";
        /// <summary>
        /// Key for AES decryption
        /// </summary>
        /// <remarks>Don't be fooled by this. Do not convert to a byte array</remarks>
        private const string AES_KEY =
            "" + "c" + "b" + "9" + "9" +
            "" + "b" + "5" + "c" + "b" +
            "" + "c" + "2" + "4" + "d" +
            "" + "b" + "3" + "9" + "8";
        /// <summary>
        /// IV for AES decryption
        /// </summary>
        /// <remarks>Ditto <see cref="AES_KEY"/></remarks>
        private const string AES_IV =
            "" + "9" + "b" + "c" + "2" +
            "" + "4" + "c" + "b" + "9" +
            "" + "9" + "5" + "c" + "b" +
            "" + "8" + "d" + "b" + "3";
        /// <summary>
        /// Size of the B64 key in the DLC File
        /// </summary>
        private const int DLC_KEYSIZE = 88;

        /// <summary>
        /// The Key that is contained in the file as B64
        /// </summary>
        private string SourceKey;
        /// <summary>
        /// The encrypted container data as B64
        /// </summary>
        private string SourceData;
        /// <summary>
        /// The real key to decrypt <see cref="SourceData"/>
        /// </summary>
        private byte[] RealKey;
        /// <summary>
        /// The real IV to decrypt <see cref="SourceData"/>
        /// </summary>
        private byte[] RealIV;
        /// <summary>
        /// Unprocessed but decrypted DLC content
        /// </summary>
        /// <remarks>This is XML</remarks>
        private string DlcContent;

        /// <summary>
        /// Header of the DLC File
        /// </summary>
        public DlcHeader Header { get; private set; }
        /// <summary>
        /// Body of the DLC File
        /// </summary>
        public DlcContent Content { get; private set; }

        /// <summary>
        /// Loads and decrypts a DLC container
        /// </summary>
        /// <param name="FileContent">DLC container content</param>
        public DlcContainer(string FileContent)
        {
            if (FileContent != null && FileContent.Length > DLC_KEYSIZE)
            {
                //DLC files are a concatenation of content and key.
                //The key is always the same length though
                SourceKey = FileContent.Substring(FileContent.Length - DLC_KEYSIZE);
                SourceData = FileContent.Substring(0, FileContent.Length - DLC_KEYSIZE);
            }
            else
            {
                throw new ArgumentException("Invalid DLC Content");
            }
            //Gets the real key from the JD Server
            try
            {
                RealKey = Tools.B64(GetKey(SourceKey));
            }
            catch (Exception ex)
            {
                throw new Exception("Unable to obtain Key from API. Please check your internet connection", ex);
            }
            //As usual, Container developers have not the slightest clue how to safely encrypt/decrypt.
            //In this case the Key and IV are hardcoded
            try
            {
                RealIV = AesDecrypt(RealKey, Tools.ASC(AES_KEY), Tools.ASC(AES_IV));
            }
            catch (Exception ex)
            {
                throw new Exception("Unable to decrypt Server response. It's either invalid or the keys have changed.", ex);
            }
            //Continuing this laughable trail of fails, the IV is also the key
            try
            {
                //B64Decode --> Decrypt --> B64Decode --> ByteToString
                DlcContent = Tools.ASC(Tools.B64(Tools.ASC(AesDecrypt(Tools.B64(SourceData), RealIV, RealIV))));
            }
            catch (Exception ex)
            {
                throw new Exception("Unable to decrypt the Key content.", ex);
            }
            //The response is XML
            var D = new XmlDocument();
            try
            {
                D.LoadXml(DlcContent);
            }
            catch (Exception ex)
            {
                throw new Exception("Unable to interpret the decrypted DLC content as XML", ex);
            }
            ReadHeader(D.DocumentElement["header"]);
            ReadBody(D.DocumentElement["content"]);
        }

        /// <summary>
        /// Reads the Header of the DLC file
        /// </summary>
        /// <param name="XmlHeader">XML "header" Node</param>
        private void ReadHeader(XmlNode XmlHeader)
        {
            Header = new DlcHeader(XmlHeader);
        }

        /// <summary>
        /// Reads the Body of the DLC file
        /// </summary>
        /// <param name="XmlContent">XML "content" Node</param>
        private void ReadBody(XmlNode XmlContent)
        {
            Content = new DlcContent(XmlContent);
        }

        /// <summary>
        /// Decrypts AES-CBC-128 encrypted content
        /// </summary>
        /// <param name="Content">Content to decrypt</param>
        /// <param name="Key">Key</param>
        /// <param name="IV">IV</param>
        /// <returns>Decrypted content</returns>
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

        /// <summary>
        /// Gets the real Key from the JS servers
        /// </summary>
        /// <param name="DlcKey">Fake Key</param>
        /// <returns>Real Key</returns>
        private static string GetKey(string DlcKey)
        {
            using (var WC = new WebClient())
            {
                var K = WC.DownloadString(GETKEY_URL + DlcKey);
                if (!string.IsNullOrEmpty(K))
                {
                    var Match = Regex.Match(K, KEY_REGEX);
                    if (Match != null)
                    {
                        return Match.Groups[1].Value;
                    }
                    throw new Exception("Invalid Key: " + K);
                }
                throw new Exception("Got an empty response from the server");
            }
        }
    }
}
