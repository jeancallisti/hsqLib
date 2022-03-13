using HsqLib2;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static CryoDataLib.TextLib.CryoTextMatadata;

namespace CryoDataLib.TextLib
{
    public class CryoTextIndex
    {
        public static int[] ReadIndex(BinaryReader reader)
        {
            var index = new List<int>();

            //the first value is the total size
            var indexByteSize = reader.ReadUInt16();

            index.Add(indexByteSize);

            var int16ToRead = indexByteSize / CryoTextAddressPair.AddressByteSize;
            while (index.Count < int16ToRead)
            {
                index.Add(reader.ReadUInt16());
            }
            return index.ToArray();
        }
    }
    public class CryoTextMatadata
    {
        public class CryoTextAddressPair
        {
            public static int AddressByteSize { get; } = 2;

            public int SentenceStartAddress { get; init; }
            public int SentenceEndAddress { get; init; }
        }

        public int TotalSize { get; init; }

        public IEnumerable<CryoTextAddressPair> Addresses { get; init; }
    }
    public class CryoTextDataInterpreter : ICryoDataInterpreter
    {

        private char ConvertChar(byte c)
        {
            //TODO: char substitution
            return (char)c;
        }

        private IEnumerable<CryoTextAddressPair> InterpretIndex(int[] indexData)
        {
            var addressPairs = new List<CryoTextAddressPair>();

            var sentenceStart = indexData[0];

            //Start at 1
            for (int i = 1; i < indexData.Length; i++)
            {
                var sentenceEnd = indexData[i];

                addressPairs.Add(new CryoTextAddressPair()
                {
                    SentenceStartAddress = sentenceStart,
                    SentenceEndAddress = sentenceEnd
                });

                sentenceStart = sentenceEnd; 
            }

            return addressPairs;
        }
        public async Task<CryoData> InterpretData(HsqFile file)
        {
            using (var stream = new MemoryStream(file.UnCompressedData))
            using (var reader = new BinaryReader(stream))
            {
                var indexData = CryoTextIndex.ReadIndex(reader);
                var addressPairs = InterpretIndex(indexData);

                var metadata = new CryoTextMatadata
                {
                    Addresses = addressPairs,
                    TotalSize = indexData[0],
                };

                var outputSentences = new Dictionary<int, string>();
                var output = new CryoTextData()
                {
                    Metadata = metadata,
                    Sentences = outputSentences,
                    SourceFile = file.SourceFile 
                };

                foreach (var addressPair in addressPairs)
                {
                    var sentenceLength = addressPair.SentenceEndAddress - addressPair.SentenceStartAddress;
                    var sentenceBytes = Enumerable
                                        .Range(0, sentenceLength)
                                        .Select(i => reader.ReadByte());

                    var sentence = string.Join("", sentenceBytes.Select(b => ConvertChar(b)));

                    Console.WriteLine(sentence);

                    output.Sentences.Add(addressPair.SentenceStartAddress, sentence);
                }

                return output;
            }
        }
    }
}
