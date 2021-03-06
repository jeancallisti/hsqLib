
A code base in **C#** for unpacking the "HSQ" file format used by Cryo in games like Dune, KGB, Lost Eden ,etc.
Unlike other code bases for Cryo games data, this one chooses **brute force** over old-school optimized code : 
It's meant to be extremly maintainable, understandable and tweakable. No C pointer arithmetics and very little binary data here, only Readers as well as Linq arrays filtering, producing easily-loadable json files and PNG files.

# hsqLib2 and UnpackCli2

- hsqLib2.dll : A library to unpack HSQ files (regardless of what they contain)
- UnpackCli2.exe : A console app using hsqLib2 to be able to unpack files using the command line. The files can be exported as **binary** or as **json** for understanding and easy processing.


# CryoDataLib and CryoDataCli

- CryoDataLib : A library to interpret the content of an unpacked HSQ file (obtained with UnpackCli2). That includes : 
   - Text data files (files "PHRASEX.HSQ"),
   - Images data files (e.g. FRM3.HSQ, ICONES.HSQ, etc.)
   We ignore scene files or animations (not the goal of this project)
- CryoDataCli : A console app using CryoDataLib to be able to export files using the command line. The files are exported as **json**

# CryoImageRenderCli

A console app allowing you to export the json output of CryoDataCli as **PNG files** .
It has one interesting feature : You can choose between 3 palette resolution modes : 
- **generate** : The files' palettes are automatically generated to roughly match the content of the sprites, in gray scale. It helps you see if the files are correct or what they contain.
- **guessInternalOnly** : The exporter looks in the file's subpalettes (internal palettes) and tries to guess which one applies to the sprite.
- **guessAll** [NOT IMPLEMENTED] : the exporter looks in ALL the subpalettes of ALL the files in the same folder as the exported file. that's for sprites which palette are not present in the same file but in some other "master" file. For example ICONES.HSQ has many sprites but the palette comes from somehwre else.



