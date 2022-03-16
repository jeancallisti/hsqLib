using System.Collections.Generic;
using System.Linq;

namespace CryoDataLib.ImageLib
{
    public class Sprite
    {
        public int Width { get; init; }
        public int Height { get; init; }
        public byte[] Pixels { get; set; } //public set is not very clean. Ideally we'd like init+private set

        public Dictionary<int, PaletteColor> Palette { get; set; } //public set is not very clean. Ideally we'd like init+private set
    }

    //Same as sprite, except the color values in 'Pixels' should not be used as-is.
    //Once you've decided with palette you want to apply, the color of each pixel is paletteColors[originalSpriteColor+paletteOffset] 
    public class SpriteWithPaletteOffset
    {
        public int Width { get; init; }
        public int Height { get; init; }
        public byte[] Pixels { get; init; } //public set is not very clean. Ideally we'd like init+private set

        public int PaletteOffset { get; init; }

        public Sprite CombineWithPalette(Dictionary<int, PaletteColor> palette)
        {
            if (Pixels.Any(p => p + PaletteOffset > 256))
            {
                throw new CryoDataCannotApplyPaletteException("There seems to be a mistake. Applying this palette with this palette offset makes the pixel values too high. Did you already apply a palette offset?");
            }

            return new Sprite()
            {
                Width = Width,
                Height = Height,
                Palette = palette,
                Pixels = Pixels.Select(p => (byte)(p + PaletteOffset)).ToArray()
            };

        }
    }
}
