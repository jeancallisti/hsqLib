using HsqLib;
using System;
using System.Collections.Generic;
using System.IO;

namespace UnpackCli
{
    class Program
    {
        private void PrintHelp()
        {
            Console.WriteLine("UnpackCli.exe <hsq file>");
        }

        public void Run(string[] args)
        {
            if (args.Length != 1)
            {
                PrintHelp();
                return;
            }

            string filename = args[0];

            if (!File.Exists(filename))
            {
                Console.Error.WriteLine("Argument needs to be valid file path.");
                return;
            }


            var input = new HsqLib.HsqCompressedFile.HsqCompressedFile(File.ReadAllBytes(filename));

            if (!HsqHandler.ValidateHeader(input))
            {
                Console.Error.WriteLine("Not a valid HSQ file.");
                return;
            }

            var output = new List<byte>();
            HsqHandler.Uncompress(input, output);

            if (!HsqHandler.ValidateOutputSize(input, output))
            {
                Console.WriteLine("Warning: Output did not match size given in header.");
            }

            Console.WriteLine("Saving file: " + args[0] + ".uncompressed");
            File.WriteAllBytes(args[0] + ".uncompressed", output.ToArray());
        }

        static void Main(string[] args)
        {
            new Program().Run(args);
        }
    }
}