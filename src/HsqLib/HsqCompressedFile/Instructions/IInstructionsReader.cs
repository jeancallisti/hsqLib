using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HsqLib.HsqCompressedFile.Instructions
{
    public interface IInstructionsReader
    {
        Step GetNextStep();
        bool ReadNextBit();
    }
}
