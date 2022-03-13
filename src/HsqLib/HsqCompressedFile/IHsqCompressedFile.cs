
namespace HsqLib.HsqCompressedFile
{
    public interface IHsqCompressedFile
    {
        bool EOF { get; }
        byte GetNextByte();
        byte[] GetNextWord();
        byte[] GetHeaderBytes();
    }
}
