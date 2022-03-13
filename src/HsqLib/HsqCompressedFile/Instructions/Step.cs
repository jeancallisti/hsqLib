using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HsqLib.HsqCompressedFile.Instructions
{


    public abstract class Step
    {

    }

    public class CopyByte : Step
    {
    }

    public class DoMethodZero : Step
    {
        public DoMethodZero(IInstructionsReader reader,
            IHsqCompressedFile source_file)
        {
            Length = 2;
            Length += reader.ReadNextBit() ? 2 : 0;
            Length += reader.ReadNextBit() ? 1 : 0;

            Distance = BitConverter.ToInt16(new Byte[]
                { source_file.GetNextByte(), 0xFF }, 0);
        }

        public int Length;
        public int Distance;
    }

    public class DoMethodOne : Step
    {
        public DoMethodOne(IHsqCompressedFile source_file)
        {
            Distance = 0;
            Length = 0;

            var raw = BitConverter.ToInt16(source_file.GetNextWord(), 0);

            Length += raw & 7;

            if (Length == 0)
                Length = source_file.GetNextByte();

            if (Length == 0)
            {
                EOF = true;
                return;
            }

            Length += 2;

            Distance = raw >> 3;
            if (Distance >= 0) Distance -= 8192; // Bug fix from blog comments
        }

        public int Length;
        public int Distance;
        public bool EOF;
    }
}
