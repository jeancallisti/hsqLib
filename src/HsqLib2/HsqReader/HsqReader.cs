using HsqLib2.BinaryReader;
using HsqLib2.HsqReader.Instructions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HsqLib2.HsqReader
{
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


        public async Task<HsqFile> Unpack(IBinaryReader reader, bool? ignoreBadChecksum = false)
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
                //Console.WriteLine(string.Join(",", output.ToArray().Select(b => Convert.ToString(b, 16))));
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
