using System;


namespace HsqLib.HsqCompressedFile.Instructions
{


    public class InstructionsReader : IInstructionsReader
    {

        IHsqCompressedFile _source_file;
        int _instruction_step = 16;
        int _raw_instructions = 0;

        public InstructionsReader(IHsqCompressedFile source_file)
        {
            _source_file = source_file;
        }

        public Step GetNextStep()
        {
            if (ReadNextBit()) // If just Copy next byte
                return new CopyByte();

            if (ReadNextBit()) // If doing Method One
            {
                return new DoMethodOne(_source_file);
            }

            // Doing Method Zero
            return new DoMethodZero(this, _source_file);
        }

        public bool ReadNextBit()
        {
            if (_instruction_step == 16)
            {
                _raw_instructions = BitConverter.ToInt16(_source_file.GetNextWord(), 0);
                _instruction_step = 0;
            }

            var result = (_raw_instructions & (int)(Math.Pow(2, _instruction_step))) > 0;
            _instruction_step++;

            //+DEBUG
            Console.WriteLine($"Bit: {result}");
            //-DEBUG
            return result;
        }
    }
}
