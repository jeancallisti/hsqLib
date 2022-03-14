using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CryoDataLib.TextLib
{
    public class HexHelper
    {
        // 15 --> "0x0f"
        public static string ByteToHexString(byte input)
        {
            try
            {
                return $"0x{Convert.ToString(input, 16)}".ToLowerInvariant();
            } catch (Exception ex)
            {
                throw new CryoDataException("Failed to convert byte", ex);
            }
            
        }

        // "0x0f" --> 15
        public static byte HexStringToByte(string input)
        {
            try
            {
                var cleanString = input.Substring(2); // "0x22" --> "22"
                return byte.Parse(cleanString, System.Globalization.NumberStyles.HexNumber);
            }
            catch (Exception ex)
            {
                throw new CryoDataException($"Failed to convert hex string {input}", ex);
            }
        }

        public static byte SafeCharToByte(char c)
        {
            if (c > 255)
            {
                throw new ArgumentException($"Char '{c}' is forbidden. Use basic ascii chars.");
            }
            return (byte)c;
        }

        //// "UVW" --> { 85, 86, 87 }
        //public static byte[] StringToBytesSequence(string input)
        //{
        //    return input
        //            .Select(c => {

        //                if (c > 127)
        //                {
        //                    throw new ArgumentException($"String '{input}' contains char '{c}' which is forbidden. Use basic ascii chars.");
        //                }
        //                return (byte)c;
        //            })
        //            .ToArray();
        //}

        //// "UVW" --> "0x55,0x56,0x57"
        //public static string StringToHexString(string input)
        //{
        //    return BytesSequenceToHexString(StringToBytesSequence(input));
        //}
    }
}
