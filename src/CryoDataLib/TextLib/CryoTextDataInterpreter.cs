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
        public IEnumerable<Tuple<byte[], byte[]>> SpecialStrings { get; }


        public CryoTextDataInterpreter(CharsetRedirectTable charSet, Dictionary<string, string> specialStrings)
        {
            Culture = charSet.Culture;

            CharSetRedirect = new Dictionary<byte, char>(
                charSet.Redirects.Select(item =>
                    new KeyValuePair<byte, char>(key: item.Key, value: item.Value))
            );

            //Store which special sequences of bytes must be replaced, and by what.
            //as we do it, we convert everything to array of bytes instead of plain strings, for optimization
            SpecialStrings = specialStrings.ToArray().Select(item => new Tuple<byte[], byte[]>
            (
                // "0x55,0x56,0x57" --> { 85, 86, 87 }
                item1: SpecialValues.HexStringToBytesSequence(item.Key),

                // "UVW" --> "%UVW%" --> { 37, 85, 86, 87, 37 }
                item2: SpecialValues.StringToBytesSequence($"%{item.Value}%")
            )).ToArray();
        }



        /// <summary>
        /// There are some places where a sequence of bytes is neither plain text nor
        /// special characters to replace, but instead placeholders for external variables.
        /// For example, text like "Take Gurney Halleck to Carthag-Tuek" will appear as
        /// "Take Gurnay Halleck to 0x80 0x00 0x02-0x80 0x00 0x11"
        /// where 0x80 0x00 0x02 is a variable for the first part of the sietch's name and 0x80 0x00 0x11 is a variable for the second part.
        /// We convert that to  "Take Gurnay Halleck to %SIETCHNAME1%-%SIETCHNAME2%". 
        /// We surround variable names with '%' by choice, to resemble environment variables.
        /// </summary>
        private byte[] ReplaceSpecialStrings(byte[] input)
        {
            if (!input.Contains(SpecialValues.EscapeByte))
            {
                return input;
            }

            SpecialStrings.ToList().ForEach(tuple =>
            {
                var toFind = tuple.Item1;
                var replacement = tuple.Item2;

                input = input.Replace(toFind, replacement);
            });

            return input;
        }

        private char ConvertChar(byte c)
        {

            if (c == 254) //0xFE
            {
                return '\n';
            }

            if (c == 13) //0x0D : Carriage return
            {
                //I'm not sure why they would have TWO different newline symbols.
                //They use 0xFE almost everywhere but in some places they use this instead.
                return '\n';
            }

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

                    sentenceBytes = ReplaceSpecialStrings(sentenceBytes.ToArray());

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
