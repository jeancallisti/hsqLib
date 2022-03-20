
using CryoDataLib.ImageLib;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace CryoImageRenderCli.BitmapExport
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

        public static Bitmap ToBitmap(int width, int height, byte[] data, Dictionary<int, PaletteColor> palette)
        {
            // alpha == 0 means transparent, alpha == 255 means opaque
            int alpha = 255;

            if (data.Length != width * height)
            {
                throw new System.Exception("Cannot convert to bitmap. Dimensions and data don't match.");
            }

            if (palette == null)
            {
                throw new CryoRenderException($"I can only render to bitmap if there's a palette.");
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
                    byte colorIndex = data[j * width + i];

                    //0 means transparent
                    if (colorIndex != 0)
                    {
                        bmp.SetPixel(i, j, bitmapPalette[colorIndex]);
                    }
                }
            }
            return bmp;
        }

        public static Bitmap ToBitmap(Sprite sprite)
        {
            if (sprite.Palette == null)
            {
                throw new CryoRenderException($"I can only render to bitmap a sprite that was given a palette.");
            }

            return ToBitmap(sprite.Width, sprite.Height, sprite.Pixels, sprite.Palette);
        }

        public static Bitmap ScaleUpNearestNeighbour(Bitmap source, int scaleFactor)
        {
            var dest = new Bitmap(source.Width * scaleFactor, source.Height * scaleFactor);

            //using (Graphics gSource = Graphics.FromImage(source))
            using (Graphics gDest = Graphics.FromImage(dest))
            {
                //gSource.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                gDest.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;

                //So that the first row of pixels is not only half-visible
                //https://stackoverflow.com/questions/20776605/missing-half-of-first-pixel-column-after-a-graphics-transform-scale
                //gSource.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
                gDest.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;

                gDest.DrawImage(source, 0, 0, source.Width * scaleFactor, source.Height * scaleFactor);
                //gSource.Set
                //gBmp.DrawEverything(); //this is your code for drawing
            }

            return dest;
        }
    }
}
