
namespace HsqLib2.HsqReader.Instructions
{
    public enum InstructionType
    {
        CopyByte = 0,
        Method0 = 1,
        Method1 = 2
    }

    public class Instruction
    {
        public InstructionType Type { get; private set; }

        public bool BitParam1 { get; private set; }
        public bool BitParam2 { get; private set; }

        public Instruction(InstructionType type, bool? bitParam1, bool? bitParam2)
        {
            Type = type;
            BitParam1 = bitParam1 ?? false;
            BitParam2 = bitParam2 ?? false;
        }
    }
}
