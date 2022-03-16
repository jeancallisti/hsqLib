﻿
using System.Collections.Generic;
using System.Linq;

namespace CryoDataLib.ImageLib
{
    public class Palette : Dictionary<int, PaletteColor>
    {
        public Palette() : base() { }

        public Palette(IEnumerable<KeyValuePair<int, PaletteColor>> keyValues) : base(keyValues) { }

        /// <summary>
        /// Takes a subpalette (on N colors) and builds a 256-color palette from it. 
        /// The subpalette is placed at the expected position within the palette.
        /// Every missing color is replaced with default color.
        /// </summary>
        public static Palette BuildFromSubpalette(SubPalette subPalette, PaletteColor defaultColor)
        {
            var palette = new Palette();

            for (int i = 0; i < 256; i++)
            {
                var color = subPalette.Colors.FirstOrDefault(c => c.Index + subPalette.LocationInPalette == i);
                if (color == null)
                {
                    color = defaultColor;
                }
                palette.Add(i, color);
            }

            return palette;
        }

        public static Sprite ToSprite(SubPalette subPalette)
        {
            var palette = BuildFromSubpalette(subPalette, PaletteColor.GREEN);

            int colorsPerRow = 8;
            int rowsCount = 32;

            var data = new byte[colorsPerRow * rowsCount];

            for (int j=0; j< rowsCount; j++)
            {
                for (int i=0; i< colorsPerRow; i++)
                {
                    var colorIndex = j * colorsPerRow + i;

                    //DEBUG
                    if (colorIndex < 0 || colorIndex >= 256)
                    {
                        throw new CryoDataException("Invalid color index.");
                    }
                    //-DEBUG

                    data[j*colorsPerRow+i] = (byte)colorIndex;
                }
            }

            var spr = new Sprite()
            {
                Width = colorsPerRow,
                Height = rowsCount,
                Pixels = data,
                Palette = palette
            };

            return spr;
        }

        public static Palette MakeEmptyPalette(PaletteColor emptyColor)
        {
            return new Palette(Enumerable.Range(0, 256).Select(i => new KeyValuePair<int, PaletteColor>(key: i, value: emptyColor)));
        }

        /// <summary>
        /// This is for a hypothetical scenario wjere we have somehow managed to unpack some sprite data
        /// but we don't know what palette to apply. We create a grayscale palette that has roughly the same range 
        /// and palette offset as this sprite.
        /// </summary>
        public static Palette CreateMockPaletteFor(SpriteWithPaletteOffset sprite)
        {
            var palette = MakeEmptyPalette(PaletteColor.GREEN);

            //If this sprite is meant to be used with a subpalette that has, let's say,
            //20 colors, then it will have colors ranging from 0 to 20.
            var spriteColorMin = sprite.Pixels.OrderBy(b => b).First();
            var spriteColorMax = sprite.Pixels.OrderBy(b => b).Last();

            //We need to convert this local range to "real" colors in the absolute colors space.
            byte paletteColorMin = (byte)(spriteColorMin + sprite.PaletteOffset);
            byte paletteColorMax = (byte)(spriteColorMax + sprite.PaletteOffset);

            var colorCount = spriteColorMax - spriteColorMin;

            //Now we fill that range with a gradient.
            for (int i=0; i<colorCount;i++)
            {
                byte grayscaleValue = (byte)(i * (colorCount / 255.0));
                palette[i + paletteColorMin] = new PaletteColor() { Index = i+ paletteColorMin, R = grayscaleValue, G = grayscaleValue, B = grayscaleValue };
            }

            return palette;
        }
    }

    public class NamedPalette
    {
        public string Name { get; init; }
        public Palette Palette { get; init; }
    }

    public class SubPalette
    {
        public string Name { get; init; }
        public int LocationInPalette { get; init; } = 0;
        public IEnumerable<PaletteColor> Colors { get; init; }
    }

    public class PaletteColor
    {
        public static PaletteColor GREEN { get; } = new PaletteColor() { R = 0, G = 255, B = 0 };

        public int Index { get; init; }
        public byte R { get; init; }
        public byte G { get; init; }
        public byte B { get; init; }
    }
}
