using HsqLib2.BinaryReader;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HsqLib2.HsqReader.Instructions
{

    public class InstructionsReader
    {
        Stack<bool> bits = new Stack<bool>();

        private readonly IBinaryReader reader;

        public InstructionsReader(IBinaryReader reader)
        {
            this.reader = reader;
        }

        public bool ReadBit()
        {
            //We've reached the end of our bits buffer. Get more!
            if (!bits.Any())
            {
                bits = new Stack<bool>(InstructionsBlock.Read16Bits(reader).Reverse());
            }

            return bits.Pop();
        }
    }
}
