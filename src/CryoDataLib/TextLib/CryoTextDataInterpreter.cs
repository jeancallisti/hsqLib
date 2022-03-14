using HsqLib2;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

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

    public class CryoTextAddressPair
    {
        public static int AddressByteSize { get; } = 2;

        public int SentenceStartAddress { get; init; }
        public int SentenceEndAddress { get; init; }
    }

    public class CryoSentenceData
    {
        public int Index { get; init; }
        public int SentenceStartAddress { get; init; }
        public int SentenceEndAddress { get; init; }

        public string Text { get; init; }
    }
    public class CryoTextDataInterpreter : ICryoDataInterpreter
    {
        public string Culture { get; }
        public Dictionary<byte, char> CharSetRedirect { get; }

        public CryoTextDataInterpreter(CharsetRedirectTable charSet)
        {
            Culture = charSet.Culture;

            CharSetRedirect = new Dictionary<byte, char>(
                charSet.Redirects.Select(item => 
                    new KeyValuePair<byte, char>(key: item.Key, value: item.Value))
            );
        }

        private char ConvertChar(byte c)
        {
            if (!CharSetRedirect.ContainsKey(c))
            {
                // In C#, unlike C, char type is a two-byte unicode char.
                // the modern equivalent of the old wchar_t. Ready to use!
                return (char)c;
            }
            
            return CharSetRedirect[c];
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
                var addressPairs = InterpretIndex(indexData).ToArray();

                var output = new CryoTextData()
                {
                    SourceFile = file.SourceFile 
                };

                foreach(var index in Enumerable.Range(0, addressPairs.Length))
                {
                    var addressPair = addressPairs[index];

                    var sentenceLength = addressPair.SentenceEndAddress - addressPair.SentenceStartAddress;
                    var sentenceBytes = Enumerable
                                        .Range(0, sentenceLength)
                                        .Select(i => reader.ReadByte());

                    var sentence = string.Join("", sentenceBytes.Select(b => ConvertChar(b)));

                    Console.WriteLine(sentence);

                    output.Sentences.Add(
                        addressPair.SentenceStartAddress,
                        new CryoSentenceData
                        {
                            Index = index,
                            SentenceStartAddress = addressPair.SentenceStartAddress,
                            SentenceEndAddress = addressPair.SentenceEndAddress,
                            Text = sentence
                        });

                }

                return output;
            }
        }
    }
}
