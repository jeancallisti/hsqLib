using HsqLib2.BinaryReader;
using HsqLib2.HsqReader.Instructions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HsqLib2.HsqReader
{
    /// <summary>
    /// A HSQ-file starts with a 6 bytes header. The first 3 bytes is the size of the uncompressed file, 
    /// the following 2 bytes the size of the compressed file and the last byte is used as a checksum value.
    /// Adding all the 6 values should result in a hexadecimal value ending with 0xAB (or simply be 0xAB 
    /// if a single byte value was used for adding).
    /// 
    /// https://zwomp.com/index.php/2019/07/22/exploring-the-dune-files-part-1-the-hsq-file-format/
    /// </summary>
    public class HsqHeader
    {
        public static int HeaderSize { get; } = 6;

        public int UncompressedSize { get; private set; }
        public int CompressedSize { get; private set; }
        public int CheckSumByte { get; private set; }


        public static bool CheckHeaderValid(byte[] data)
        {
            if (data.Length != HeaderSize)
            {
                throw new HsqException("Hsq header has the wrong size.");
            }

            int validCheckSum = 171; //0xAB

            byte total = 0;
            for (int i =0; i<6; i++)
            {
                total += data[i];
            }

            return (total == validCheckSum);
        }

        public HsqHeader(byte[] data, bool? ignoreBadChecksum)
        {
            if (!(ignoreBadChecksum ?? false) && !CheckHeaderValid(data))
            {
                throw new HsqException("Hsq header did not pass the checksum test.");
            }

            int position = 0;

            // First 3 bytes = 24-bits int that we make 32 bits for convenience
            var _4bytes = new byte[] { data[0], data[1], data[2], 0 };
            UncompressedSize = (int) BitConverter.ToUInt32(_4bytes, 0);
            position += 3;

            // Subsequent 2 bytes = 16-bits int
            CompressedSize = BitConverter.ToUInt16(data, position);
            position += 2;

            //subsequent 1 byte = checksum byte
            CheckSumByte = data[position];
        }
    }

    public class HsqFile
    {
        public HsqHeader Header { get; private set; }
        public byte[] UnCompressedData { get; private set;}

        public HsqFile(HsqHeader header, byte[] uncompressedData)
        {
            Header = header;
            UnCompressedData = uncompressedData;
        }
    }

    //The delegate that we will provide to InstructionsParser to get its precious batches of 16 bits
    public delegate IEnumerable<bool> BitsReaderDelegate();



    public class HsqReader
    {
        private byte[] ReadHeader(IBinaryReader reader)
        {
            return reader.ReadBytes(HsqHeader.HeaderSize);
        }


        // Duplicates a sequence of bytes from somewhere in the output to somewhere else in the output.
        // See https://zwomp.com/index.php/2019/07/22/exploring-the-dune-files-part-1-the-hsq-file-format/
        private void DoMethod0(IBinaryReader reader, bool method0paramBit1, bool method0paramBit2, List<byte> output)
        {
            // How much we want to duplicate (the "length" of the sequence)
            var length = 2;
            length += method0paramBit1 ? 2 : 0;
            length += method0paramBit2 ? 1 : 0;

            //How far apart from the original we want to write the duplicate.
            //The distance is always negative.
            var distanceBytes = new byte[] { reader.ReadByte(), 0xFF };
            var distance = BitConverter.ToInt16(distanceBytes, 0);

            int start_offset = output.Count + distance;
            for (int i = 0; i < length; i++)
            {
                output.Add(output[start_offset + i]);
            }
        }

        // 
        // See https://zwomp.com/index.php/2019/07/22/exploring-the-dune-files-part-1-the-hsq-file-format/
        private void DoMethod1(IBinaryReader reader, List<byte> output)
        {
            var raw = reader.ReadUInt16();

            var length = raw & 7;

            if (length == 0)
                length = reader.ReadByte();

            if (length == 0)
                return;

            length += 2;
            var distance = raw >> 3;
            if (distance >= 0) distance -= 8192;


            int start_offset = output.Count + distance;
            for (int i = 0; i < length; i++)
            {
                output.Add(output[start_offset + i]);
            }
        }


        private void ProcessInstruction(IBinaryReader reader, Instruction instruction, List<byte> output)
        {
            switch(instruction.Type)
            {
                case InstructionType.CopyByte:
                    output.Add(reader.ReadByte());
                    break;
                case InstructionType.Method0:
                    DoMethod0(reader, instruction.BitParam1, instruction.BitParam2, output);
                    break;
                case InstructionType.Method1:
                    DoMethod1(reader, output);
                    break;
                default:
                    throw new NotSupportedException("Unsupported HSQ instruction.");
            }
        }


        public async Task<HsqFile> Unpack(IBinaryReader reader, bool? ignoreBadChecksum)
        {
            //TODO: optimize the output (stream?)
            var output = new List<byte>();

            var headerRaw = ReadHeader(reader);
            var header = new HsqHeader(headerRaw, ignoreBadChecksum);

            var instructionsReader = new InstructionsReader(reader);

            while (!reader.EOF)
            {
                var instruction = InstructionsBlock.ReadAndParseInstruction(instructionsReader);

                ProcessInstruction(reader, instruction, output);

                //+DEBUG
                Console.WriteLine(string.Join(",", output.ToArray().Select(b => Convert.ToString(b, 16))));
                //-DEBUG
            }

            return new HsqFile(header, output.ToArray());
        }
        
        public async Task<HsqFile> UnpackFile(FileStream fileStream, bool? ignoreBadChecksum)
        {
            using (var reader = new System.IO.BinaryReader(fileStream))
            {
                var customReader = new CustomBinaryReader(reader);
                return await Unpack(customReader, ignoreBadChecksum);
            }            
        }
    }
}
