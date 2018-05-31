using System;
using System.IO;

namespace undlc
{
    class Program
    {
        static void Main(string[] args)
        {
#if DEBUG
            args = new string[] { @"C:\Apache24\htdocs\decrypt\test\sample-container-1.dlc" };
#endif
            var F = new DlcContainer(File.ReadAllText(args[0]));

            Console.Error.WriteLine("#END");
            Console.ReadKey(true);
        }
    }
}
