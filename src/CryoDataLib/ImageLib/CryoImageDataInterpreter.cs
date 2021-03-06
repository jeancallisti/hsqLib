using CryoDataLib.ImageLib.Part;
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

        //The size of a line of pixels (in bytes) is not bytesCount or even bytesCount/2.
        //It's more subtle. Lines that have an odd number of pixels can leave garbage byte(s) at the end.
        //This is not very well documented neither on ZWomp website nor on bigs.fr website.
        private int CalculateBytesPerLine(int width)
        {
            //Add 1 before dividing by two to avoid rounding error if width is an odd number
            var result = (width + 1) / 2; 

            //Re-add 1 to transform an odd number of pixels to read into an even number of pixels to read.
            if (result % 2 == 1)
            {
                result++;
            }

            if (result == 0)
            {
                throw new CryoDataException("Unexpected : bytes per line == 0.");
            }

            return result;
        }

        private byte[] ByteToTwoPixels(byte b)
        {
            var twoPixels = new byte[2];

            //warning! The 4 leftmost bits are for the pixel that will be rendered second (i.e. pixels #1 and #2 are reversed).
            twoPixels[1] = (byte)((b & 240) >> 4); // b & 1111 0000
            twoPixels[0] = (byte)((b & 15));  // b & 0000 1111 << 4. the <<4 is to bring the value back in the same range as the other pixel.

            //Reminder : 0 means transparent

            return twoPixels;
        }

        private byte[] InterpretPartNotCompressed(int width, int height, byte[] rawPixelData)
        {
            try
            {
                var allPixels = new List<byte>();

                if (!rawPixelData.Any())
                {
                    throw new CryoDataException("Unexpected : no pixel data for uncompressed sprite.");
                }

                var bytesPerLine = CalculateBytesPerLine(width);

                using (var stream = new MemoryStream(rawPixelData))
                using (var reader = new BinaryReader(stream))
                {
                    for (int j = 0; j < height; j++)
                    {

                        var lineData = reader.ReadBytes(bytesPerLine);
                        var currentByte = 0;

                        byte[] twoPixels = new byte[2];

                        for (int i = 0; i < width; i++)
                        {
                            //Read a new byte every EVEN pixel (i.e. once out of two pixels), as one byte contains 2 pixels.
                            var timeToReadNewByte = i % 2 == 0;

                            if (timeToReadNewByte)
                            {
                                twoPixels = ByteToTwoPixels(lineData[currentByte]);
                                currentByte++;
                            }

                            //we alternate between the two pixels in our little buffer
                            var currentPixel = twoPixels[i % 2]; 

                            allPixels.Add(currentPixel);
                        }

                        //Garbage byte(s) at the very end
                        if (currentByte < lineData.Length)
                        {
                            var garbage = lineData.Skip(currentByte).Take(lineData.Length);
                            Console.WriteLine($"Line garbage bytes :{string.Join(",", garbage.Select(b => $"'{HexHelper.ByteToHexString(b)}'"))}.");
                        }
                    }
                }

                //Safety. Not needed if program works as expected
                if (allPixels.Count() != width * height)
                {
                    throw new CryoDataException("Image not properly decoded. Dimensions don't match");
                }

                return allPixels.ToArray();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error while trying to read uncompressed sprite.");
                throw;
            }
        }

        private bool ReadRLEIndicator(BinaryReader reader, out int sequenceLength)
        {
            var signedByte = reader.ReadSByte(); //Watch out! Here, unlike other places, we want SIGNED values from -128 to +127 (i.e. not 0 to 255).

            if (signedByte >= 0)
            {
                sequenceLength = signedByte +1; // +1 is by design
                return false; //Not RLE - just copy
            }

            // <0
            sequenceLength = -signedByte +1;  // +1 is by design
            return true; // RLE - repeat
        }

        /// <summary>
        /// Implementation by ... ???? (there were so many different repos and implementations that I couldn't find where I took this from!)
        /// If you recognize your implementation then please drop me a message.
        /// </summary>
        private byte[] InterpretRLECompressed(int width, int height, byte[] rawPixelData)
        {
            try
            {
                var result = new byte[width * height];
                var inputPos = 0;

                for (int y = 0; y != height; ++y)
                {
                    int dst = 0;
                    int line_remain = 4 * ((width + 3) / 4);

                    do
                    {
                        byte cmd = rawPixelData[inputPos++];

                        if ((cmd & 0x80) != 0)
                        { // 1000 0000 <=> < 0
                            int count = 257 - cmd;
                            byte value = rawPixelData[inputPos++];

                            byte p1 = (byte)(value & 0x0f);
                            byte p2 = (byte)(value >> 4);

                            for (int i = 0; i != count; ++i)
                            {
                                if (p1 != 0)
                                {
                                    result[y * width + dst] = p1;
                                }
                                dst++;
                                if (p2 != 0)
                                {
                                    result[y * width + dst] = p2;
                                }
                                dst++;
                            }
                            line_remain -= 2 * count;
                        }
                        else
                        { // >= 0
                            int count = cmd + 1;
                            for (int i = 0; i != count; ++i)
                            {
                                byte value = rawPixelData[inputPos++];

                                byte p1 = (byte)(value & 0x0f);
                                byte p2 = (byte)(value >> 4);

                                if (p1 != 0)
                                {
                                    result[y * width + dst] = p1;
                                }
                                dst++;
                                if (p2 != 0)
                                {
                                    result[y * width + dst] = p2;
                                }
                                dst++;
                            }
                            line_remain -= 2 * count;
                        }
                    } while (line_remain > 0);
                }

                return result;
            }
            catch (Exception)
            {
                Console.Error.WriteLine($"Error while trying to read RLE-compressed sprite.");
                throw;
            }
        }

        private AbstractPart InterpretPart(byte[] partData, int partIndex)
        {
            using (var stream = new MemoryStream(partData))
            using (var reader = new BinaryReader(stream))
            {
                // 1. Read data

                //The first two bytes contain mixed info (image width AND compression flag) that we'll need to parse
                var compressionAndWidth = reader.ReadUInt16();

                //Other metadata
                var height = reader.ReadByte();
                var paletteOffset = reader.ReadByte();

                //long --> int because sprites are small enough.
                var remainingBytesCount = (int)(partData.Length - reader.BaseStream.Position);

                //Actual sprite data
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
                        RawDataHexString = string.Join(" ", rawPixelData.Select(b => HexHelper.ByteToHexString(b))),
                    };
                }

                if (width == 0 && height == 0)
                {
                    throw new CryoDataException("Unexpected : Sprite of dimensions 0x0.");
                }

                byte[] uncompressedPixels = null;
                //Extra processing required to interpret RLE compression
                if (isCompressed)
                {
                    Console.WriteLine($"RLE-Compressed.");
                    uncompressedPixels = InterpretRLECompressed(width, height, rawPixelData);
                }
                else
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
                    RawDataHexString = string.Join(" ", rawPixelData.Select(b => HexHelper.ByteToHexString(b))),
                    UncompressedPixels = uncompressedPixels,
                    UncompressedPixelsHexString = string.Join(" ", uncompressedPixels.Select(p => HexHelper.ByteToHexString(p))),
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
                        Console.Error.WriteLine($"Details : {ex.Message}");
                        Console.Error.WriteLine($"{ex.StackTrace}");
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

                if (output.UnknownParts.Count() > 0)
                {
                    Console.WriteLine($"There were {output.UnknownParts.Count()} unknown parts.");
                }

                return output;
            }
        }
    }
}
