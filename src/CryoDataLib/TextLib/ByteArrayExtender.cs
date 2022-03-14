using System;
using System.Linq;


namespace CryoDataLib
{
    public static class ByteArrayExtender
    {
        /// <summary>
        /// inside an array of bytes, replaces a specific sequence of bytes with another given sequence.
        /// </summary>
        public static byte[] Replace(this byte[] array, byte[] search, byte[] replacement)
        {
            if (Enumerable.SequenceEqual(search, replacement))
            {
                return array;
            }

            var position = array.PositionOf(search);

            if (position < 0 )
            {
                return array;
            }

            var result = array.ToList()
                .Take(position) //up until found sequence
                .Concat(replacement) //sequence
                .Concat(array.ToList().Skip(position + search.Length).Take(array.Length)) //after sequence
                .ToArray();

            return result;
        }

        public static int PositionOf(this byte[] array, byte[] search)
        {
            return (from i in Enumerable.Range(0, 1 + array.Length - search.Length)
                    where array.Skip(i).Take(search.Length).SequenceEqual(search)
                    select (int?)i).FirstOrDefault().GetValueOrDefault(-1);
        }
    }
}
