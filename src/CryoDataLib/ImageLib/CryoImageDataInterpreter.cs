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

        private Palette InterpretPalette(int initialOffset, BinaryReader reader)
        {
            var palette = new List<PaletteColor>();

            //If the data starts immediately after the 2 bytes we've just read it means there was no room for a palette, duh!
            bool hasPalette = initialOffset > 2;

            if (!hasPalette)
            {
                Console.WriteLine($"No palette.");
            }
            else
            {
                int paletteDataSize = initialOffset - 2;

                if (paletteDataSize % 3 != 0)
                {
                    throw new CryoDataException("Palette data is not a multiple of 3");
                }

                var paletteData = new Stack<byte>(CryoImageIndex.ReadPalette(reader, paletteDataSize).Reverse());

                int paletteColorCount = paletteData.Count / 3;
                Console.WriteLine($"Palette has {paletteColorCount} colors");

                int colorIndex = 0;
                while (paletteData.Count > 0)
                {
                    palette.Add(new PaletteColor()
                    {
                        Index = colorIndex++,
                        R = paletteData.Pop(),
                        G = paletteData.Pop(),
                        B = paletteData.Pop(),
                    });
                }

            }

            return new Palette
            {
                Colors = palette
            };
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

                var palette = InterpretPalette(initialOffset, reader);

                long arrayStartsAt = reader.BaseStream.Position;

                var offsetsArray = InterpretOffsetsArray(reader);

                if (reader.BaseStream.Position != offsetsArray.First())
                {
                    throw new CryoDataException("Addresses don't match.");
                }
                var output = new CryoImageData()
                {
                    SourceFile = file.SourceFile,
                    Palette = palette,
                    Addresses = offsetsArray
                };

                return output;
            }
        }
    }
}
