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
            Console.WriteLine("HsqLibSampleProject.exe <hsq file>");
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
                Console.WriteLine("Error: Argument needs to be valid file path.");
                return;
            }


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

        }

        static void Main(string[] args)
        {
            new Program().Run(args);
        }
    }
}
