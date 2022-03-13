
using System.Collections.Generic;

namespace CryoDataLib.TextLib
{
    public class CryoTextData : CryoData
    {
        public CryoTextMatadata Metadata { get; init; }
        public Dictionary<int, string> Sentences { get; init; }
    }
}
