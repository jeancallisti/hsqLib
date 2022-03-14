using HsqLib2;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CryoDataLib.ImageLib
{
    public class CryoImageDataInterpreter : ICryoDataInterpreter
    {

        public CryoImageDataInterpreter()
        {
        }

        public async Task<CryoData> InterpretData(HsqFile file)
        {
            using (var stream = new MemoryStream(file.UnCompressedData))
            using (var reader = new BinaryReader(stream))
            {
                
                var output = new CryoImageData()
                {
                    SourceFile = file.SourceFile
                };

                return output;
            }
        }
    }
}
