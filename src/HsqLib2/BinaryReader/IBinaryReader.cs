
namespace HsqLib2.BinaryReader
{
    public interface IBinaryReader
    {
        public byte ReadByte();
        public byte[] ReadBytes(int count);
        public int ReadUInt16();

        public long Position { get; }
        public long Length { get; }
        public bool EOF { get; }
    }
}
