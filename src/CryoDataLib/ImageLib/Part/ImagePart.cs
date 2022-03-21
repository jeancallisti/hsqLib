
namespace CryoDataLib.ImageLib.Part
{
    public class ImagePart : AbstractPart
    {
        public bool IsCompressed { get; init; }
        public int Width { get; init; }
        public int Height { get; init; }
        public int PaletteOffset { get; init; }
        public byte[] UncompressedPixels { get; init; } //An actual pixels array of size width*length. Null means transparent
        public string UncompressedPixelsHexString { get; init; }

        public SpriteWithPaletteOffset ToSpriteWithPaletteOffset()
        {
            return new SpriteWithPaletteOffset()
            {
                Name = Name,
                Pixels = UncompressedPixels,
                Width = Width,
                Height = Height,
                PaletteOffset = PaletteOffset
            };
        }
    }

    //Only for export
    public class JSonImagePart : JSonAbstractPart
    {
        public ImagePart ImagePart { get; init; }
    }

}
