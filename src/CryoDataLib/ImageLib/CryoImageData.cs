
using System.Collections.Generic;

namespace CryoDataLib.ImageLib
{
    public class CryoImageData : CryoData
    {
        public Palette Palette { get; init; }
        public IEnumerable<long> Addresses { get; init; }

    }
}
