
using System.Collections.Generic;
using System.Linq;

namespace CryoDataLib.ImageLib
{
    public class Palette
    {
        public static PaletteColor GREEN { get; } = new PaletteColor() { R = 0, G = 255, B = 0 };

        public IEnumerable<SubPalette> SubPalettes { get; init; }

        /// <summary>
        /// Takes a subpalette (on N colors) and builds a 256-color palette from it. 
        /// The subpalette is placed at the expected position within the palette.
        /// Every missing color is replaced with default color.
        /// </summary>
        public static Dictionary<int, PaletteColor> BuildFromSubpalette(SubPalette subPalette, PaletteColor defaultColor)
        {
            var palette = new Dictionary<int, PaletteColor>();

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

        public Sprite ToSprite(int whichSubPalette)
        {
            if (whichSubPalette < 0 || whichSubPalette >= SubPalettes.Count())
            {
                throw new CryoDataException($"Invalid subpalette: {whichSubPalette}. Valid : [{string.Join(",", Enumerable.Range(0, SubPalettes.Count()))}]");
            }

            var subPalette = SubPalettes.ToArray()[whichSubPalette];

            var palette = BuildFromSubpalette(subPalette, Palette.GREEN);

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
                Data = data,
                Palette = palette
            };

            return spr;
        }
    }

    public class SubPalette
    {
        public int LocationInPalette { get; init; } = 0;
        public IEnumerable<PaletteColor> Colors { get; init; }
    }

    public class PaletteColor
    {
        public int Index { get; init; }
        public byte R { get; init; }
        public byte G { get; init; }
        public byte B { get; init; }
    }
}
