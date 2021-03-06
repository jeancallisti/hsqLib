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
        public byte[] Pixels { get; set; } //public set is not very clean. Ideally we'd like init+private set

        public Dictionary<int, PaletteColor> Palette { get; set; } //public set is not very clean. Ideally we'd like init+private set

    }

    //Same as sprite, except the color values in 'Pixels' should not be used as-is.
    //Once you've decided with palette you want to apply, the color of each pixel is paletteColors[originalSpriteColor+paletteOffset] 
    public class SpriteWithPaletteOffset
    {
        public string Name { get; init; }
        public int Width { get; init; }
        public int Height { get; init; }
        public byte[] Pixels { get; init; } //Null means transparant

        public int PaletteOffset { get; set; /* TODO revert to 'init' */ }

        public SpriteWithPaletteOffset Clone()
        {
            return new SpriteWithPaletteOffset
            {
                Name = Name,
                Width = Width,
                Height = Height,
                Pixels = Pixels.Select(b => b).ToArray(), //Terrible cloning of array
                PaletteOffset = PaletteOffset,
            };
        }

        //Try to apply the palette offset to all colors, or revert to an offset of zero if for some reason that causes 
        //some colors to become out of bounds ( >= 256 )
        public bool TryApplyPaletteOffset(out SpriteWithPaletteOffset correctedSprite, out byte min, out byte max)
        {
            if (!Palette.TryFindColorRange(Pixels, out min, out max))
            {
                //Return unmodified copy
                correctedSprite = Clone();
                return false;
            }

            if (max + PaletteOffset > 255)
            {
                //Return unmodified copy
                correctedSprite = Clone();
                return false;
            }

            min = (byte)(min + PaletteOffset);
            max = (byte)(max + PaletteOffset);

            //We leave zero as it is because it has a special meaning (transparent)
            byte[] colorCorrectedPixels = Pixels.Select(p => (byte)(p != 0 ? (byte)(p + PaletteOffset) : 0)).ToArray();

            correctedSprite = new SpriteWithPaletteOffset
            {
                Name = Name,
                Width = Width,
                Height = Height,
                Pixels = colorCorrectedPixels,
                PaletteOffset = 0, //important
            };
            return true;
        }

        public Sprite CombineWithPalette(Palette palette)
        {
            TryApplyPaletteOffset(out var correctedSprite, out var min, out var max);

            return new Sprite()
            {
                Name = $"{Name}.{palette.Name}",
                Width = Width,
                Height = Height,
                Palette = palette,
                Pixels = correctedSprite.Pixels
            };
        }
    }
}
