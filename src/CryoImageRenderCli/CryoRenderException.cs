using System;

namespace CryoImageRenderCli
{
    public class CryoRenderException : Exception
    {
        public CryoRenderException(string message) : base(message) { }

        public CryoRenderException(string message, Exception? innerException) : base(message, innerException) { }

    }
}
