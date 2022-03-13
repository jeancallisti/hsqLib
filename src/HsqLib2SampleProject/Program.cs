using HsqLib2;
using HsqLib2.HsqReader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace SampleProject
{
    class Program
    {
        private void PrintHelp()
        {
            Console.WriteLine("HsqLib2SampleProject.exe <hsq file>");
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

            using (var inputStream = File.OpenRead(filename))
            {
                var reader = new HsqReader();

                //if (!HsqHandler.ValidateHeader(input))
                //{
                //    Console.WriteLine("Error: Not a valid HSQ file.");
                //    return;
                //}

                var task = Task.Run(async () =>
                {
                    var unpacked = await reader.UnpackFile(inputStream, false);

                    //if (!HsqHandler.ValidateOutputSize(input, output))
                    //{
                    //    Console.WriteLine("Warning: Output did not match size given in header.");
                    //}

                    Console.WriteLine("Saving file: " + args[0] + ".org");
                    File.WriteAllBytes(args[0] + ".org", unpacked.UnCompressedData);
                });
                task.Wait();




            }
        }

        static void Main(string[] args)
        {
            new Program().Run(args);
        }
    }
}
