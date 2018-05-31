using System;
using System.IO;

namespace undlc
{
    class Program
    {
        static void Main(string[] args)
        {
            var F = new DlcContainer(File.ReadAllText(args[0]));

            Console.Error.WriteLine("#END");
            Console.ReadKey(true);
        }
    }
}
