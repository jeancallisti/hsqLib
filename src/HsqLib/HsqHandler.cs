using HsqLib.HsqCompressedFile;
using HsqLib.HsqCompressedFile.Instructions;
using System;
using System.Collections.Generic;

namespace HsqLib
{
    /// <summary>
    /// Version 1 of the library (verbatim from ZWomp's source code)
    /// </summary>
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

}
