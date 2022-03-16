using System.Collections.Generic;
using System.Linq;

namespace CryoDataLib.ImageLib
{
    public class Sprite
    {
        public string Name { get; init; }
        public int Width { get; init; }
        public int Height { get; init; }

        //Null means transparent
        public byte?[] Pixels { get; set; } //public set is not very clean. Ideally we'd like init+private set

        public Dictionary<int, PaletteColor> Palette { get; set; } //public set is not very clean. Ideally we'd like init+private set
    }

    //Same as sprite, except the color values in 'Pixels' should not be used as-is.
    //Once you've decided with palette you want to apply, the color of each pixel is paletteColors[originalSpriteColor+paletteOffset] 
    public class SpriteWithPaletteOffset
    {
        public string Name { get; init; }
        public int Width { get; init; }
        public int Height { get; init; }
        public byte?[] Pixels { get; init; } //Null means transparant

        public int PaletteOffset { get; set; /* TODO revert to 'init' */ }

        private byte? ApplyOffset(byte? pixel)
        {
            if (pixel == null)
            {
                return null;
            }

            var pixelWithOffset = pixel + PaletteOffset; 

            if (pixelWithOffset >= 256)
            {
                //TODO: Restore.
                //throw new CryoDataCannotApplyPaletteException("There seems to be a mistake. Applying this palette onto a sprite with this palette offset makes the pixel values too high.");
                return 0;
            }

            return (byte)pixelWithOffset;

        }
        public Sprite CombineWithPalette(Dictionary<int, PaletteColor> palette)
        {
            return new Sprite()
            {
                Name = Name,
                Width = Width,
                Height = Height,
                Palette = palette,
                Pixels = Pixels.Select(p => ApplyOffset(p)).ToArray()
            };

        }
    }
}
