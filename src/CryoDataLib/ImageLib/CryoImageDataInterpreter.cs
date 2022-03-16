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

        public static int[] ReadOffsetsArray(BinaryReader reader)
        {
            var offsetArrayStartsAt = reader.BaseStream.Position;

            var data = new List<int>();

            //Read the first offset separately as we'll use the value for two different things
            data.Add(reader.ReadUInt16());

            var offsetArrayEndsAt = offsetArrayStartsAt + data[0];

            var itemsCount = data[0] / 2;

            for (int i = 1; i < itemsCount; i++) //Start at 1 because we've already read one.
            {
                data.Add(reader.ReadUInt16());
            }

            //For safety. Not needed if the program works as expected
            if (reader.BaseStream.Position != offsetArrayEndsAt)
            {
                throw new CryoDataException("Offsets array was not read correctly.");
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

    public abstract class AbstractPart
    {
        public string Name { get; init; }
        public int Index { get; init; }

        public byte[] RawData { get; init; }
    }

    public class ImagePart : AbstractPart
    {
        public bool IsCompressed { get; init; }
        public int Width { get; init; }
        public int Height { get; init; }
        public int PaletteOffset { get; init; }
        public byte?[] UncompressedPixels { get; init; } //An actual pixels array of size width*length. Null means transparent

        public SpriteWithPaletteOffset ToSpriteWithPaletteOffset()
        {
            return new SpriteWithPaletteOffset()
            {
                Name = Name,
                Pixels = UncompressedPixels,
                Width = Width,
                Height = Height,
                PaletteOffset = PaletteOffset
            };
        }
    }

    public class UnknownPart: AbstractPart
    {
    }

    //Only for export
    public abstract class JSonAbstractPart
    {
        public long AbsoluteStartAddress { get; init; }
        public long AbsoluteEndAddress { get; init; }
    }


    //Only for export
    public class JSonImagePart : JSonAbstractPart
    {
        public ImagePart ImagePart { get; init; }
    }

    //Only for export
    public class JSonUnknownPart : JSonAbstractPart
    {
        public UnknownPart UnknownPart { get; init; }

    }


    public class CryoImageDataInterpreter : ICryoDataInterpreter
    {
        public CryoImageDataInterpreter()
        {
        }

        // See https://www.bigs.fr/dune_old/#ch4
        // See https://zwomp.com/index.php/2020/07/01/exploring-the-dune-files-the-image-files/
        // WARNING : This function returns the absolute addresses in the file, not the relative addresses as they are stored.
        private IEnumerable<long> InterpretOffsetsArray(int[] offsetsArray, long initialOffset)
        {
            return offsetsArray.Select(addr => initialOffset+addr);
        }

        private bool InterpretCompressionAndWidth(int value16bits, out bool isCompressed, out int width)
        {
            // See https://zwomp.com/index.php/2021/09/01/exploring-the-dune-files-the-image-parts
            // "If the first two bytes are both zero, you are not looking at an image part"
            if (value16bits == 0)
            {
                isCompressed = false;
                width = 0;
                return false;
            }

            //TODO : Dunes.hsq, Dunes2.hsq and Dunes3.hsq work differently.
            //       See the very end of https://zwomp.com/index.php/2021/09/01/exploring-the-dune-files-the-image-parts

            // 1000 0000 0000 X0X0 == compressed
            // 0000 0000 0000 X0X0 == not compressed
            isCompressed = (value16bits > 32767); //Checks if heaviest bit is '1'

            width = value16bits & 32767; // Keep only lower bits : 0111 1111 1111 1111

            //Safety. Not needed if program works as expected
            if (width == 0)
            {
                throw new CryoDataException("Width was zero!");
            }

            return true;
        }

        private class PixelsReader
        {
            private BinaryReader _reader;
            private Stack<byte?> pixelsFIFO = new Stack<byte?>();

            public PixelsReader(BinaryReader reader)
            {
                _reader = reader;
            }

            //End of data
            public bool HasMore { get { return _reader.BaseStream.Position < _reader.BaseStream.Length; } }

            public byte? GetPixel()
            {
                if (!HasMore)
                {
                    return null;
                }

                //We've used up the pixels, let's reader more.
                if (pixelsFIFO.Count() == 0)
                {
                    var newByte = _reader.ReadByte();
                    var pixel1 = (byte)(newByte & 240); // b & 1111 0000
                    var pixel2 = (byte)(newByte & 15);  // b & 0000 1111

                    pixelsFIFO.Push(pixel2 != 0 ? pixel2 : null); //0 means transparent. we represent that as null.
                    pixelsFIFO.Push(pixel1 != 0 ? pixel1 : null);
                }

                return pixelsFIFO.Pop();
            }
        }

        private byte?[] InterpretPartNotCompressed(int width, int height, byte[] rawPixelData)
        {
            var allPixels = new List<byte?>();

            using (var stream = new MemoryStream(rawPixelData))
            using (var reader = new BinaryReader(stream))
            {
                var pixelsReader = new PixelsReader(reader);

                if (!pixelsReader.HasMore)
                {
                    return allPixels.ToArray();
                }

                var pixel = pixelsReader.GetPixel();
                
                int currentRow = 0;
                while (currentRow < height) {

                    int pixelRowCount = 0;

                    while (pixelRowCount < width) {

                        allPixels.Add(pixel);
                        pixel = pixelsReader.GetPixel();
                        pixelRowCount++;

                    }

                    currentRow++;
                }

                //Garbage pixels at the very end
                while (pixelsReader.HasMore)
                {
                    var garbage = pixelsReader.GetPixel();
                    var strGarbage = garbage.HasValue ? HexHelper.ByteToHexString(garbage.Value) : "null";
                    Console.WriteLine($"Garbage pixel '{strGarbage}'.");
                }

            }

            //Safety. Not needed if program works as expected
            if (allPixels.Count() != width*height)
            {
                throw new CryoDataException("Image not properly decoded. Dimension don't match");
            }

            return allPixels.ToArray();
        }

        private AbstractPart InterpretPart(byte[] partData, int partIndex)
        {
            using (var stream = new MemoryStream(partData))
            using (var reader = new BinaryReader(stream))
            {
                // 1. Read data
                // 
                //The first two bytes contain mixed info (image width AND compression flag) that we'll need to parse
                var compressionAndWidth = reader.ReadUInt16();
                var height = reader.ReadByte();
                var paletteOffset = reader.ReadByte();
                //long --> int because sprites are small enough.
                var remainingBytesCount = (int)(partData.Length - reader.BaseStream.Position);
                var rawPixelData = reader.ReadBytes(remainingBytesCount);

                //2. Interpret data

                if (!InterpretCompressionAndWidth(compressionAndWidth, out var isCompressed, out var width))
                {
                    Console.WriteLine($"Part {partIndex} does not seem to be an image part.");
                    return new UnknownPart
                    {
                        Index = partIndex,
                        Name = $"part{partIndex}",
                        RawData = rawPixelData,
                    };
                }

                byte?[] uncompressedPixels = null;
                //Extra processing required to interpret RLE compression
                if (isCompressed)
                {
                    Console.WriteLine($"Sprite is compressed. TODO.");
                    //TODO
                } else
                {
                    uncompressedPixels = InterpretPartNotCompressed(width, height, rawPixelData);
                }

                return new ImagePart
                {
                    Index = partIndex,
                    Name = $"part{partIndex}",
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
                int offsetsArrayStartsAt = CryoImageIndex.ReadInitialOffset(reader);

                var paletteDataSize = offsetsArrayStartsAt - CryoImageIndex.InitialOffsetSize; //Subtract the two bytes we've just read.
                var paletteData = CryoImageIndex.ReadPalette(reader, paletteDataSize);  
                var subPalettes = PaletteInterpreter.DecodePaletteArea(paletteData);

                //For control. This is not needed if the program works as expected
                if (reader.BaseStream.Position != offsetsArrayStartsAt) {
                    throw new CryoDataException("Palette was not read properly");
                }

                var offsetsArrayData = CryoImageIndex.ReadOffsetsArray(reader);

                Console.WriteLine($"Offsets array contains {offsetsArrayData.Length} parts...");

                var offsetsArrayAbsoluteAddresses = InterpretOffsetsArray(offsetsArrayData, offsetsArrayStartsAt).ToArray();

                //Safety. Not needed if the code works as expected.
                if (reader.BaseStream.Position != offsetsArrayAbsoluteAddresses.First())
                {
                    throw new CryoDataException("Addresses don't match.");
                }

                var addressPairs = Enumerable.Range(0, offsetsArrayAbsoluteAddresses.Length).Select(i => new
                {
                    AbsoluteStartAddress = offsetsArrayAbsoluteAddresses[i],
                    AbsoluteEndAddress = i < offsetsArrayAbsoluteAddresses.Length -1 ? 
                                                offsetsArrayAbsoluteAddresses[i+1] 
                                                : reader.BaseStream.Length //the very last address is implicit (end of file). We add it manually, lean and mean!
                }).ToList();

                var imageParts = new List<JSonImagePart>(); 
                var unknownParts = new List<JSonUnknownPart>();
                
                int partIndex = 0;
                addressPairs.ForEach(addressesPair =>
                {
                    try
                    {
                        Console.WriteLine($"Part {partIndex}...");

                        // long --> int because hopefully there's no such thing as a sprite so big that its size needs more than 32 bits :-)
                        var partLengthInBytes = (int)(addressesPair.AbsoluteEndAddress - addressesPair.AbsoluteStartAddress);

                        var partData = CryoImagePartData.ReadData(reader, partLengthInBytes);
                        var part = InterpretPart(partData, partIndex);
                        if (part is ImagePart)
                        {
                            imageParts.Add(new JSonImagePart()
                            {
                                ImagePart = (ImagePart)part,
                                AbsoluteEndAddress = addressesPair.AbsoluteEndAddress,
                                AbsoluteStartAddress = addressesPair.AbsoluteStartAddress
                            });
                        } else if (part is UnknownPart)
                        {
                            unknownParts.Add(new JSonUnknownPart()
                            {
                                UnknownPart = (UnknownPart)part,
                                AbsoluteEndAddress = addressesPair.AbsoluteEndAddress,
                                AbsoluteStartAddress = addressesPair.AbsoluteStartAddress
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"Could not process part {partIndex} of {offsetsArrayAbsoluteAddresses.Count()}.");
                        Console.Error.WriteLine($"Details : {ex.StackTrace}");
                    }
                    partIndex++;
                });

                var output = new CryoImageData()
                {
                    DataType = "image",
                    SourceFile = file.SourceFile,
                    SubPalettes = subPalettes,
                    ImageParts = imageParts,
                    UnknownParts = unknownParts,
                };

                return output;
            }
        }
    }
}
