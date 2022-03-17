using System;
using System.Collections.Generic;
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


        private static SubPalette DecodeSubPalette(BinaryReader paletteReader, int paletteIndex)
        {
            var locationInPalette = paletteReader.ReadByte();
            var colorsCount = paletteReader.ReadByte();

            Console.WriteLine($"{colorsCount} colors.");

            var colors = new List<PaletteColor>();

            for(var i = 0; i < colorsCount; i++)
            {
                // *4 because the colors are stored on 6 bits for whatever reason and
                // by design we need to do 2 bitshifts to the left (*4) to get the real value.
                // See https://www.bigs.fr/dune_old/#ch4
                int boostedR = paletteReader.ReadByte() * 4;
                int boostedG = paletteReader.ReadByte() * 4;
                int boostedB = paletteReader.ReadByte() * 4;

                //Safety (not needed if the program works as expected)
                if (boostedR >= 256 || boostedG >= 256 || boostedB >= 256)
                {
                    throw new CryoDataException($"Invalid color value. Original RGB bytes : '{boostedR/4}', '{boostedG/4}', '{boostedB/4}'");
                }

                colors.Add(new PaletteColor()
                {
                    Index = i,
                    R = (byte)boostedR,
                    G = (byte)boostedG,
                    B = (byte)boostedB,
                });
            }

            return new SubPalette()
            {
                Name = $"subPalette{paletteIndex}",
                Colors = colors,
                LocationInPalette = locationInPalette,
            };
        }

        public static IEnumerable<SubPalette> DecodePaletteArea(byte[] paletteData)
        {
            var result = new List<SubPalette>();

            using (var stream = new MemoryStream(paletteData))
            using (var paletteReader = new BinaryReader(stream))
            {
                if (paletteData.Length == 0)
                {
                    Console.WriteLine($"No palette.");
                    return result;
                } else
                {
                    Console.WriteLine($"There is subpalette(s).");
                }

                int count = 0;
                while (!IsEndOfPalette(paletteReader))
                {
                    Console.WriteLine($"Subpalette {count}...");
                    try
                    {
                        result.Add(DecodeSubPalette(paletteReader, count));
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

                return result;
            }
        }

    }
}
