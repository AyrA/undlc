using System.Xml;
using System.Linq;

namespace undlc
{
    public struct DlcHeader
    {
        public DlcGenerator Generator;
        public string Version;
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

    public struct DlcContent
    {
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

    public struct DlcPackage
    {
        public string Name;
        public string Passwords;
        public string Comment;
        public string Category;

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

    public struct DlcFile
    {
        public string URL;
        public string Filename;
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

    public struct DlcGenerator
    {
        public string App;
        public string Version;
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

    public struct DlcTribute
    {
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
