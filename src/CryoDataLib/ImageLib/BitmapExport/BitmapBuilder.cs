
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace CryoDataLib.ImageLib.BitmapExport
{
    public class BitmapBuilder
    {

        private static Color ConvertColor(int alpha, PaletteColor c)
        {
            // alpha == 0 means transparent, alpha == 255 means opaque
            return Color.FromArgb(alpha, c.R, c.G, c.B);
        }

        private static Dictionary<int, Color> ToMicrosoftPalette(Dictionary<int, PaletteColor> palette, int alpha)
        {
            return new Dictionary<int, Color>(
                            Enumerable.Range(0, 256)
                            .Select(i => new KeyValuePair<int, Color>(
                                            key: i,
                                            value: ConvertColor(alpha, palette[i]))
                            ));
        }

        public static Bitmap ToBitmap(int width, int height, byte?[] data, Dictionary<int, PaletteColor> palette)
        {
            // alpha == 0 means transparent, alpha == 255 means opaque
            int alpha = 255;

            if (data.Length != width * height)
            {
                throw new System.Exception("Cannot convert to bitmap. Dimensions and data don't match.");
            }

            if (palette == null)
            {
                throw new CryoDataException($"I can only render to bitmap if there's a palette.");
            }

            if (palette.Keys.ToArray().Length != 256)
            {
                throw new System.Exception("Palette must have 256 colors.");
            }

            var bitmapPalette = ToMicrosoftPalette(palette, alpha);

            Bitmap bmp = new Bitmap(width, height);
            for (int j = 0; j < height; j++)
            {
                for (int i = 0; i < width; i++)
                {
                    byte? colorIndex = data[j * width + i];

                    //Null means transparent
                    if (colorIndex.HasValue)
                    {
                        bmp.SetPixel(i, j, bitmapPalette[colorIndex.Value]);
                    }
                }
            }
            return bmp;
        }

        public static Bitmap ToBitmap(Sprite sprite)
        {
            if (sprite.Palette == null)
            {
                throw new CryoDataException($"I can only render to bitmap a sprite that was given a palette.");
            }

            return ToBitmap(sprite.Width, sprite.Height, sprite.Pixels, sprite.Palette);
        }

        public static Bitmap ScaleUpNearestNeighbour(Bitmap b, int scaleFactor)
        {
            Graphics g = Graphics.FromImage(b);
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;

            var dest = new Bitmap(b.Width * scaleFactor, b.Height * scaleFactor);
            Graphics g2 = Graphics.FromImage(dest);
            g2.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;

            g2.DrawImage(b, 0, 0, b.Width * scaleFactor, b.Height * scaleFactor);
            //g.Set
            //gBmp.DrawEverything(); //this is your code for drawing
            g.Dispose();
            g2.Dispose();

            return dest;
        }
    }
}
