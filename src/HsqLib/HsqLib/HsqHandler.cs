using System;
using System.Collections.Generic;

namespace HsqLib
{
    public class HsqHandler
    {
        public static void Uncompress(IHsqCompressedFile source_file, IList<byte> destination_file)
        {
            var instructions = new InstructionsReader(source_file);

            while (!source_file.EOF)
            {
                var step = instructions.GetNextStep();

                if (step is CopyByte)
                {
                    destination_file.Add(source_file.GetNextByte());
                    continue;
                }
                else if (step is DoMethodZero)
                {
                    var method0 = (DoMethodZero)step;

                    int start_offset = destination_file.Count + method0.Distance;
                    for (int i = 0; i < method0.Length; i++)
                    {
                        destination_file.Add(destination_file[start_offset + i]);
                    }
                }
                else if (step is DoMethodOne)
                {
                    var method1 = (DoMethodOne)step;

                    if (method1.EOF)
                        break;

                    int start_offset = destination_file.Count + method1.Distance;
                    for (int i = 0; i < method1.Length; i++)
                    {
                        destination_file.Add(destination_file[start_offset + i]);
                    }
                }
                else
                {
                    throw new Exception("Unsupported step.");
                }
            }
        }

        public static bool ValidateOutputSize(IHsqCompressedFile input, IList<byte> output)
        {
            var header = input.GetHeaderBytes();

            var size = BitConverter.ToInt32(new byte[] { header[0], header[1], header[2], 0x00 }, 0);

            return (size == output.Count);
        }

        public static bool ValidateHeader(IHsqCompressedFile input)
        {
            int test = 171;

            var header = input.GetHeaderBytes();

            test -= BitConverter.ToInt16(new byte[] { header[0], 0x00 }, 0);
            test -= BitConverter.ToInt16(new byte[] { header[1], 0x00 }, 0);
            test -= BitConverter.ToInt16(new byte[] { header[2], 0x00 }, 0);
            test -= BitConverter.ToInt16(new byte[] { header[3], 0x00 }, 0);
            test -= BitConverter.ToInt16(new byte[] { header[4], 0x00 }, 0);

            while (test < 0)
                test += 256;

            var checksum = BitConverter.ToInt16(new byte[] { header[5], 0x00 }, 0);

            return (test == checksum);
        }
    }

    public interface IHsqCompressedFile
    {
        bool EOF { get; }
        byte GetNextByte();
        byte[] GetNextWord();
        byte[] GetHeaderBytes();
    }

    public class HsqCompressedFile : IHsqCompressedFile
    {
        byte[] _data;
        int _offset = 6;

        public HsqCompressedFile(byte[] data)
        {
            _data = data;
        }

        public bool EOF
        {
            get
            {
                return (_offset == _data.Length);
            }
        }

        public int GetCompressedFileSize()
        {
            return BitConverter.ToUInt16(_data, 3);
        }

        public int GetUncompressedFileSize()
        {
            var bytes = new byte[] { _data[0], _data[1], _data[2], 0 };
            return BitConverter.ToUInt16(bytes, 0);
        }

        public byte GetNextByte()
        {
            return _data[_offset++];
        }

        public byte[] GetNextWord()
        {
            var result = new byte[] { _data[_offset], _data[_offset + 1] };
            _offset += 2;
            return result;
        }

        public byte[] GetHeaderBytes()
        {
            return new byte[] { _data[0], _data[1], _data[2],
                                _data[3], _data[4], _data[5],};
        }

        public int CurrentOffset
        {
            get
            {
                return _offset;
            }
        }

    }

    public interface IInstructionsReader
    {
        Step GetNextStep();
        bool ReadNextBit();
    }

    public class InstructionsReader : IInstructionsReader
    {

        IHsqCompressedFile _source_file;
        int _instruction_step = 16;
        int _raw_instructions = 0;

        public InstructionsReader(IHsqCompressedFile source_file)
        {
            _source_file = source_file;
        }

        public Step GetNextStep()
        {
            if (ReadNextBit()) // If just Copy next byte
                return new CopyByte();

            if (ReadNextBit()) // If doing Method One
            {
                return new DoMethodOne(_source_file);
            }

            // Doing Method Zero
            return new DoMethodZero(this, _source_file);
        }

        public bool ReadNextBit()
        {
            if (_instruction_step == 16)
            {
                _raw_instructions = BitConverter.ToInt16(_source_file.GetNextWord(), 0);
                _instruction_step = 0;
            }

            var result = (_raw_instructions & (int)(Math.Pow(2, _instruction_step))) > 0;
            _instruction_step++;

            return result;
        }
    }

    public abstract class Step
    {

    }

    public class CopyByte : Step
    {
    }

    public class DoMethodZero : Step
    {
        public DoMethodZero(IInstructionsReader reader,
            IHsqCompressedFile source_file)
        {
            Length = 2;
            Length += reader.ReadNextBit() ? 2 : 0;
            Length += reader.ReadNextBit() ? 1 : 0;

            Distance = BitConverter.ToInt16(new Byte[]
                { source_file.GetNextByte(), 0xFF }, 0);
        }

        public int Length;
        public int Distance;
    }

    public class DoMethodOne : Step
    {
        public DoMethodOne(IHsqCompressedFile source_file)
        {
            Distance = 0;
            Length = 0;

            var raw = BitConverter.ToInt16(source_file.GetNextWord(), 0);

            Length += raw & 7;

            if (Length == 0)
                Length = source_file.GetNextByte();

            if (Length == 0)
            {
                EOF = true;
                return;
            }

            Length += 2;

            Distance = raw >> 3;
        }

        public int Length;
        public int Distance;
        public bool EOF;
    }
}
