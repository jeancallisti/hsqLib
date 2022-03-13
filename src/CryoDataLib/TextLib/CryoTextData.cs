
using System.Collections.Generic;

namespace CryoDataLib.TextLib
{
    public class CryoTextData : CryoData
    {
        public Dictionary<int, CryoSentenceData> Sentences { get; } = new Dictionary<int, CryoSentenceData>();
    }
}
