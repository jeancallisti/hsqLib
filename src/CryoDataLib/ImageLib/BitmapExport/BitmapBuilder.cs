
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace CryoDataLib.ImageLib.BitmapExport
{
    public class BitmapBuilder
    {

        private static Color ConvertColor(int alpha, PaletteColor c)
        {
            return Color.FromArgb(alpha, c.R, c.G, c.B);
        }

        private static Dictionary<int, Color> ConvertPalette(Dictionary<int, PaletteColor> palette, int alpha)
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
            int alpha = 255;

            if (data.Length != width * height)
            {
                throw new System.Exception("Cannot convert to bitmap. Dimensions and data don't match.");
            }

            if (palette.Keys.ToArray().Length != 256)
            {
                throw new System.Exception("Palette must have 256 colors.");
            }

            var bitmapPalette = ConvertPalette(palette, alpha);

            Bitmap bmp = new Bitmap(width, height);
            for (int j = 0; j < height; j++)
            {
                for (int i = 0; i < width; i++)
                {
                    var colorIndex = data[j * width + i];
                    bmp.SetPixel(i, j, bitmapPalette[colorIndex]);
                }
            }

            //Graphics g = Graphics.FromImage(bmp);
            //g.Set
            //gBmp.DrawEverything(); //this is your code for drawing
            //g.Dispose();
            //bmp.Save("image.png", ImageFormat.Png);

            return bmp;
        }

        public static Bitmap ToBitmap(Sprite sprite)
        {
            return ToBitmap(sprite.Width, sprite.Height, sprite.Data, sprite.Palette);
        }

        public static Bitmap ScaleUpNearestNeighbout(Bitmap b, int scaleFactor)
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
