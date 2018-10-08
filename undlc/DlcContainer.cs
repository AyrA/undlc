using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
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
        /// URL of the service that gives us the proper key
        /// </summary>
        /// <remarks>Yes, if this URL is ever unavailable, all DLC containers will break</remarks>
        private const string GETKEY_URL = "http://service.jdownloader.org/dlcrypt/service.php?srcType=dlc&destType=pylo&data=";
        /// <summary>
        /// URL of the service that registers the key
        /// </summary>
        private const string SETKEY_URL = "http://service.jdownloader.org/dlcrypt/service.php?jd=1&srcType=plain&data=";
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
                //DLC files are a concatenation of content + key.
                //The key is always the same length though
                SourceKey = FileContent.Substring(FileContent.Length - DLC_KEYSIZE);
                SourceData = FileContent.Substring(0, FileContent.Length - DLC_KEYSIZE);
            }
            else
            {
                throw new ArgumentException("Invalid DLC Content");
            }
            //Gets the real 16 bytes key from the JD Server
            try
            {
                RealKey = Tools.B64(GetKey(SourceKey));
            }
            catch (Exception ex)
            {
                throw new Exception("Unable to obtain Key from API. Please check your internet connection", ex);
            }
            //As usual, Container developers have not the slightest clue how to safely encrypt/decrypt.
            //In this case the Key and IV are hardcoded.
            //A software doesn't needs these keys to create a DLC since the encryption step is done on the server.
            //Because of how shitty this has been set up, they can't easily change the key since this method lacks
            //any sort of versioning of the encrypted data.
            //If they change the keys, all existing containers would become invalid.
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
                var AES = Tools.ASC(AesDecrypt(Tools.B64(SourceData), RealIV, RealIV));
                DlcContent = Tools.ASC(Tools.B64(AES));
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
            //Read XML Data
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
        /// Decrypts the IV
        /// </summary>
        /// <param name="IV">Encrypted IV Data</param>
        /// <returns>Decrypted IV Data</returns>
        private static byte[] DecryptIV(byte[] IV)
        {
            return AesDecrypt(IV, Tools.ASC(AES_KEY), Tools.ASC(AES_IV));
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
        /// Encrypts AES-CBC-128 encrypted content
        /// </summary>
        /// <param name="Content">Content to encrypt</param>
        /// <param name="Key">Key</param>
        /// <param name="IV">IV</param>
        /// <returns>Encrypted content</returns>
        private static byte[] AesEncrypt(byte[] Content, byte[] Key, byte[] IV)
        {
            using (var AES = Rijndael.Create())
            {
                AES.BlockSize = 128;
                AES.KeySize = 128;
                AES.Mode = CipherMode.CBC;
                AES.Padding = PaddingMode.Zeros;
                AES.IV = IV;
                AES.Key = Key;
                using (var ENC = AES.CreateEncryptor())
                {
                    return ENC.TransformFinalBlock(Content, 0, Content.Length);
                }
            }
        }

        /// <summary>
        /// Gets the real Key from the JD servers
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
                    throw new Exception("Invalid Key value: " + K);
                }
                throw new Exception("Got an empty response from the server");
            }
        }

        /// <summary>
        /// Sends a Key to the JD servers
        /// </summary>
        /// <param name="DlcKey">Unaltered Key</param>
        /// <returns>Replacement Key/Token</returns>
        /// <remarks>The Key is encrypted server side. <see cref="GetKey"/> returns the encrypted key</remarks>
        private static string SetKey(byte[] DlcKey)
        {
            var RealKey = Tools.B2S(DlcKey);
            using (var WC = new WebClient())
            {
                var K = WC.DownloadString(SETKEY_URL + RealKey);
                if (!string.IsNullOrEmpty(K))
                {
                    var Match = Regex.Match(K, KEY_REGEX);
                    if (Match != null)
                    {
                        Console.Error.WriteLine(K);
                        return Match.Groups[1].Value;
                    }
                    throw new Exception("Invalid Key value: " + K);
                }
                throw new Exception("Got an empty response from the server");
            }
        }

        /// <summary>
        /// Creates a DLC file from the given URL List
        /// </summary>
        /// <remarks>This is unreliable if the last segment of the URL isn't the obvious file name</remarks>
        /// <param name="FileLinks">URL List</param>
        /// <param name="PackageName">Package Name</param>
        /// <param name="Comment">Package Comment</param>
        /// <returns>DLC string</returns>
        public static string CreateDlc(IEnumerable<string> FileLinks, string PackageName, string Comment)
        {
            return CreateDlc(FileLinks.Select(m => new DlcFile()
            {
                Filename = (new Uri(m)).Segments.Last(),
                URL = m,
                Size = 0
            }), PackageName, Comment);
        }

        /// <summary>
        /// Creates a DLC file from the given File list
        /// </summary>
        /// <param name="FileLinks">List of Files</param>
        /// <param name="PackageName">Package Name</param>
        /// <param name="Comment">Package Comment</param>
        /// <returns>DLC string</returns>
        public static string CreateDlc(IEnumerable<DlcFile> FileLinks, string PackageName, string Comment)
        {
            return CreateDlc(FileLinks, new DlcHeader()
            {
                Generator = new DlcGenerator()
                {
                    App = "undlc",
                    Version = "undlc",
                    Url = Assembly.GetExecutingAssembly().GetName().Version.ToString()
                },
                Tribute = new DlcTribute() { Name = "AyrA" },
                Version = "20_02_2008"
            }, PackageName, Comment);
        }

        /// <summary>
        /// Creates a DLC file from the given File list
        /// </summary>
        /// <param name="FileLinks">List of Files</param>
        /// <param name="Header">Custom DLC Header</param>
        /// <param name="PackageName">Package Name</param>
        /// <param name="Comment">Package Comment</param>
        /// <returns>DLC string</returns>
        public static string CreateDlc(IEnumerable<DlcFile> FileLinks, DlcHeader Header, string PackageName, string Comment)
        {
            if (FileLinks.Count() > 0)
            {
                var XML = BuildXML(FileLinks, Header, PackageName, Comment);
                var RealKey = RandomKey(8);
                var FakeKey = SetKey(RealKey);
                var Encrypted = Tools.B64(AesEncrypt(Tools.ASC(Tools.B64(Tools.ASC(XML))), Tools.ASC(Tools.B2S(RealKey)), Tools.ASC(Tools.B2S(RealKey))));
                return Encrypted + FakeKey;
            }
            return null;
        }

        /// <summary>
        /// Builds 
        /// </summary>
        /// <param name="FileLinks">List of Files</param>
        /// <param name="Header">DLC Header</param>
        /// <param name="PackageName">Package Name</param>
        /// <param name="Comment">Package Comment</param>
        /// <returns>XML string</returns>
        private static string BuildXML(IEnumerable<DlcFile> FileLinks, DlcHeader Header, string PackageName, string Comment)
        {
            if (string.IsNullOrEmpty(PackageName))
            {
                Comment = "unknown";
            }
            if (string.IsNullOrEmpty(Comment))
            {
                Comment = PackageName;
            }
            //Build XML Document
            var d = new XmlDocument();
            d.LoadXml("<dlc><header></header><content><package></package></content></dlc>");

            //XML Generator Info
            var Generator = d.CreateElement("generator");
            Generator.AppendChild(CE(d, "app", Tools.B64(Tools.ASC(Header.Generator.App))));
            Generator.AppendChild(CE(d, "version", Tools.B64(Tools.ASC(Header.Generator.Version))));
            Generator.AppendChild(CE(d, "url", Tools.B64(Tools.ASC(Header.Generator.Url))));

            //Build Header
            d["dlc"]["header"].AppendChild(Generator);
            d["dlc"]["header"].AppendChild(CE(d, "tribute", $"<name>{Tools.B64(Tools.ASC(Header.Tribute.Name))}</name>"));
            d["dlc"]["header"].AppendChild(CE(d, "dlcxmlversion", Tools.ASC(Tools.B64(Header.Version))));

            var P = d["dlc"]["content"]["package"];
            P.SetAttribute("name", Tools.B64(Tools.ASC(PackageName)));
            P.SetAttribute("comment", Tools.B64(Tools.ASC(Comment)));
            P.SetAttribute("passwords", "e30=");
            P.SetAttribute("category", "dmFyaW91cw==");

            foreach (var File in FileLinks)
            {
                var F = d.CreateElement("file");
                F.AppendChild(CE(d, "url", Tools.B64(Tools.ASC(File.URL))));
                F.AppendChild(CE(d, "filename", Tools.B64(Tools.ASC(File.Filename))));
                F.AppendChild(CE(d, "size", Tools.B64(Tools.ASC(File.Size.ToString()))));
                P.AppendChild(F);
            }

            return d.DocumentElement.OuterXml;
        }

        /// <summary>
        /// Creates an XML Element and sets its contents
        /// </summary>
        /// <param name="d">Document of new Element</param>
        /// <param name="Name">Element Name</param>
        /// <param name="Content">Element Content</param>
        /// <param name="Encode">Encode content to not be interpreted as XML</param>
        /// <returns>XML Element</returns>
        private static XmlElement CE(XmlDocument d, string Name, string Content, bool Encode = false)
        {
            var E = d.CreateElement(Name);
            if (Encode)
            {
                E.InnerText = Content;
            }
            else
            {
                E.InnerXml = Content;
            }
            return E;
        }

        /// <summary>
        /// Generates a Cryptographically safe random number
        /// </summary>
        /// <param name="Length">Number of Bytes</param>
        /// <returns>Random bytes</returns>
        public static byte[] RandomKey(int Length)
        {
            using (var RNG = RandomNumberGenerator.Create())
            {
                byte[] Data = new byte[Length];
                RNG.GetBytes(Data);
                return Data;
            }
        }
    }
}
