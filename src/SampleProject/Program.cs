using HsqLib;
using System;
using System.Collections.Generic;
using System.IO;

namespace SampleProject
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("DuneHSQHandler.exe <hsq file>");
                return;
            }

            if (!File.Exists(args[0]))
            {
                Console.WriteLine("Error: Argument needs to be valid file path.");
                return;
            }

            var input = new HsqCompressedFile(File.ReadAllBytes(args[0]));

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
    }
}
