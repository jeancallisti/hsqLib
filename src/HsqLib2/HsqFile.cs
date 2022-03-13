using System;

namespace HsqLib2
{
    public class HsqFile
    {
        public string SourceFile { get; init; }
        public HsqHeader Header { get; init; }
        public byte[] UnCompressedData { get; init; }
    }

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

        public int UncompressedSize { get; init; }
        public int CompressedSize { get; init; }
        public int CheckSumByte { get; init; }


        public static bool IsChecksumValid(byte[] data)
        {
            if (data.Length != HeaderSize)
            {
                throw new HsqException("Hsq header has the wrong size.");
            }

            int validCheckSum = 171; //0xAB

            byte total = 0;
            for (int i = 0; i < 6; i++)
            {
                total += data[i];
            }

            return (total == validCheckSum);
        }

        //Only for Json deserialize
        public HsqHeader() { }

        public HsqHeader(byte[] data)
        {
            if (data.Length != HeaderSize)
            {
                throw new HsqException("Header should be 6 bytes long");
            }

            int position = 0;

            // First 3 bytes = 24-bits int that we make 32 bits for convenience
            var _4bytes = new byte[] { data[position++], data[position++], data[position++], 0 };
            UncompressedSize = (int)BitConverter.ToUInt32(_4bytes, 0);

            // Subsequent 2 bytes = 16-bits int
            CompressedSize = BitConverter.ToUInt16(data, position);
            position += 2;

            //subsequent 1 byte = checksum byte
            CheckSumByte = data[position++];
        }
    }
}
