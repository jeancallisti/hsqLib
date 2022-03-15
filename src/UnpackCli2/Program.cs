using CommandLineUtil;
using HsqLib2;
using HsqLib2.HsqReader;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace UnpackCli
{


    class Program
    {
        private static List<SwitchSetting> availableSwitches = new List<SwitchSetting>()
        {
            new SwitchSetting() { Key = "FILEMODE", AcceptedValues = new List<string>() { "json", "binary"}, IsFollowedByValue = false, IsOptional = false, FallbackValue = "" },
            new SwitchSetting() { Key = "IGNOREHEADER", AcceptedValues = new List<string>() { "ignoreHeader"}, IsFollowedByValue = false, IsOptional = true, FallbackValue = ""},
            new SwitchSetting() { Key = "FILE", AcceptedValues = new List<string>() { "file"}, IsFollowedByValue = true, IsOptional = false, FallbackValue = ""}
        };

        private void PrintHelp()
        {
            Console.WriteLine("UnpackCli2.exe {-binary|-json} [-ignoreHeader] -file <hsq file>");
            Console.WriteLine("");
            Console.WriteLine("  Using '-binary' exports the unpacked data as-is, as a series of bytes (without the header)");
            Console.WriteLine("  Using '-json' exports all data into json file");
            Console.WriteLine("  Using '-ignoreHeader' still reads the header but does not verify consistency.");
            Console.WriteLine("");
            Console.WriteLine("Examples:");
            Console.WriteLine("  UnpackCli2.exe -binary -file ./Data/SAMPLE.HSQ");
            Console.WriteLine("  UnpackCli2.exe -json -file ./Data/SAMPLE.HSQ");
            Console.WriteLine("  UnpackCli2.exe -json -ignoreHeader -file ./Data/SAMPLE.HSQ");
        }

        public void Run(string[] args)
        {
            if (!ArgumentsParsing.TryParseArguments(args, availableSwitches, out var switches ))
            {
                PrintHelp();
                return;
            }

            string switchFileMode = switches["FILEMODE"];
            string filename = switches["FILE"];
            var ignoreHeader = !string.IsNullOrEmpty(switches["IGNOREHEADER"]);

            if (!File.Exists(filename))
            {
                Console.Error.WriteLine($"File '{Path.GetFullPath(filename)}' not found.");
                return;
            }

            using (var inputStream = File.OpenRead(filename))
            {
                var reader = new HsqReader();

                var task = Task.Run(async () =>
                {
                    var unpacked = await reader.UnpackFile(inputStream, ignoreHeader);

                    //if (!HsqHandler.ValidateOutputSize(input, output))
                    //{
                    //    Console.WriteLine("Warning: Output did not match size given in header.");
                    //}

                    if (switchFileMode == "-BINARY") {
                        Console.WriteLine("Saving binary file: " + filename + ".uncompressed");
                        File.WriteAllBytes(filename + ".uncompressed", unpacked.UnCompressedData);
                        return;
                    }

                    if (switchFileMode == "-JSON")
                    {
                        string outputFile = $"{filename}.uncompressed.json";
                        Console.WriteLine("Saving json file: " + outputFile);
                        var jsonHsqFile = JsonConvert.SerializeObject(unpacked,
                            //To save prettified json
                            Formatting.Indented);
                        File.WriteAllText(outputFile, jsonHsqFile);
                        return;
                    }

                    throw new NotImplementedException(switchFileMode);
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
