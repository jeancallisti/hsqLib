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

        public string TextWithInstructions { get; init; }
        public string TextRaw { get; init; }
    }
    public class CryoTextDataInterpreter : ICryoDataInterpreter
    {
        public string Culture { get; }
        public Dictionary<byte, char> CharSetRedirect { get; }
        public IEnumerable<TextInstruction> TextInstructions { get; }


        public CryoTextDataInterpreter(CharsetRedirectTable charSet, IEnumerable<JsonTextInstruction> textInstructions)
        {
            Culture = charSet.Culture;

            CharSetRedirect = new Dictionary<byte, char>(
                charSet.Redirects.Select(item =>
                    new KeyValuePair<byte, char>(key: item.Key, value: item.Value))
            );

            TextInstructions = textInstructions.Select(i => new TextInstruction
            {
                TriggerByte = HexHelper.HexStringToByte(i.TriggerByteHex),
                FunctionName = i.FunctionName,
                Params = i.Params.Select(p => new TextInstructionParam
                {
                    Name = p.Name,
                    Mode = p.Mode,
                    Terminator = !string.IsNullOrEmpty(p.Terminator) ? HexHelper.HexStringToByte(p.Terminator) : null
                })
            }).ToList();
        }


        private byte[] ProcessInstruction(byte[] input, int instructionPosition, TextInstruction instruction)
        {
            //+DEBUG
            //if (instruction.TriggerByte == 134)
            //{
            //    int a = 1;
            //}
            //-DEBUG
            var instructionStart = instructionPosition;

            var strParams = new List<string>();

            var currentPosition = instructionPosition;

            currentPosition++; //Skip instruction byte

            foreach (var param in instruction.Params)
            {
                var paramName = param.Name;
                //Console.WriteLine($"Reading param {param.Name}.");


                switch (param.Mode)
                {
                    case "READ8":
                        var paramValue8 = input[currentPosition++];
                        //Console.WriteLine($"Value : {paramValue}.");

                        strParams.Add($"{param.Name}=\"{paramValue8}\"");

                        break;

                    case "READ16":
                        var paramBytes = new byte[2] { input[currentPosition++], input[currentPosition++] };
                        var paramValue16 = BitConverter.ToUInt16(paramBytes, 0);
                        //Console.WriteLine($"Value : {paramValue}.");

                        strParams.Add($"{param.Name}=\"{paramValue16}\"");

                        break;

                    case "READUNTIL":
                        var endByte = param.Terminator;
                        var buffer = new List<byte>();
                        var c = input[currentPosition++];
                        while (c != endByte && currentPosition < input.Length)
                        {
                            buffer.Add(c);
                            c = input[currentPosition++];
                        }
                        string asString = new string(buffer.Select(b => (char)b).ToArray());

                        //Console.WriteLine($"Value : '{asString}'.");
                        strParams.Add($"{asString}");

                        break;

                    default:
                        throw new NotImplementedException($"Not implemented : {param.Mode}");
                }
            }

            var instructionEnd = currentPosition;

            var sequenceToReplace = input.Skip(instructionStart).Take(instructionEnd-instructionStart).ToArray();

            var strInstruction = "";
            // it's an open/close tag instruction
            if (instruction.Params.Any(p => p.Mode == "READUNTIL"))
            {
                strInstruction = $"<{instruction.FunctionName}>{strParams.First()}</{instruction.FunctionName}>";
            } 
            
            //It's a standard instruction with a finite set of parameters
            else
            {
                strInstruction = $"<{instruction.FunctionName} {string.Join(" ", strParams)} />";
            }

            var asBytes = strInstruction.ToCharArray().Select(c => HexHelper.SafeCharToByte(c)).ToArray();

            input = input.Replace(sequenceToReplace, asBytes);

            return input;
        }
        /// <summary>
        /// There are some places where a sequence of bytes is neither plain text nor
        /// special characters to replace, but instead special instructions for text replacement.
        /// For example, text like "Take Gurney Halleck to Carthag-Tuek" will appear as
        /// "Take Gurney Halleck to 0x80 0x00 0x02-0x80 0x00 0x11".
        /// where 0x80 is a special instructiona and {0x00 0x02} is its parameter.
        /// We convert that to  "Take Gurney Halleck to %SIETCHNAME(value)%-%SIETCHNAME(value)%". 
        /// We surround variable names with '%' by choice, to resemble environment variables and for easy text search/replace.
        /// </summary>
        private byte[] ParseTextInstructions(byte[] input)
        {
            var instructionsToDetect = TextInstructions.Select(i => i.TriggerByte);

            var detectedInstructions = input
                                        .Intersect(instructionsToDetect)
                                        .ToList();

            if (!detectedInstructions.Any())
            {
                // return as-is
                return input;
            }

            detectedInstructions.ForEach(instToProcess =>
            {
                var position = input.ToList().IndexOf(instToProcess);
                
                if (position >= 0)
                {
                    var instructionData = TextInstructions.First(i => i.TriggerByte == instToProcess);

                    while (position >= 0)
                    {
                        input = ProcessInstruction(input, position, instructionData);

                        //Keep searching, there might be several instructions of the same kind
                        position = input.ToList().IndexOf(instToProcess);
                    }
                }

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
                                        .Select(i => reader.ReadByte())
                                        .ToArray();

                    try
                    {
                        var asStringRaw = string.Join("", sentenceBytes.Select(b => ConvertChar(b)));

                        Console.WriteLine(asStringRaw);

                        sentenceBytes = ParseTextInstructions(sentenceBytes.ToArray());

                        var asStringWithInstructions = string.Join("", sentenceBytes.Select(b => ConvertChar(b)));

                        output.Sentences.Add(
                            addressPair.SentenceStartAddress,
                            new CryoSentenceData
                            {
                                Index = index,
                                SentenceStartAddress = addressPair.SentenceStartAddress,
                                SentenceEndAddress = addressPair.SentenceEndAddress,
                                TextRaw = asStringRaw,
                                TextWithInstructions = asStringWithInstructions
                            });
                    } catch (Exception ex)
                    {
                        Console.WriteLine($"ERROR: Failed to process sentence at address '{addressPairs[index].SentenceStartAddress}'.");
                        Console.WriteLine($"ERROR: Details: '{ex.Message}'.");
                        Console.WriteLine($"ERROR: String was '{ string.Join("", sentenceBytes.Select(b => ConvertChar(b))) }'");
                        //throw ex;
                    }


                }

                return output;
            }
        }
    }
}
