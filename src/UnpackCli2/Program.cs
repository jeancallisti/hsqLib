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
    public class SwitchSetting
    {
        public string Key { get; init; }
        public bool IsOptional { get; init; }
        public IEnumerable<string> AcceptedValues { get; init; }
        public bool IsFollowedByValue { get; init; }
        public string FallbackValue { get; init; }

    }

    class Program
    {
        private static List<SwitchSetting> availableSwitches = new List<SwitchSetting>()
        {
            new SwitchSetting() { Key = "FILEMODE", AcceptedValues = new List<string>() { "json", "binary"}, IsFollowedByValue = false, IsOptional = false, FallbackValue = "" },
            new SwitchSetting() { Key = "IGNOREHEADER", AcceptedValues = new List<string>() { "ignoreHeader"}, IsFollowedByValue = false, IsOptional = true, FallbackValue = "true"},
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

        private bool TryParseArguments(string[] args, out Dictionary<string, string> switches)
        {
            var result = new Dictionary<string, string>();

            var argsUpperCase = args.Select(a => a.ToUpperInvariant()).ToArray();
            if (argsUpperCase.Contains("-H") || argsUpperCase.Contains("--H") || argsUpperCase.Contains("-HELP") || argsUpperCase.Contains("--HELP"))
            {
                switches = result;
                return false;
            }

            //Transform args to make their position available at any time in the future.
            var argsWithPosition = Enumerable.Range(0, argsUpperCase.Length).ToArray()
                                            //Filter the switches from the other arguments
                                            .Where(i => argsUpperCase[i].StartsWith("-"))
                                            .Select(i => new { Position = i, Arg = argsUpperCase[i] });

            try
            {
                availableSwitches.ForEach(s =>
                {
                    var textToRecognize = s.AcceptedValues.Select(v => $"-{v.ToUpperInvariant()}");

                    var matches = argsWithPosition.Where(m => textToRecognize.Contains(m.Arg));

                    if (matches.Count() > 1)
                    {
                        throw new Exception($"You can have only one of those simultaneously : {string.Join(",", matches.Select(v => $"'{v}'"))} ");
                    }

                    if (matches.Count() == 0)
                    {
                        if (!s.IsOptional)
                        {
                            throw new Exception($"A mandatory switch is missing. Expected : {string.Join(",", s.AcceptedValues.Select(v => $"'{v}'"))} ");
                        }

                        //Use fallback value
                        result.Add(s.Key, s.FallbackValue);
                    }
                    else
                    {
                        var match = matches.First();

                        if (!s.IsFollowedByValue)
                        {
                            result.Add(s.Key, match.Arg);
                        }
                        else
                        {
                            var paramPosition = match.Position + 1;

                            if (paramPosition >= args.Length)
                            {
                                throw new Exception($"Switch '{match.Arg}' needs to be followed by a value.");
                            }

                            //Read the arg just after this switch.
                            var param = args[paramPosition];

                            if (param.StartsWith("-"))
                            {
                                throw new Exception($"Switch '{match.Arg}' needs to be followed by a value. Found this instead : '{param}'");
                            }

                            result.Add(s.Key, param);
                        }

                    }
                });
            } catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                switches = result;
                return false;
            }
 

            switches = result;
            return true;
        }

        public void Run(string[] args)
        {
            if (!TryParseArguments(args, out var switches ))
            {
                PrintHelp();
                return;
            }

            string switchFileMode = switches["FILEMODE"];
            string filename = switches["FILE"];

            var ignoreHeader = switches["IGNOREHEADER"] == "TRUE";

            //+DEBUG
            switches.Keys.ToList().ForEach(s => Console.WriteLine($"{s} : {switches[s]}"));
            //-DEBUG

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
