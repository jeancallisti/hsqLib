using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CryoDataLib.TextLib
{
    public class TextInstructionParam
    {
        public string Name { get; init; }
        public string Mode { get; init; }

        public byte? Terminator { get; init; } // e.g. 0x06 or null
    }

    public class TextInstruction
    {
        public string FunctionName { get; init; }
        public byte TriggerByte { get; init; } // e.g. 0x08
        public IEnumerable<TextInstructionParam> Params { get; init; }
    };

    public static class TextInstructions
    {
        public static string Json { get; } = @"
            [
                {
                    //the name of the smuggler's location is triggered by byte '0x86' 
                    //followed by nothing (parameterless)
                    FunctionName: ""SmugglerLocation"",
                    TriggerByteHex: ""0x86"",
                    Params: []
                },
                {
                    //A sietch's name is triggered by byte '0x80' 
                    //followed by the sietch name's index as a 16bit value
                    FunctionName: ""SietchName"",
                    TriggerByteHex: ""0x80"",
                    Params: [
                        {
                            Name: ""Name"",
                            Mode: ""READ16"",
                        },
                    ]
                },
                {
                    //A spice amount is triggered by byte '0x92' 
                    //followed by the actual ""Spice variable"" requested.
                    // - Production decrease since yesterday                    =  0xB2
                    // - Production increase since yesterday                    =  0xB0
                    // - Yesterday's spice production                           =  0xAE
                    // - Current spice stock  (in Duncan's report)              =  0xA0
                    // - Current spice stock  (when negociating with emperor)   =  0xB4
                    // - ??? (sometimes means ALL the spice, sometime 3/4)      =  0xB6
                    // - 3/4 spice stock  (when negociating with emperor)       =  0xB8
                    // 0xBA is a problematic value, sometimes it means 1/4 sometimes it means 1/2
                    // - 1/4 spice stock  (when negociating with emperor)       =  0xBA
                    // - 1/2 spice stock  (when negociating with emperor)       =  0xBA
                    // - Emperors's last spice demand                           =  0xBC
                    // - What the smuggler wants to be paid                     =  0x20
                    FunctionName: ""SpiceVariable"",
                    TriggerByteHex: ""0x92"",
                    Params: [
                        {
                            Name: ""Value"",
                            Mode: ""READ8"",
                        },
                    ]
                },
                {
                    //A sequence of small text is triggered by byte 0x06
                    //And terminated by byte 0x08
                    FunctionName: ""SmallText"",
                    TriggerByteHex: ""0x06"",
                    Params: [
                        {
                            Name: ""Text"",
                            Mode: ""READUNTIL"",
                            Terminator: ""0x08""
                        },
                    ]
                },
            ]
        ";
    }

    //For serialization
    public class JsonTextInstructionParam
    {
        public string Name { get; init; }
        public string Mode { get; init; }

        public string Terminator { get; init; } // e.g. "0x06" or empty/null
    }

    //For serialization
    public class JsonTextInstruction
    {
        public string FunctionName { get; init; }
        public string TriggerByteHex { get; init; } // e.g. "0x08"
        public IEnumerable<JsonTextInstructionParam> Params { get; init; }
    };

    /*
    public static class SpecialValues
    {
        public static byte EscapeByte { get; } = 0x80;

        public static string ValuesJson { get; } = @"
            [
                {
                    Key: ""0x80,0x00,0x02"",
                    Value: ""SIETCHNAME1"", 

                },
                {
                    Key: ""0x80,0x00,0x11"", 
                    Value: ""SIETCHNAME2"", 
                },
            ]
        ";
    }
    */
}
