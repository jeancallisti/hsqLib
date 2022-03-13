using HsqLib2.BinaryReader;
using System;
using System.Collections.Generic;
using System.Linq;


namespace HsqLib2.HsqReader.Instructions
{
    public class InstructionsBlock
    {
        //Blocks of instructions come as a pair of bytes
        public static int InstructionsBlockSize { get; } = 2;

        private static bool GetBitValue(int sourceUint16, int bitPosition)
        {
            return (sourceUint16 & (int)(Math.Pow(2, bitPosition))) > 0;
        }

        private static byte[] ReadInstructionsBlock(IBinaryReader reader)
        {
            return reader.ReadBytes(InstructionsBlockSize);
        }

        public static IEnumerable<bool> ToBits(ushort uint16)
        {
            return Enumerable
                    .Range(0, 16)
                    .Select(position => GetBitValue(uint16, position))
                    .ToArray();
        }

        public static IEnumerable<bool> Read16Bits(IBinaryReader reader)
        {
                //By design, we expect to find 2 bytes that will be our next instructions.
                var instructionsAsBytes = ReadInstructionsBlock(reader);

                //We get one 16-bits number from the two bytes, then we'll read its bits one by one.
                var instructionsAsInt = BitConverter.ToUInt16(instructionsAsBytes, 0);

                //Convert to array of 16 bits.
                return ToBits(instructionsAsInt);
        }

        //See : https://zwomp.com/index.php/2019/07/22/exploring-the-dune-files-part-1-the-hsq-file-format/
        public static Instruction ReadAndParseInstruction(InstructionsReader reader)
        {

            // A '1' means instruction "Copy byte"
            if (reader.ReadBit()) //Read and increase
            {
                Console.WriteLine("Bit: True");
                Console.WriteLine("Step: CopyByte");

                return new Instruction(InstructionType.CopyByte, null, null);
            }
            else
            {
                Console.WriteLine("Bit: False");

                // '01' means "Method 1"
                if (reader.ReadBit()) //Read and increase
                {
                    Console.WriteLine("Bit: True");
                    Console.WriteLine("Step: Method1");

                    return new Instruction(InstructionType.Method1, null, null);
                }
                // '00' means "Method 0"
                else
                {
                    Console.WriteLine("Bit: False"); //DEBUG

                    var param1 = reader.ReadBit(); //Read and increase

                    Console.WriteLine($"Bit: {param1}"); //DEBUG

                    var param2 = reader.ReadBit(); //Read and increase

                    Console.WriteLine($"Bit: {param2}"); //DEBUG
                    Console.WriteLine("Step: Method0"); //DEBUG


                    //we read two more bits as parameters
                    return new Instruction(InstructionType.Method0, param1, param2);
                }
            }
        }
    }
}
