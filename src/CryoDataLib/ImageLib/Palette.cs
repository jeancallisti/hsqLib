
using System;
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

            var data = new byte?[colorsPerRow * rowsCount];

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

        //Find the color with the lowest palette index value and the one with the highest value within a set of pixels.
        public static void FindColorRange(byte?[] pixels, out byte min, out byte max)
        {
            //Null means transparent
            var actualColorsOrdered = pixels.Where(p => p != null).OrderBy(b => b).ToArray();

            if (!actualColorsOrdered.Any())
            {
                throw new CryoDataException($"Could not compute color range. No pixels!");
            }

            //If this sprite is meant to be used with a subpalette that has, let's say,
            //20 colors, then it will have colors ranging from 0 to 20.
            min = (byte)actualColorsOrdered.First();
            max = (byte)actualColorsOrdered.Last();
        }

        //A gradient that goes from dark to bright in 'colorCount' steps.
        private static IEnumerable<PaletteColor> GenerateGradient(int colorCount, float lowRed, float highRed, float lowGreen, float highGreen, float lowBlue, float highBlue )
        {
            var colors = new List<PaletteColor>();

            float redStep = (highRed - lowRed) / colorCount;
            float greenStep = (highGreen - lowGreen) / colorCount;
            float blueStep = (highBlue - lowBlue) / colorCount;

            for (int i = 0; i < colorCount; i++)
            {
                //Classic lerping between a lower bound and an upper bound
                float fR = redStep * i + lowRed;
                float fG = greenStep * i + lowGreen;
                float fB = blueStep * i + lowBlue;

                byte r = (byte)fR;
                byte g = (byte)fG;
                byte b = (byte)fB;

                colors.Add(new PaletteColor() { Index = i, R = r, G = g, B = b });
            }

            if (colors.Count() >= 256)
            {
                throw new CryoDataException($"Gradient too big!");
            }

            return colors;
        }

        /// <summary>
        /// This is for a hypothetical scenario where we have somehow managed to unpack some sprite data
        /// but we don't know what palette to apply. We create a grayscale palette that has roughly the same range 
        /// and palette offset as this sprite.
        /// </summary>
        public static Palette CreateMockPaletteFor(SpriteWithPaletteOffset sprite)
        {
            var palette = MakeEmptyPalette(PaletteColor.GREEN);

            FindColorRange(sprite.Pixels, out var min, out var max);

            var spriteColorCount = max+1 - min;

            //Generate red-ish gradient that goes from dark red to bright red.
            var gradient = GenerateGradient(spriteColorCount, 50,255,0,200,0,200).ToArray();

            //Apply gradient and update offsets
            for (int i = 0; i < spriteColorCount; i++)
            {
                palette[i+min] = gradient[i];
                //By the way, the Indices in the colors generated by the gradient are now wrong in this palette. Update them.
                palette[i + min].Index = i + min;
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

        public int Index { get; set; }
        public byte R { get; init; }
        public byte G { get; init; }
        public byte B { get; init; }

    }
}
