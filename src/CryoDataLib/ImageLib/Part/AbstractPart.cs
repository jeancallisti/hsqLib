namespace CryoDataLib.ImageLib
{

    public abstract class AbstractPart
    {
        public string Name { get; init; }
        public int Index { get; init; }

        public byte[] RawData { get; init; }
        public string RawDataHexString { get; init; }
    }

    //Only for export
    public abstract class JSonAbstractPart
    {
        public long AbsoluteStartAddress { get; init; }
        public long AbsoluteEndAddress { get; init; }
    }

}
