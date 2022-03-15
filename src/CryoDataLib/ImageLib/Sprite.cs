using System.Collections.Generic;

namespace CryoDataLib.ImageLib
{
    public class Sprite
    {
        public int Width { get; init; }
        public int Height { get; init; }
        public byte[] Data { get; init; }

        public Dictionary<int, PaletteColor>? Palette { get; init; }
    }
}
