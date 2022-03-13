#define VERBOSE

using System;
using System.Linq;

namespace HsqLib2.BinaryReader
{
    public class CustomBinaryReader : IBinaryReader
    {
        private System.IO.BinaryReader binaryReader;

        public long Position => binaryReader.BaseStream.Position;

        public long Length => binaryReader.BaseStream.Length;

        public bool EOF => binaryReader.BaseStream.Position == binaryReader.BaseStream.Length;

        public CustomBinaryReader(System.IO.BinaryReader binaryReader)
        {
            this.binaryReader = binaryReader;
        }

        public byte ReadByte()
        {
            byte b = binaryReader.ReadByte();
            #if VERBOSE
            Console.WriteLine($"ReadByte : {Convert.ToString(b, 16)}");
            #endif
            return b;

        }

        public byte[] ReadBytes(int count)
        {
            byte[] bytes = binaryReader.ReadBytes(count);
            #if VERBOSE
            Console.WriteLine($"ReadBytes : {string.Join(",",bytes.Select(b => Convert.ToString(b, 16) ))}");
            #endif
            return bytes;
        }

        public int ReadUInt16()
        {
            int v = binaryReader.ReadUInt16();
            #if VERBOSE
            Console.WriteLine($"ReadUInt16 : {v}");
            #endif
            return v;
        }
    }
}
