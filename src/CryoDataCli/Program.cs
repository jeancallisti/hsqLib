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

            var charSets = LoadCharSets(); //TODO: Load from file instead of hard-coded json
            var charSet = charSets.ToList().FirstOrDefault(set => set.Culture.ToUpperInvariant() == culture.ToUpperInvariant());

            if (charSet == null)
            {
                throw new NotImplementedException($"Unknown charset '{culture}'. Available : {string.Join(", ", charSets.Select(cs => "-"+cs.Culture))}");
            }

            var textInstructions = LoadTextInstructions(); //TODO: Load from file instead of hard-coded json


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
                    string fileName = $"{cryoData.SourceFile}.{cryoData.DataType}.json";
                    SaveJsonFile(cryoData, fileName);
                    return;

                });
                task.Wait();
            }
        }

        private string JsonSerializePrettified (object o)
        {
            return JsonConvert.SerializeObject(
                                    o,
                                    //To save prettified json
                                    Formatting.Indented);
        }


        // Due to polymorphism this can save CryoImageData and CryoTextData
        private void SaveJsonFile(CryoData cryoData, string fileName)
        {
            Console.WriteLine("Saving json file: " + fileName);
            var jsonHsqFile = JsonSerializePrettified(cryoData);

            File.WriteAllText(fileName, jsonHsqFile);
        }

        private void SavePaletteFileAsPng(SubPalette palette, string fileName)
        {
            int scaleUpFactor = 20;

            var asSprite = Palette.ToSprite(palette);
            var asBitmap = BitmapBuilder.ToBitmap(asSprite);

            var scaledUpBitmap = BitmapBuilder.ScaleUpNearestNeighbour(asBitmap, scaleUpFactor);

            Console.WriteLine($"Saving palette file {fileName}...");
            scaledUpBitmap.Save(fileName, ImageFormat.Png);
        }

        private void SavePartSpriteAsPng(Sprite sprite, string fileName)
        {
            int scaleUpFactor = 20;

            var asBitmap = BitmapBuilder.ToBitmap(sprite);

            var scaledUpBitmap = BitmapBuilder.ScaleUpNearestNeighbour(asBitmap, scaleUpFactor);

            Console.WriteLine($"Saving part file {fileName}...");
            scaledUpBitmap.Save(fileName, ImageFormat.Png);
        }

        private void SaveImageDataToDisk(CryoImageData cryoData)
        {
            string jsonFileName = $"{cryoData.SourceFile}.{cryoData.DataType}.json";
            SaveJsonFile(cryoData, jsonFileName);

            if (true) // TODO : make it optional?
            {
                var subPalettes = cryoData.SubPalettes.ToArray();
                for (int i = 0; i < subPalettes.Length; i++)
                {
                    var paletteFileName = $"{cryoData.SourceFile}.palette{i}.png";
                    SavePaletteFileAsPng(subPalettes[i], paletteFileName);
                }
            }

            if (true) // TODO : make it optional?
            {
                var imageParts = cryoData.ImageParts.Select(p => p.ImagePart);

                var uncompressedParts = imageParts
                                .Where(p => !p.IsCompressed) //TODO : for now, only non-compressed parts
                                .ToList();

                uncompressedParts.ForEach(p =>
                {

                    //No palette for now. 'Parts' sprites rely on palette offset
                    var asSpriteWithPaletteOffset = p.ToSpriteWithPaletteOffset();
                    //+DEBUG
                    asSpriteWithPaletteOffset.PaletteOffset = 0;
                    //-DEBUG

                    var namedPalettes = cryoData.SubPalettes.Select(subp => new NamedPalette()
                    {
                        Name = subp.Name,
                        Palette = Palette.BuildFromSubpalette(subp, PaletteColor.GREEN)
                    }).ToList();

                    namedPalettes.Add(new NamedPalette()
                    {
                        Name = "subpaletteMock",
                        Palette = Palette.CreateMockPaletteFor(asSpriteWithPaletteOffset)
                    });

                    //TODO : fix this 
                    for (int j = 0; j < namedPalettes.Count(); j++)
                    {
                        var paletteName = namedPalettes[j].Name;
                        var palette = namedPalettes[j].Palette;

                        var partFileName = $"{cryoData.SourceFile}.{p.Name}.{paletteName}.png";

                        try
                        {
                            try
                            {
                                var asSprite = asSpriteWithPaletteOffset.CombineWithPalette(palette);
                                SavePartSpriteAsPng(asSprite, partFileName);
                            }
                            catch (CryoDataCannotApplyPaletteException ex)
                            {
                                Console.WriteLine($"Subpalette '{paletteName}' does not seem to be a good candidate for part {p.Name}. Not saving as PNG.");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.Error.WriteLine($"Could not export part {p.Name}, subpalette {j} as PNG.");
                        }
                    }
                });
            }

            if (cryoData.UnknownParts.Count() > 0)
            {
                Console.WriteLine($"There were {cryoData.UnknownParts.Count()} unknown parts.");
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

                    SaveImageDataToDisk(cryoData);

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
