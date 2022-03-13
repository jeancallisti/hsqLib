using HsqLib2;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests.HsqLib2
{
    public class TestHeader
    {
        [Test]
        public void TestHeaderChecksum()
        {

            var jess = new byte[] { 0x5F, 0x61, 0x00, 0x00, 0x25, 0xC6 };
            var bunk = new byte[] { 0xF2, 0x59, 0x00, 0xA6, 0x38, 0x82 };
            var chan = new byte[] { 0x81, 0x4A, 0x00, 0xDE, 0x21, 0xE1 };

            Assert.That(HsqHeader.CheckHeaderValid(jess), Is.True);
            jess[4] = 0x61;
            Assert.That(HsqHeader.CheckHeaderValid(jess), Is.False);

            Assert.That(HsqHeader.CheckHeaderValid(bunk), Is.True);
            bunk[2] = 0x61;
            Assert.That(HsqHeader.CheckHeaderValid(bunk), Is.False);

            Assert.That(HsqHeader.CheckHeaderValid(chan), Is.True);
            chan[3] = 0x61;
            Assert.That(HsqHeader.CheckHeaderValid(chan), Is.False);
        }

        [Test]
        public void TestHeaderUncompressedSize()
        {
            bool ignoreBadChecksum = false;

            var jess = new HsqHeader(new byte[] { 0x5F, 0x61, 0x00, 0x00, 0x25, 0xC6 }, ignoreBadChecksum);
            var bunk = new HsqHeader(new byte[] { 0xF2, 0x59, 0x00, 0xA6, 0x38, 0x82 }, ignoreBadChecksum);
            var chan = new HsqHeader(new byte[] { 0x81, 0x4A, 0x00, 0xDE, 0x21, 0xE1 }, ignoreBadChecksum);

            Assert.That(jess.UncompressedSize == 24927, Is.True);
            Assert.That(bunk.UncompressedSize == 23026, Is.True);
            Assert.That(chan.UncompressedSize == 19073, Is.True);
            Assert.That(chan.UncompressedSize == 666, Is.False);
        }
    }
}
