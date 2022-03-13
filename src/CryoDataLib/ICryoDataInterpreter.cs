using HsqLib2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CryoDataLib
{
    public interface ICryoDataInterpreter
    {
        public Task<CryoData> InterpretData(HsqFile file);
    }
}
