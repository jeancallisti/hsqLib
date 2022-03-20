using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CryoDataLib.ImageLib
{
    public class CryoImageAlternativeImplementation
    {
		public byte[] interpretRLE(byte[] input, int width, int height)
        {
			var result = new byte[width*height];
			var inputPos = 0;

			for (int y = 0; y != height; ++y) {
				int dst = 0;
				int line_remain = 4 * ((width + 3) / 4);

				do {
					byte cmd = input[inputPos++];

					if ((cmd & 0x80) != 0) { // 1000 0000 <=> < 0
						int count = 257 - cmd;
						byte value = input[inputPos++];

						byte p1 = (byte)(value & 0x0f);
						byte p2 = (byte)(value >> 4);

						for (int i = 0; i != count; ++i) {
							if (p1 != 0) {
								result[y*width+dst] = p1;
							}
							dst++;
							if (p2 != 0) {
								result[y * width + dst] = p2;
							}
							dst++;
						}
						line_remain -= 2 * count;
					} else { // >= 0
						int count = cmd + 1;
						for (int i = 0; i != count; ++i) {
							byte value = input[inputPos++];

							byte p1 = (byte)(value & 0x0f);
							byte p2 = (byte)(value >> 4);

							if (p1 != 0) {
								result[y * width + dst] = p1;
							}
							dst++;
							if (p2 != 0) {
								result[y * width + dst] = p2;
							}
							dst++;
						}
						line_remain -= 2 * count;
					}
				} while (line_remain > 0);
			}

			return result;
        }
    }
}
