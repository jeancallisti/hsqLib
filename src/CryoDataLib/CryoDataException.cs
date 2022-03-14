using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CryoDataLib
{
    internal class CryoDataException : Exception
    {
        public CryoDataException(string message) : base(message) { }

        public CryoDataException(string message, Exception? innerException) : base(message, innerException) { }

    }
}
