
using System.Collections.Generic;

namespace CryoDataLib.ImageLib
{
    public class CryoImageData : CryoData
    {
        public IEnumerable<SubPalette> SubPalettes { get; init; }
        public IEnumerable<Part> Parts { get; init; }
    }
}
