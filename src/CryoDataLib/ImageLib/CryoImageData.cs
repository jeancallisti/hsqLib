
using CryoDataLib.ImageLib.Part;
using System.Collections.Generic;

namespace CryoDataLib.ImageLib
{
    public class CryoImageData : CryoData
    {
        public IEnumerable<SubPalette> SubPalettes { get; init; }
        public IEnumerable<JSonImagePart> ImageParts { get; init; }
        public IEnumerable<JSonUnknownPart> UnknownParts { get; init; }
    }
}
