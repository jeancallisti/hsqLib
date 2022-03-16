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

        public static byte[] ReadOffsetsArray(BinaryReader reader)
        {
            var sizeInBytes = reader.ReadUInt16();
            //Rewind the two bytes we've just read
            reader.BaseStream.Position -= 2;

            var data = new List<byte>();
            for (int i = 0; i < sizeInBytes; i++)
            {
                data.Add(reader.ReadByte());
            }

            return data.ToArray();
        }

    }

    public class CryoImagePartData
    {
        public static byte[] ReadData(BinaryReader reader, int nbBytes)
        {
            return reader.ReadBytes(nbBytes);
        }
    }

    public class Part
    {
        public long AbsoluteStartAddress { get; init; }
        public long AbsoluteEndAddress { get; init; }
        public bool IsCompressed { get; init; }
        public int Width { get; init; }
        public int Height { get; init; }
        public int PaletteOffset { get; init; }
        public byte[] RawData { get; init; } // Where each byte is two pixels or has RLE encoding
        public byte[] UncompressedPixels { get; init; } //An actual pixels array of size width*length

        public SpriteWithPaletteOffset ToSpriteWithPaletteOffset()
        {
            return new SpriteWithPaletteOffset()
            {
                Pixels = UncompressedPixels,
                Width = Width,
                Height = Height,
                PaletteOffset = PaletteOffset
            };
        }
    }


    public class CryoImageDataInterpreter : ICryoDataInterpreter
    {
        public CryoImageDataInterpreter()
        {
        }

        // See https://www.bigs.fr/dune_old/#ch4
        // See https://zwomp.com/index.php/2020/07/01/exploring-the-dune-files-the-image-files/
        // WARNING : This function returns the absolute addresses in the file, not the relative addresses as they are stored.
        private IEnumerable<long> InterpretOffsetsArray(byte[] offsetsArrayData, long initialOffset)
        {
            //The very first offset's value also happens to give us an indication on the size
            var sizeInBytes = BitConverter.ToInt16( new byte[2] { offsetsArrayData[0], offsetsArrayData[1] }, 0);

            //Control
            if (sizeInBytes != offsetsArrayData.Length)
            {
                throw new CryoDataException("Invalid offsets array size.");
            }

            var addresses = new List<long>();
            var partsCount = sizeInBytes / 2;

            Console.WriteLine($"Offsets array contains {partsCount} parts...");

            using (var stream = new MemoryStream(offsetsArrayData))
            using (var reader = new BinaryReader(stream))
            {
                for (int i = 0; i < partsCount; i++)
                {
                    addresses.Add(initialOffset + reader.ReadUInt16());
                }
            }

            return addresses;
        }

        private void InterpretCompressionAndWidth(int value16bits, out bool isCompressed, out int width)
        {
            // 1000 0000 0000 X0X0 == compressed
            // 0000 0000 0000 X0X0 == not compressed
            isCompressed = (value16bits > 32767); //Checks if heaviest bit is '1'

            width = value16bits & 32767; // Keep only lower bits : 0111 1111 1111 1111
        }

        private byte[] InterpretPartNotCompressed(int width, int height, byte[] rawPixelData)
        {
            var allPixels = new List<byte>();

            using (var stream = new MemoryStream(rawPixelData))
            using (var reader = new BinaryReader(stream))
            {
                for (int j = 0; j < height; j++) {

                    int pixelCount = 0;
                    var pixelsRow = new List<byte>();

                    for (int i = 0; i < width; i++)
                    {
                        //Subtle! there can be garbage at the end of a row.
                        //we need to keep track of how many actual pixels we've decoded!
                        if (pixelCount < width)
                        {
                            var b = reader.ReadByte(); //Each byte is actually two pixels.

                            var pixel1 = (byte)(b & 240); // b & 1111 0000
                            pixelsRow.Add(pixel1);
                            pixelCount++;

                            var pixel2 = (byte)(b & 15);  // b & 0000 1111

                            if (pixelCount < width)
                            {
                                pixelsRow.Add(pixel1);
                                pixelCount++;
                            } else
                            {
                                Console.WriteLine($"row {j} : Garbage pixel '{HexHelper.ByteToHexString(pixel2)}'.");
                            }

                        }
                    }
                    allPixels.AddRange(pixelsRow);
                }

            }

            //Safety. Not needed if program works as expected
            if (allPixels.Count() != width*height)
            {
                throw new CryoDataException("Image not properly decoded. Dimension don't match");
            }

            return allPixels.ToArray();
        }

        private Part InterpretPart(long absoluteStartAddress, long absoluteEndAddress, byte[] partData)
        {
            using (var stream = new MemoryStream(partData))
            using (var reader = new BinaryReader(stream))
            {
                //The first two bytes contain mixed info (image width AND compression flag) that we'll need to parse
                var compressionAndWidth = reader.ReadUInt16();
                var height = reader.ReadByte();
                var paletteOffset = reader.ReadByte();
                //long --> int because sprites are small enough.
                var remainingBytesCount = (int)(partData.Length - reader.BaseStream.Position);
                var rawPixelData = reader.ReadBytes(remainingBytesCount);

                //Start interpreting data

                InterpretCompressionAndWidth(compressionAndWidth, out var isCompressed, out var width);

                byte[] uncompressedPixels = null;
                //Extra processing required to interpret RLE compression
                if (isCompressed)
                {
                    Console.WriteLine($"Sprite is compressed. TODO.");
                    //TODO
                } else
                {
                    uncompressedPixels = InterpretPartNotCompressed(width, height, rawPixelData);
                }

                return new Part
                {
                    AbsoluteStartAddress = absoluteStartAddress,
                    AbsoluteEndAddress = absoluteEndAddress,
                    IsCompressed = isCompressed,
                    Width = width,
                    Height = height,
                    PaletteOffset = paletteOffset,
                    RawData = rawPixelData,
                    UncompressedPixels = uncompressedPixels
                };
            }
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
                var subPalettes = PaletteInterpreter.DecodePalette(paletteData);

                var offsetsArrayStartsAt = reader.BaseStream.Position;
                var offsetsArrayData = CryoImageIndex.ReadOffsetsArray(reader);

                var offsetsArray = InterpretOffsetsArray(offsetsArrayData, offsetsArrayStartsAt).ToArray();

                //Safety. Not needed if the code works as expected.
                if (reader.BaseStream.Position != offsetsArray.First())
                {
                    throw new CryoDataException("Addresses don't match.");
                }

                var addressPairs = Enumerable.Range(0, offsetsArray.Length).Select(i => new
                {
                    AbsoluteStartAddress = offsetsArray[i],
                    AbsoluteEndAddress = i < offsetsArray.Length -1 ? 
                                                offsetsArray[i+1] 
                                                : reader.BaseStream.Length //the very last address is implicit (end of file). We add it manually, lean and mean!
                }).ToList();

                var parts = new List<Part>(); 
                int partIndex = 0;
                addressPairs.ForEach(addressesPair =>
                {
                    try
                    {
                        Console.WriteLine($"Part {partIndex}...");

                        // long --> int because hopefully there's no such thing as a sprite so big that its size needs more than 32 bits :-)
                        var partLengthInBytes = (int)(addressesPair.AbsoluteEndAddress - addressesPair.AbsoluteStartAddress);

                        var partData = CryoImagePartData.ReadData(reader, partLengthInBytes);

                        parts.Add(InterpretPart(addressesPair.AbsoluteStartAddress, addressesPair.AbsoluteEndAddress, partData));
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"Could not process part {partIndex} of {offsetsArray.Count()}.");
                        Console.Error.WriteLine($"Details : {ex.StackTrace}");
                    }
                    partIndex++;
                });

                var output = new CryoImageData()
                {
                    DataType = "image",
                    SourceFile = file.SourceFile,
                    SubPalettes = subPalettes,
                    Parts = parts
                };

                return output;
            }
        }
    }
}
