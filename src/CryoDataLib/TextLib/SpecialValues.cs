using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CryoDataLib.TextLib
{
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

        // { 85, 86, 87 } --> "0x55,0x56,0x57"
        public static string BytesSequenceToHexString(byte[] input)
        {
            return string.Join(",", input.Select(b => $"0x{Convert.ToString(b, 16)}")).ToLowerInvariant();
        }

        // "0x55,0x56,0x57" --> { 85, 86, 87 }
        public static byte[] HexStringToBytesSequence(string input)
        {
            return input.Split(",")
                    .Select(hexValueAsString => {
                        var cleanString = hexValueAsString.Substring(2); // "0x22" --> "22"
                        return byte.Parse(cleanString, System.Globalization.NumberStyles.HexNumber);
                    })
                    .ToArray();
        }

        // "UVW" --> { 85, 86, 87 }
        public static byte[] StringToBytesSequence(string input)
        {
            return input
                    .Select(c => {

                        if (c > 127)
                        {
                            throw new ArgumentException($"String '{input}' contains char '{c}' which is forbidden. Use basic ascii chars.");
                        }
                        return (byte)c;
                    })
                    .ToArray();
        }

        // "UVW" --> "0x55,0x56,0x57"
        public static string StringToHexString(string input)
        {
            return BytesSequenceToHexString(StringToBytesSequence(input));
        }
    }
}
