using CryoDataLib;
using CryoDataLib.TextLib;
using HsqLib2;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CryoDataCli
{
    class Program
    {
        private void PrintHelp()
        {
            Console.WriteLine("To unpack a text file (e.g. PHRASEXX.HSQ) :");
            Console.WriteLine("       CryoDataCli.exe -text -<charset> <uncompressed hsq file in json format>");
            Console.WriteLine("       Charset can be : en-US, fr-FR, and others (see latest source code for reference).");
            Console.WriteLine("");
            Console.WriteLine("To obtain a file in json format, use other CLI utility : UnpackCli2.exe -json FILE.HSQ");
        }

        public static IEnumerable<CharsetRedirectTable> DeserializeCharSet(string json)
        {
            using (var streamReader = new StringReader(json))
            using (var jsonTextReader = new JsonTextReader(streamReader))
            {
                var jsonSerializer = new JsonSerializer();
                return jsonSerializer.Deserialize<IEnumerable<CharsetRedirectTable>>(jsonTextReader);
            }
        }

        public static IEnumerable<JsonTextInstruction> DeserializeTextInstructions(string json)
        {
            using (var streamReader = new StringReader(json))
            using (var jsonTextReader = new JsonTextReader(streamReader))
            {
                var jsonSerializer = new JsonSerializer();
                return jsonSerializer.Deserialize<IEnumerable<JsonTextInstruction>>(jsonTextReader);
            }
        }

        private IEnumerable<CharsetRedirectTable> LoadCharSets()
        {
            //For now we load from static string but we could load from config file
            var jsonData = CharSets.BasicJsonData;

            return DeserializeCharSet(jsonData);
        }

        private IEnumerable<JsonTextInstruction> LoadTextInstructions()
        {
            //For now we load from static string but we could load from config file
            var jsonData = TextInstructions.Json;

            return DeserializeTextInstructions(jsonData);
        }

        private bool ParseTextParams(string[] args, out string culture, out string fileName)
        {
            culture = "";
            fileName = "";

            if (args.Length != 2)
            {
                PrintHelp();
                return false;
            }

            string cultureSwitch = args[0];

            if (!cultureSwitch.StartsWith("-"))
            {
                PrintHelp();
                return false;
            }

            culture = cultureSwitch.Substring(1);
            fileName = args[1];

            return true;
        }

        private void DoText(string[] args)
        {
            if (!ParseTextParams(args, out var culture, out var filename)) {
                return;
            }

            if (!File.Exists(filename))
            {
                Console.WriteLine($"Error: Could not open '{Path.GetFullPath(filename)}'");
                return;
            }

            var charSets = LoadCharSets();
            var charSet = charSets.ToList().FirstOrDefault(set => set.Culture.ToUpperInvariant() == culture.ToUpperInvariant());

            if (charSet == null)
            {
                throw new NotImplementedException($"Unknown charset '{culture}'. Available : {string.Join(", ", charSets.Select(cs => "-"+cs.Culture))}");
            }

            var textInstructions = LoadTextInstructions();


            using (var stream = File.OpenRead(filename))
            using (var streamReader = new StreamReader(stream))
            using (var jsonTextReader = new JsonTextReader(streamReader))
            {
                var jsonSerializer = new JsonSerializer();
                var hsqFile = jsonSerializer.Deserialize<HsqFile>(jsonTextReader);

                var textParser = new CryoTextDataInterpreter(charSet, textInstructions);

                var task = Task.Run(async () =>
                {
                    var cryoData = (CryoTextData)await textParser.InterpretData(hsqFile);

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

        public void Run(string[] args)
        {
            if (args.Length < 2)
            {
                PrintHelp();
                return;
            }

            string switchText = args[0];

            //Skip the switch
            var subArgs = args.ToArray().Skip(1).Take(args.Length).ToArray();

            if (switchText == "-text")
            {
                DoText(subArgs);
                return;
            }



        }

        static void Main(string[] args)
        {
            new Program().Run(args);
        }
    }
}
