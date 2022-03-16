using CryoDataLib;
using CryoDataLib.ImageLib;
using CryoDataLib.ImageLib.BitmapExport;
using CryoDataLib.TextLib;
using HsqLib2;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
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
            Console.WriteLine("To unpack a set of images (e.g. CHANKISS.HSQ) :");
            Console.WriteLine("       CryoDataCli.exe -image <uncompressed hsq file in json format>");
            Console.WriteLine("");
            Console.WriteLine("To obtain a file in json format, use other CLI utility : UnpackCli2.exe -json -file FILE.HSQ");
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

        //TODO: Use ArgumentsParsing instead, as in UnpackCli2
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


        //TODO: Use ArgumentsParsing instead, as in UnpackCli2
        private bool ParseImageParams(string[] args, out string fileName)
        {
            fileName = "";

            if (args.Length != 1)
            {
                PrintHelp();
                return false;
            }

            fileName = args[0];

            return true;
        }

        private void DoText(string[] args)
        {
            if (!ParseTextParams(args, out var culture, out var filename)) {
                return;
            }

            if (!File.Exists(filename))
            {
                Console.Error.WriteLine($"Could not open '{Path.GetFullPath(filename)}'");
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
        }

        private void SaveJsonFile(CryoImageData cryoData)
        {
            string outputFile = $"{cryoData.SourceFile}.image.json";
            Console.WriteLine("Saving json file: " + outputFile);
            var jsonHsqFile = JsonConvert.SerializeObject(
                                                cryoData,
                                                //To save prettified json
                                                Formatting.Indented);

            File.WriteAllText(outputFile, jsonHsqFile);
        }

        private void SavePaletteFileAsPng(CryoImageData cryoData, int subpaletteIndex)
        {
            int scaleUpFactor = 20;

            var subPaletteAsSprite = cryoData.Palette.ToSprite(subpaletteIndex);
            var asBitmap = BitmapBuilder.ToBitmap(subPaletteAsSprite);

            var scaledUpBitmap = BitmapBuilder.ScaleUpNearestNeighbour(asBitmap, scaleUpFactor);

            var outputPaletteFile = $"{cryoData.SourceFile}.palette{subpaletteIndex}.png";
            Console.WriteLine($"Saving palette file {outputPaletteFile}...");
            scaledUpBitmap.Save(outputPaletteFile, ImageFormat.Png);
        }


        private void SaveToDisk(CryoImageData cryoData)
        {

            SaveJsonFile(cryoData);

            if (true)
            {
                for (int i = 0; i < cryoData.Palette.SubPalettes.Count(); i++)
                {
                    SavePaletteFileAsPng(cryoData, i);
                }
            }
        }


        private void DoImage(string[] args)
        {
            if (!ParseImageParams(args, out var filename))
            {
                return;
            }

            if (!File.Exists(filename))
            {
                Console.Error.WriteLine($"Could not open '{Path.GetFullPath(filename)}'");
                return;
            }

            using (var stream = File.OpenRead(filename))
            using (var streamReader = new StreamReader(stream))
            using (var jsonTextReader = new JsonTextReader(streamReader))
            {
                var jsonSerializer = new JsonSerializer();
                var hsqFile = jsonSerializer.Deserialize<HsqFile>(jsonTextReader);

                var imageInterpreter = new CryoImageDataInterpreter();

                var task = Task.Run(async () =>
                {
                    var cryoData = (CryoImageData)await imageInterpreter.InterpretData(hsqFile);

                    SaveToDisk(cryoData);

                    return;

                });
                task.Wait();
            }
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

            if (switchText == "-image")
            {
                DoImage(subArgs);
                return;
            }

            PrintHelp();

        }

        static void Main(string[] args)
        {
            new Program().Run(args);
        }
    }
}
