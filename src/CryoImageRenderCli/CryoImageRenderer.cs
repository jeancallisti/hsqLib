
using CryoDataLib.ImageLib;
using CryoDataLib.ImageLib.Part;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CryoImageRenderCli
{
    public enum PaletteModes
    {
        eGenerate,
        eguessInternalOnly,
        eGuessAll
    }

    public class CryoImageRenderer
    {
        public CryoImageData CryoImage { get; }
        public string DestFolder { get; }

        public CryoImageRenderer(CryoImageData cryoImage, string destFolder)
        {
            CryoImage = cryoImage;
            DestFolder = destFolder;
        }

        public void SaveSubpalettesToDisk()
        {
            var subPalettes = CryoImage.SubPalettes.ToArray();
            for (int i = 0; i < subPalettes.Length; i++)
            {
                var fileName = Path.Combine(DestFolder, $"{CryoImage.SourceFile}.palette{i}.png");
                PngExport.SavePaletteFileAsPng(subPalettes[i], fileName);
            }
        }

        private Sprite GetSpriteWithGeneratedSubpalette(SpriteWithPaletteOffset sprWithPaletteOffset)
        {
            var palette = Palette.CreateMockPaletteFor("subpaletteMock", sprWithPaletteOffset);
            return sprWithPaletteOffset.CombineWithPalette(palette);
        }

        private void SaveImagePartToDisk(PaletteModes paletteMode, ImagePart p, IEnumerable<SubPalette> availableSubpalettes)
        {
            Console.WriteLine($"Part {p.Name}...");

            if (p.UncompressedPixels == null || !p.UncompressedPixels.Any())
            {
                Console.WriteLine($"Skipping (empty).");
                return;
            }


            try
            {
                //No palette for now. 'Parts' sprites rely on palette offset
                var asSpriteWithPaletteOffset = p.ToSpriteWithPaletteOffset();

                if (!asSpriteWithPaletteOffset.TryApplyPaletteOffset(out var offsetedSprite, out var newMin, out var newMax))
                {
                    Console.Error.WriteLine($"Some colors of sprite '{asSpriteWithPaletteOffset.Name}' would have forbidden values if we applied the palette offset (highest pixel value : {newMax}, offset : {asSpriteWithPaletteOffset.PaletteOffset}).");
                }

                asSpriteWithPaletteOffset = offsetedSprite;

                var spritesToRender = new List<Sprite>();
                switch (paletteMode)
                {
                    case PaletteModes.eGenerate:
                        spritesToRender.Add(GetSpriteWithGeneratedSubpalette(asSpriteWithPaletteOffset));
                        break;

                    case PaletteModes.eguessInternalOnly:
                    case PaletteModes.eGuessAll:
                        var availableFullPalettes = availableSubpalettes.Select(subp => Palette.BuildFromSubpalette(subp, PaletteColor.GREEN));
                        var goodPaletteCandidates = GuessPaletteCandidates(asSpriteWithPaletteOffset, availableFullPalettes);

                        goodPaletteCandidates.ToList().ForEach(pal =>
                        {
                            spritesToRender.Add(asSpriteWithPaletteOffset.CombineWithPalette(pal));
                        });
                        break;

                    default:
                        throw new NotImplementedException($"Unknown palette mode : {paletteMode}.");
                }

                spritesToRender.ForEach(sprite => {
                    var fileName = Path.Combine(DestFolder, $"{this.CryoImage.SourceFile}.{sprite.Name}.png");
                    PngExport.SavePartSpriteAsPng(sprite, fileName);
                });

            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Could not export part {p.Name} as PNG.");
            }
        }

        private IEnumerable<Palette> GuessPaletteCandidates(SpriteWithPaletteOffset sprWithPaletteOffset, IEnumerable<Palette> availableFullPalettes)
        {
            if (!Palette.TryFindColorRange(sprWithPaletteOffset.Pixels, out var min, out var max))
            {
                //No color range. Transparant sprite? either way, fallback : 
                return new List<Palette>() { Palette.MakeEmptyPalette(name: "paletteNoRange", PaletteColor.PINK) };
            }

            var successPalettes = new List<Palette>();
            availableFullPalettes.ToList().ForEach(p =>
            {
                Console.Write($"   Trying subpalette '{p.Name}' on sprite {sprWithPaletteOffset.Name}...");

                //Does any of the sprite's colors seem to be outside of the palette's defined colors?
                if (p[min] == PaletteColor.GREEN || p[max] == PaletteColor.GREEN)
                {
                    Console.WriteLine($"  No.");
                    return;
                }

                Console.WriteLine($"  YES.");


                successPalettes.Add(p);

            });

            return successPalettes;
        }

        public void SaveImagePartsToDisk(PaletteModes paletteMode, IEnumerable<SubPalette> availableSubpalettes)
        {
            this.CryoImage.ImageParts.ToList().ForEach(p =>
            {
                SaveImagePartToDisk(paletteMode, p.ImagePart, availableSubpalettes);
            });
        }
    }
}
