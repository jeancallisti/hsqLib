using CryoDataLib.ImageLib;
using CryoImageRenderCli.BitmapExport;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CryoImageRenderCli
{
    public class PngExport
    {
        public static void SaveSpriteAsPng(Sprite sprite, string fileName, int scaleUpFactor)
        {
            var asBitmap = BitmapBuilder.ToBitmap(sprite);

            var scaledUpBitmap = BitmapBuilder.ScaleUpNearestNeighbour(asBitmap, scaleUpFactor);

            Console.WriteLine($"Saving file {fileName}...");
            scaledUpBitmap.Save(fileName, ImageFormat.Png);
        }

        public static void SavePaletteFileAsPng(SubPalette palette, string fileName)
        {
            int scaleUpFactor = 20;
            var asSprite = Palette.ToSprite(palette);
            SaveSpriteAsPng(asSprite, fileName, scaleUpFactor);
        }

        public static void SavePartSpriteAsPng(Sprite sprite, string fileName)
        {
            int scaleUpFactor = 20;
            SaveSpriteAsPng(sprite, fileName, scaleUpFactor);
        }
    }
}
