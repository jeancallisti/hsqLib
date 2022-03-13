#define VERBOSE
using System;
using System.Linq;

namespace HsqLib.HsqCompressedFile
{

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
            var b = _data[_offset++];
#if VERBOSE
            Console.WriteLine($"ReadByte : {Convert.ToString(b, 16)}");
#endif
            return b;
        }

        public byte[] GetNextWord()
        {
            var result = new byte[] { _data[_offset], _data[_offset + 1] };
            _offset += 2;

#if VERBOSE
            Console.WriteLine($"ReadUint16 : {string.Join(",", result.Select(b => Convert.ToString(b, 16)))}");
#endif
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
}
