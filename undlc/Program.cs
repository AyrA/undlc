using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace undlc
{
    class Program
    {
        struct ArgFormat
        {
            public bool Json, Full;
            public List<string> Files;
        }

        static void Main(string[] args)
        {
            if (args == null || args.Length == 0 || args.Contains("/?"))
            {
                Console.Error.WriteLine(@"undlc.exe /F /JSON file ...
Decrypts a DLC file

/F      - Full output. This prints URL, Filename and Size, spaced with tabs.
          If combined with /JSON it prints the entire DLC structure instead
          of a link array
/JSON   - JSON output instead of text
file    - DLC file to decrypt. You can specifiy multiple files");
            }
            else
            {
                ArgFormat A = GetArgs(args);
                DlcContainer DLC;
                foreach (var FN in A.Files)
                {
                    Console.Error.WriteLine(FN);
                    try
                    {
                        DLC = new DlcContainer(File.ReadAllText(FN));
                        if (A.Json)
                        {
                            if(A.Full)
                            {
                                Console.WriteLine(JsonConvert.SerializeObject(DLC));
                            }
                            else
                            {
                                Console.WriteLine(JsonConvert.SerializeObject(DLC.Content.Package.Files.Select(m => m.URL)));
                            }
                        }
                        else
                        {
                            foreach (var Link in DLC.Content.Package.Files)
                            {
                                if (A.Full)
                                {
                                    Console.WriteLine("{0}\t{1}\t{2}", Link.URL, Link.Filename, Link.Size);
                                }
                                else
                                {
                                    Console.WriteLine(Link.URL);
                                }
                            }
                        }
                    }
                    catch(Exception ex)
                    {
                        Console.Error.WriteLine($"Unable to decrypt {FN}. Reason: {ex.Message}");
                    }
                }
            }
#if DEBUG
            Console.Error.WriteLine("#END");
            Console.ReadKey(true);
#endif
        }

        private static ArgFormat GetArgs(string[] args)
        {
            ArgFormat A = new ArgFormat();
            A.Files = new List<string>();
            foreach (var Arg in args)
            {
                if (Arg.ToLower() == "/f")
                {
                    A.Full = true;
                }
                else if (Arg.ToLower() == "/json")
                {
                    A.Json = true;
                }
                else
                {
                    A.Files.Add(Arg);
                }
            }
            return A;
        }
    }
}
