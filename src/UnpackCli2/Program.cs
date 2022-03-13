using HsqLib2;
using HsqLib2.HsqReader;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace UnpackCli
{
    class Program
    {
        private void PrintHelp()
        {
            Console.WriteLine("UnpackCli2.exe -binary <hsq file>");
            Console.WriteLine("UnpackCli2.exe -json <hsq file>");
            Console.WriteLine("-binary exports the unpacked file as is (without the header)");
            Console.WriteLine("-json exports all data into json file");
        }

        public void Run(string[] args)
        {
            if (args.Length != 2)
            {
                PrintHelp();
                return;
            }

            string switchText = args[0];
            string filename = args[1];

            if (switchText != "-binary" && switchText != "-json")
            {
                PrintHelp();
                return;
            }

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

                    if (switchText == "-binary") {
                        Console.WriteLine("Saving binary file: " + filename + ".uncompressed");
                        File.WriteAllBytes(filename + ".uncompressed", unpacked.UnCompressedData);
                        return;
                    }

                    if (switchText == "-json")
                    {
                        string outputFile = $"{filename}.uncompressed.json";
                        Console.WriteLine("Saving json file: " + outputFile);
                        var jsonHsqFile = JsonConvert.SerializeObject(unpacked,
                            //To save prettified json
                            Formatting.Indented);
                        File.WriteAllText(outputFile, jsonHsqFile);
                        return;
                    }

                    throw new NotImplementedException(switchText);
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
