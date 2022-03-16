using System;

namespace CryoDataLib
{
    public class CryoDataException : Exception
    {
        public CryoDataException(string message) : base(message) { }

        public CryoDataException(string message, Exception? innerException) : base(message, innerException) { }

    }
}
