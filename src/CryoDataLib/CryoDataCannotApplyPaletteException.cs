using System;

namespace CryoDataLib
{
    /// <summary>
    /// Specifically for misuse of CombineWithPalette
    /// </summary>
    public class CryoDataCannotApplyPaletteException : Exception
    {
        public CryoDataCannotApplyPaletteException(string message) : base(message) { }

        public CryoDataCannotApplyPaletteException(string message, Exception? innerException) : base(message, innerException) { }

    }
}
