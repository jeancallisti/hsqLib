using CryoDataLib;
using CryoDataLib.TextLib;
using HsqLib2;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;

namespace CryoDataCli
{
    class Program
    {
        private void PrintHelp()
        {
            Console.WriteLine("CryoDataCli.exe -text <uncompressed hsq file>");
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

            if (switchText != "-text")
            {
                PrintHelp();
                return;
            }

            if (!File.Exists(filename))
            {
                Console.WriteLine("Error: Argument needs to be valid file path.");
                return;
            }

            using (var stream = File.OpenRead(filename))
            using (var streamReader = new StreamReader(stream))
            using (var jsonTextReader = new JsonTextReader(streamReader))
            {
                var jsonSerializer = new JsonSerializer();
                var hsqFile = jsonSerializer.Deserialize<HsqFile>(jsonTextReader);

                var textParser = new CryoTextDataInterpreter();

                var task = Task.Run(async () =>
                {
                    var cryoData = (CryoTextData) await textParser.InterpretData(hsqFile);

                    //string outputFile = $"{filename}.text.json";
                    string outputFile = $"{cryoData.SourceFile}.text.json";
                    Console.WriteLine("Saving json file: " + outputFile);
                    var jsonHsqFile = JsonConvert.SerializeObject(
                                                        cryoData,
                                                        //To save prettified json
                                                        Formatting.Indented);
                    File.WriteAllText(outputFile, jsonHsqFile);
                    return;

                });
                task.Wait();
            }

            //Console.WriteLine("Saving file: " + args[0] + ".org");
            //File.WriteAllBytes(args[0] + ".org", output.ToArray());

        }

        static void Main(string[] args)
        {
            new Program().Run(args);
        }
    }
}
