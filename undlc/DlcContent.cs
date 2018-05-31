using System.Xml;
using System.Linq;

namespace undlc
{
    /// <summary>
    /// Represents a DLC header
    /// </summary>
    public struct DlcHeader
    {
        /// <summary>
        /// DLC Generator Application information
        /// </summary>
        public DlcGenerator Generator;
        /// <summary>
        /// Freely chosable version number
        /// </summary>
        public string Version;
        /// <summary>
        /// Attributes or something
        /// </summary>
        public DlcTribute Tribute;

        public DlcHeader(XmlNode E)
        {
            if (E != null)
            {
                Generator = new DlcGenerator(E["generator"]);
                Version = Tools.ASC(Tools.B64(Tools.Val(E["dlcxmlversion"])));
                Tribute = new DlcTribute(E["tribute"]);
            }
            else
            {
                Version = null;
                Generator = new DlcGenerator();
                Tribute = new DlcTribute();
            }
        }
    }

    /// <summary>
    /// Represents a DLC body
    /// </summary>
    public struct DlcContent
    {
        /// <summary>
        /// DLC Package
        /// </summary>
        public DlcPackage Package;

        public DlcContent(XmlNode E)
        {
            if (E != null)
            {
                Package = new DlcPackage(E["package"]);
            }
            else
            {
                Package = new DlcPackage();
            }
        }
    }

    /// <summary>
    /// DLC File Group Information
    /// </summary>
    public struct DlcPackage
    {
        /// <summary>
        /// Package Name
        /// </summary>
        public string Name;
        /// <summary>
        /// List of passwords for files
        /// </summary>
        public string Passwords;
        /// <summary>
        /// Comment
        /// </summary>
        public string Comment;
        /// <summary>
        /// Category
        /// </summary>
        public string Category;

        /// <summary>
        /// File List
        /// </summary>
        public DlcFile[] Files;

        public DlcPackage(XmlNode E)
        {
            if (E != null)
            {
                Name = Tools.ASC(Tools.B64(Tools.Attr(E.Attributes["name"])));
                Passwords = Tools.ASC(Tools.B64(Tools.Attr(E.Attributes["passwords"])));
                Comment = Tools.ASC(Tools.B64(Tools.Attr(E.Attributes["comment"])));
                Category = Tools.ASC(Tools.B64(Tools.Attr(E.Attributes["category"])));
                Files = E.ChildNodes
                    .OfType<XmlNode>()
                    .Where(m => m.Name.ToLower() == "file")
                    .Select(m => new DlcFile(m))
                    .ToArray();
            }
            else
            {
                Name = Passwords = Comment = Category = null;
                Files = new DlcFile[0];
            }
        }
    }

    /// <summary>
    /// Represents a DLC File entry
    /// </summary>
    public struct DlcFile
    {
        /// <summary>
        /// Download URL
        /// </summary>
        public string URL;
        /// <summary>
        /// Real File name
        /// </summary>
        public string Filename;
        /// <summary>
        /// File size
        /// </summary>
        public long Size;

        public DlcFile(XmlNode E)
        {
            if (E != null)
            {
                URL = Tools.ASC(Tools.B64(Tools.Val(E["url"])));
                Filename = Tools.ASC(Tools.B64(Tools.Val(E["filename"])));
                Size = long.Parse(Tools.ASC(Tools.B64(Tools.Val(E["size"]))));
            }
            else
            {
                URL = Filename = null;
                Size = 0;
            }
        }
    }

    /// <summary>
    /// DLC Generator Info
    /// </summary>
    public struct DlcGenerator
    {
        /// <summary>
        /// Application name
        /// </summary>
        public string App;
        /// <summary>
        /// Application version
        /// </summary>
        public string Version;
        /// <summary>
        /// Application info URL
        /// </summary>
        public string Url;

        public DlcGenerator(XmlNode E)
        {
            if (E != null)
            {
                App = Tools.ASC(Tools.B64(Tools.Val(E["app"])));
                Version = Tools.ASC(Tools.B64(Tools.Val(E["version"])));
                Url = Tools.ASC(Tools.B64(Tools.Val(E["url"])));
            }
            else
            {
                App = Version = Url = null;
            }
        }
    }

    /// <summary>
    /// ¯\(O-°)/¯
    /// </summary>
    public struct DlcTribute
    {
        /// <summary>
        /// Name
        /// </summary>
        public string Name;

        public DlcTribute(XmlNode E)
        {
            if (E != null)
            {
                Name = Tools.ASC(Tools.B64(Tools.Val(E["name"])));
            }
            else
            {
                Name = null;
            }
        }
    }
}
