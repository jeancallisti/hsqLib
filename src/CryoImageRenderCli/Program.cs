using CommandLineUtil;
using CryoDataLib.ImageLib;
using HsqLib2;
using HsqLib2.HsqReader;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CryoImageRenderCli
{


    class Program
    {
        private static List<SwitchSetting> availableSwitches = new List<SwitchSetting>()
        {
            new SwitchSetting() { Key = "PALETTE", AcceptedValues = new List<string>() { "palette"}, IsFollowedByValue = true, IsOptional = true, FallbackValue = "-GENERATE"},
            new SwitchSetting() { Key = "SOURCE", AcceptedValues = new List<string>() { "source"}, IsFollowedByValue = true, IsOptional = false, FallbackValue = ""},
            new SwitchSetting() { Key = "DESTFOLDER", AcceptedValues = new List<string>() { "dest"}, IsFollowedByValue = true, IsOptional = true, FallbackValue = ""}
        };

        private void PrintHelp()
        {
            Console.WriteLine("CryoImageRenderCli.exe [-palette {generate|guessInternalOnly|guessAll}] -source <uncompressed hsq image json file> [-dest <destinationFolder>]");
            Console.WriteLine("");
            Console.WriteLine("Examples:");
            Console.WriteLine("  CryoImageRenderCli.exe -source ./Data/ICONES.HSQ.image.json");
            Console.WriteLine("  CryoImageRenderCli.exe -source ./Data/ICONES.HSQ.image.json -dest ./Output");
            Console.WriteLine("  CryoImageRenderCli.exe -palette generate -source ./Data/ICONES.HSQ.image.json -dest ./Output");
            Console.WriteLine("  CryoImageRenderCli.exe -palette guessInternalOnly -source ./Data/ICONES.HSQ.image.json");
            Console.WriteLine("");
            Console.WriteLine("About -palette:");
            Console.WriteLine("   'generate' creates a dummy palette that roughly matches the colors found in the sprite, for visualization purposes.");
            Console.WriteLine("   'guessInternalOnly' reads the subpalettes in the source file and generates sprites for the palettes that seem to correspond.");
            Console.WriteLine("   'guessAll' Same as 'guessInternalOnly' except it tries EVERY SUBPALETTE FROM EVERY FILE in the source file folder.");
        }



        private PaletteModes ParsePaletteMode(string paletteMode)
        {
            switch (paletteMode)
            {
                case "-GENERATE": return PaletteModes.eGenerate;
                case "-GUESSINTERNALONLY": return PaletteModes.eguessInternalOnly;
                case "-GUESSALL": return PaletteModes.eGuessAll;
            }

            throw new NotImplementedException($"Not implemented : {paletteMode}");
        }

        private IEnumerable<SubPalette> GetSubPalettes(CryoImageData cryoImage, string sourceFolder, PaletteModes paletteMode)
        {
            switch (paletteMode)
            {
                case PaletteModes.eGenerate:
                    //Palettes will be generated on the fly
                    return new List<SubPalette>();
                case PaletteModes.eguessInternalOnly:
                    return cryoImage.SubPalettes;
                case PaletteModes.eGuessAll:
                    //TODO
                    throw new NotImplementedException("TODO: Guess all");
                    return new List<SubPalette>();
            }

            throw new NotImplementedException($"Not implemented palette mode: {paletteMode}");
        }


        public void Run(string[] args)
        {
            if (!ArgumentsParsing.TryParseArguments(args, availableSwitches, out var switches))
            {
                PrintHelp();
                return;
            }

            string strPaletteMode = switches["PALETTE"];

            var paletteMode = ParsePaletteMode(strPaletteMode);

            string sourceFile = switches["SOURCE"];
            string destFolder = switches["DESTFOLDER"];

            if (!File.Exists(sourceFile))
            {
                Console.Error.WriteLine($"File '{Path.GetFullPath(sourceFile)}' not found.");
                return;
            }

            if (!string.IsNullOrEmpty(destFolder) &&!Directory.Exists(destFolder))
            {
                Console.Error.WriteLine($"Creating folder '{Path.GetFullPath(destFolder)}'.");
                Directory.CreateDirectory(destFolder);
            }

            var sourceFolder = new FileInfo(sourceFile).Directory.FullName;

            Console.Error.WriteLine($"Source folder is '{Path.GetFullPath(sourceFolder)}'.");

            using (var stream = File.OpenRead(sourceFile))
            using (var streamReader = new StreamReader(stream))
            using (var jsonTextReader = new JsonTextReader(streamReader))
            {
                var jsonSerializer = new JsonSerializer();
                var cryoImage = jsonSerializer.Deserialize<CryoImageData>(jsonTextReader);

                if (cryoImage.UnknownParts.Count() > 0)
                {
                    Console.WriteLine($"There were {cryoImage.UnknownParts.Count()} unknown parts. they will be ignored.");
                }

                var task = Task.Run(async () =>
                {
                    var renderer = new CryoImageRenderer(cryoImage, destFolder);

                    Console.WriteLine($"Obtaining required subpalettes...");

                    //Depending on the palette mode it could be the file's palettes, other files' palettes, or no palette at all.
                    var subPalettes = GetSubPalettes(cryoImage, sourceFolder, paletteMode);

                    Console.WriteLine($"Saving this file's subpalettes...");

                    //Regardless of the palette mode, we save the palettes present in the file
                    renderer.SaveSubpalettesToDisk();

                    renderer.SaveImagePartsToDisk(paletteMode, subPalettes);

                    return;

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
