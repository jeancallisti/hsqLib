using HsqLib;
using System;
using System.Collections.Generic;
using System.IO;

namespace SampleProject
{
    class Program
    {
        private void PrintHelp()
        {
            Console.WriteLine("DuneHSQHandler.exe -v1 <hsq file>");
            Console.WriteLine("   OR");
            Console.WriteLine("DuneHSQHandler.exe -v2 <hsq file>");
            Console.WriteLine("");
            Console.WriteLine("-v1 uses version 1 of the library (stable), -v2 uses version 2 (refactored, unstable).");
        }

        public void Run(string[] args)
        {
            if (args.Length != 2)
            {
                PrintHelp();
                return;
            }

            string version = args[0];
            string filename = args[1];


            if (!File.Exists(filename))
            {
                Console.WriteLine("Error: Argument needs to be valid file path.");
                return;
            }

            if (version == "-v1")
            {
                var input = new HsqLib.HsqCompressedFile.HsqCompressedFile(File.ReadAllBytes(filename));

                if (!HsqHandler.ValidateHeader(input))
                {
                    Console.WriteLine("Error: Not a valid HSQ file.");
                    return;
                }

                var output = new List<byte>();
                HsqHandler.Uncompress(input, output);

                if (!HsqHandler.ValidateOutputSize(input, output))
                {
                    Console.WriteLine("Warning: Output did not match size given in header.");
                }

                Console.WriteLine("Saving file: " + args[0] + ".org");
                File.WriteAllBytes(args[0] + ".org", output.ToArray());

                return;
            }

            if (version == "-v2")
            {
                using (var inputStream = File.OpenRead(filename))
                {

                }

                //var input = new HsqLib2.HsqReader.HsqCompressedFile(File.ReadAllBytes(filename));

                //if (!HsqHandler.ValidateHeader(input))
                //{
                //    Console.WriteLine("Error: Not a valid HSQ file.");
                //    return;
                //}

                //var output = new List<byte>();
                //HsqHandler.Uncompress(input, output);

                //if (!HsqHandler.ValidateOutputSize(input, output))
                //{
                //    Console.WriteLine("Warning: Output did not match size given in header.");
                //}

                //Console.WriteLine("Saving file: " + args[0] + ".org");
                //File.WriteAllBytes(args[0] + ".org", output.ToArray());

                return;
            }

            Console.WriteLine("Version must be -v1 or -v2.");
            return;

        }

        static void Main(string[] args)
        {
            new Program().Run(args);
        }
    }
}
