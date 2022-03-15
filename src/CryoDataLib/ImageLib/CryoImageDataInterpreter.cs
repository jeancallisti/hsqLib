using HsqLib2;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CryoDataLib.ImageLib
{
    public class CryoImageIndex
    {
        public static int InitialOffsetSize { get; } = 2;

        /// <summary>
        /// First 2 bytes of the data. Offset to "Offsets aray"
        /// See https://zwomp.com/index.php/2020/07/01/exploring-the-dune-files-the-image-files/
        /// </summary>
        public static int ReadInitialOffset(BinaryReader reader)
        {
            return reader.ReadUInt16();
        }

        public static byte[] ReadPalette(BinaryReader reader, int nbBytes)
        {
            return reader.ReadBytes(nbBytes);
        }
    }

    public class PaletteColor
    {

        public int Index { get; init; }
        public byte R { get; init; }
        public byte G { get; init; }
        public byte B { get; init; }
    }

    public class Palette
    {
        public IEnumerable<PaletteColor> Colors { get; init; }
    }

    public class CryoImageDataInterpreter : ICryoDataInterpreter
    {


        public CryoImageDataInterpreter()
        {
        }

        private static bool IsEndOfPalette(BinaryReader paletteReader)
        {
            var nextWord = paletteReader.ReadUInt16();
            //Rewind the two bytes we've just read.
            paletteReader.BaseStream.Position -= 2;

            //The palette data always ends with 0xFF 0xFF
            return (nextWord == 65535);
        }

        private Palette DecodePalette(byte[] paletteData)
        {
            using (var stream = new MemoryStream(paletteData))
            using (var paletteReader = new BinaryReader(stream))
            {
                if (paletteData.Length == 0)
                {
                    Console.WriteLine($"No palette.");
                    return new Palette
                    {
                        Colors = new List<PaletteColor>()
                    };
                }

                var colors = new List<PaletteColor>();

                while (!IsEndOfPalette(paletteReader))
                {
                    try
                    {

                    } catch {
                        Console.Error.WriteLine("Could not process palette.");
                    }


                }

                //var paletteData = new Stack<byte>(CryoImageIndex.ReadPalette(reader, paletteDataSize).Reverse());

                //int paletteColorCount = paletteData.Count / 3;
                //Console.WriteLine($"Palette has {paletteColorCount} colors");

                //int colorIndex = 0;
                //while (paletteData.Count > 0)
                //{
                //    palette.Add(new PaletteColor()
                //    {
                //        Index = colorIndex++,
                //        R = paletteData.Pop(),
                //        G = paletteData.Pop(),
                //        B = paletteData.Pop(),
                //    });
                //}



                return new Palette
                {
                    Colors = colors
                };
            }
        }


        private IEnumerable<long> InterpretOffsetsArray(BinaryReader reader)
        {
            long arrayStartsAt = reader.BaseStream.Position;
            var arrayLengthInBytes = reader.ReadUInt16();
            reader.BaseStream.Position -= 2; //rewind the 2 bytes we've just read.

            var arrayItemsCount = arrayLengthInBytes / 2; //2 bytes per address

            var addresses = new List<long>();
            for (int i=0; i<arrayItemsCount; i++)
            {
                addresses.Add(arrayStartsAt+reader.ReadUInt16());
            }

            Console.WriteLine($"Read {addresses.Count} addresses");

            return addresses;
        }

        /// <summary>
        /// See https://zwomp.com/index.php/2020/07/01/exploring-the-dune-files-the-image-files/
        /// See https://www.bigs.fr/dune_old/#ch4
        /// </summary>
        public async Task<CryoData> InterpretData(HsqFile file)
        {
            using (var stream = new MemoryStream(file.UnCompressedData))
            using (var reader = new BinaryReader(stream))
            {
                int initialOffset = CryoImageIndex.ReadInitialOffset(reader);

                var paletteDataSize = initialOffset - CryoImageIndex.InitialOffsetSize; //Subtract the two bytes we've just read.

                var paletteData = CryoImageIndex.ReadPalette(reader, paletteDataSize);  
                
                DecodePalette(paletteData);

                long arrayStartsAt = reader.BaseStream.Position;

                var offsetsArray = InterpretOffsetsArray(reader);

                if (reader.BaseStream.Position != offsetsArray.First())
                {
                    throw new CryoDataException("Addresses don't match.");
                }
                var output = new CryoImageData()
                {
                    SourceFile = file.SourceFile,
                    //Palette = palette,
                    Addresses = offsetsArray
                };

                return output;
            }
        }
    }
}
