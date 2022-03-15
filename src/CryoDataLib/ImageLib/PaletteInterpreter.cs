using CryoDataLib.ImageLib.BitmapExport;
using CryoDataLib.TextLib;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace CryoDataLib.ImageLib
{
    public static class PaletteInterpreter
    {
        private static bool IsEndOfPalette(BinaryReader paletteReader)
        {
            var nextWord = paletteReader.ReadUInt16();
            //Rewind the two bytes we've just read.
            paletteReader.BaseStream.Position -= 2;

            //The palette data always ends with 0xFF 0xFF
            return (nextWord == 65535);
        }


        private static SubPalette DecodeSubPalette(BinaryReader paletteReader)
        {
            var locationInPalette = paletteReader.ReadByte();
            var colorsCount = paletteReader.ReadByte();

            Console.WriteLine($"{colorsCount} colors.");

            var colors = new List<PaletteColor>();

            for(var i = 0; i < colorsCount; i++)
            {
                colors.Add(new PaletteColor()
                {
                    Index = i,
                    R = paletteReader.ReadByte(),
                    G = paletteReader.ReadByte(),
                    B = paletteReader.ReadByte(),
                });
            }

            return new SubPalette()
            {
                Colors = colors,
                LocationInPalette = locationInPalette,
            };
        }

        public static Palette DecodePalette(byte[] paletteData)
        {
            using (var stream = new MemoryStream(paletteData))
            using (var paletteReader = new BinaryReader(stream))
            {
                if (paletteData.Length == 0)
                {
                    Console.WriteLine($"No palette.");
                    return new Palette
                    {
                        SubPalettes = new List<SubPalette>()
                    };
                } else
                {
                    Console.WriteLine($"There is subpalette(s).");
                }


                var subPalettes = new List<SubPalette>();

                int count = 0;
                while (!IsEndOfPalette(paletteReader))
                {
                    Console.WriteLine($"Subpalette {count}...");
                    try
                    {
                        subPalettes.Add(DecodeSubPalette(paletteReader));
                    }
                    catch
                    {
                        throw new CryoDataException("Could not process subpalette.");
                    }
                    count++;
                }

                //Palette junk (between the terminating 0xFF 0xFF and the address where the palette ends)
                //See https://zwomp.com/index.php/2021/03/01/exploring-the-dune-files-the-palette-structure/
                //See https://www.bigs.fr/dune_old/#ch4
                var junkLength = (int)(paletteReader.BaseStream.Length - paletteReader.BaseStream.Position);
                if (junkLength > 0)
                {
                    //we display this only for control, we absolutely don't care about that junk data.
                    Console.WriteLine("There is palette junk.");

                    var junkBytes = paletteReader.ReadBytes(junkLength);
                    Console.WriteLine($"[{string.Join(",", junkBytes.Select(b => HexHelper.ByteToHexString(b)))}]");
                }

                return new Palette
                {
                    SubPalettes = subPalettes
                };
            }
        }

    }
}
