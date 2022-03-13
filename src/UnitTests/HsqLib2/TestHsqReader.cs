using HsqLib2;
using HsqLib2.BinaryReader;
using HsqLib2.HsqReader;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace UnitTests
{
    /// <summary>
    /// Unit Tests for version 1 of the library (verbatim from ZWomp's source code)
    /// </summary>
    public class TestHsqReader
    {

        private async Task CompareInputOutput(byte[] input_bytes,  byte[] output_bytes)
        {
            using (var stream = new MemoryStream(input_bytes))
            using (var binaryReader = new System.IO.BinaryReader(stream))
            {
                var hsqReader = new HsqReader();
                bool ignoreBadChecksum = true;
                var output = await hsqReader.Unpack("", new CustomBinaryReader(binaryReader), ignoreBadChecksum);

                var outputData = output.UnCompressedData;

                Assert.That(outputData.Length, Is.EqualTo(output_bytes.Length));
                Assert.AreEqual(outputData, output_bytes);
            }
        }

        [Test]
        public async Task CanUncompressFileWithoutRepeatedData()
        {
            var input_bytes = new byte[]
            {
                0x18, 0x00, 0x10, 0x00, 0x00, 0x83, // Header (mabye checksum is right?)
                0xFF, 0xFF, // Instructions
                0x01, 0x02, 0x04, 0x08, 0x0C, 0x10, 0x03, 0x11, // Data
                0x21, 0x32, 0xA4, 0xC8, 0x1C, 0x1B, 0xB3, 0xF1,
            };

            var output_bytes = new byte[]
            {
                0x01, 0x02, 0x04, 0x08, 0x0C, 0x10, 0x03, 0x11,
                0x21, 0x32, 0xA4, 0xC8, 0x1C, 0x1B, 0xB3, 0xF1,
            };

            await CompareInputOutput(input_bytes, output_bytes);
        }

        
        [Test]
        public async Task CanUncompressFileWithoutRepeatedDataMultipleInstructions()
        {
            var input_bytes = new byte[]
            {
                0x2A, 0x00, 0x20, 0x00, 0x00, 0x83, // Header (mabye checksum is right?)
                0xFF, 0xFF, // Instructions
                0x01, 0x02, 0x04, 0x08, 0x0C, 0x10, 0x03, 0x11, // Data
                0x21, 0x32, 0xA4, 0xC8, 0x1C, 0x1B, 0xB3, 0xF1,
                0xFF, 0xFF, // Instructions
                0x31, 0xA2, 0x74, 0xD8, 0xDC, 0x1D, 0x13, 0x22, // Data
                0x27, 0x64, 0x14, 0xCA, 0xAC, 0xAB, 0xA3, 0xE2,
            };
            var output_bytes = new byte[]
            {
            0x01, 0x02, 0x04, 0x08, 0x0C, 0x10, 0x03, 0x11,
            0x21, 0x32, 0xA4, 0xC8, 0x1C, 0x1B, 0xB3, 0xF1,
            0x31, 0xA2, 0x74, 0xD8, 0xDC, 0x1D, 0x13, 0x22,
            0x27, 0x64, 0x14, 0xCA, 0xAC, 0xAB, 0xA3, 0xE2,

            };

            await CompareInputOutput(input_bytes, output_bytes);
        }
        

        [Test]
        public async Task CanUncompressWithMethodZero_Lenght2()
        {
            var input_bytes = new byte[]
            {
                0x18, 0x00, 0x0D, 0x00, 0x00, 0x83, // Header (checksum wrong)
                0xFF, 0xC3, // Instructions
                0x01, 0x02, 0x04, 0x08, 0x0C, 0x10, 0x03, 0x11, // Data
                0x21, 0x32,
                0xF7, // Distance
                0xF1, // Data
            };
            var output_bytes = new byte[]
            {
            0x01, 0x02, 0x04, 0x08, 0x0C, 0x10, 0x03, 0x11,
            0x21, 0x32,
            0x02, 0x04,
            0xF1,
            };

            await CompareInputOutput(input_bytes, output_bytes);

        }

        [Test]
        public async Task CanUncompressWithMethodZero_Lenght5()
        {
            var input_bytes = new byte[]
            {
                0x18, 0x00, 0x10, 0x00, 0x00, 0x83, // Header (checksum wrong)
                0xFF, 0xF3, // Instructions
                0x01, 0x02, 0x04, 0x08, 0x0C, 0x10, 0x03, 0x11, // Data
                0x21, 0x32,
                0xF7, // Distance
                0xF1, // Data
            };
            var output_bytes = new byte[]
            {
                0x01, 0x02, 0x04, 0x08, 0x0C, 0x10, 0x03, 0x11,
                0x21, 0x32,
                0x02, 0x04, 0x08, 0x0C, 0x10,
                0xF1,
            };

            await CompareInputOutput(input_bytes, output_bytes);

        }

        [Test]
        public async Task CanUncompressWithMethodZeroWhenInstructionsAreSplitUp()
        {
            var input_bytes = new byte[]
            {
                0x0D, 0x00, 0x15, 0x00, 0x00, 0x83, // Header (checksum wrong)
                0x1F, 0x1E, // Instructions. They end up with bit '0' which means we have only a half instruction...
                0xF0, 0xFF, 0x2F, 0x22, 0x12,
                0xFE, // Distance
                0x54, 0xFD, 0x33, 0x03,
                0x0E, 0x0F, // ...With the other half here, in this new batch of instructions.
                0xFD, // Distance
            };

            var output_bytes = new byte[]
            {
                0xF0, 0xFF, 0x2F, 0x22, 0x12,
                0x22, 0x12,
                0x54, 0xFD, 0x33, 0x03,
                0xFD, 0x33,
            };

            await CompareInputOutput(input_bytes, output_bytes);

        }


        [Test]
        public async Task CanUncompressWithMethodOne()
        {
            var input_bytes = new byte[]
            {
                0x17, 0x00, 0x13, 0x00, 0x00, 0x83, // Header (checksum wrong)
                0xFF, 0xFE, // Instructions
                0x01, 0x02, 0x04, 0x08, 0x0C, 0x10, 0x03, 0x11, // Data
                0xD2, 0xFF, // Lenght & Distance, 0100 1011 1111 1111

                0x21, 0x32, 0xA4, 0xC8, 0x1C, 0x1B, // Data
            };

            var output_bytes = new byte[]
            {
                0x01, 0x02, 0x04, 0x08, 0x0C, 0x10, 0x03, 0x11,
                0x04, 0x08, 0x0C, 0x10,
                0x21, 0x32, 0xA4, 0xC8, 0x1C, 0x1B,
            };

            await CompareInputOutput(input_bytes, output_bytes);
        }

        [Test]
        public async Task CanUncompressWithMethodOneUsingAlternativeLengthByte()
        {
            var input_bytes = new byte[]
            {
                0x17, 0x00, 0x13, 0x00, 0x00, 0x83, // Header (checksum wrong)
                0xFF, 0xFE, // Instructions
                0x01, 0x02, 0x04, 0x08, 0x0C, 0x10, 0x03, 0x11, // Data
                0xD0, 0xFF, // Lenght & Distance, 000 1011 1111 1111 1
                0x02, // Alternative Length byte
                0x21, 0x32, 0xA4, 0xC8, 0x1C, 0x1B, // Data
            };

            var output_bytes = new byte[]
            {
                0x01, 0x02, 0x04, 0x08, 0x0C, 0x10, 0x03, 0x11,
                0x04, 0x08, 0x0C, 0x10,
                0x21, 0x32, 0xA4, 0xC8, 0x1C, 0x1B,
            };

            await CompareInputOutput(input_bytes, output_bytes);

        }

        [Test]
        public async Task CanUncompressWithMethodOneAndEofMarker()
        {
            var input_bytes = new byte[]
            {
                0x17, 0x00, 0x13, 0x00, 0x00, 0x83, // Header (checksum wrong)
                0xFF, 0xFE, // Instructions
                0x01, 0x02, 0x04, 0x08, 0x0C, 0x10, 0x03, 0x11, // Data
                0xD0, 0xFF, // Lenght & Distance, 000 1011 1111 1111 1
                0x00, // EOF Marker
            };

            var output_bytes = new byte[]
            {
                0x01, 0x02, 0x04, 0x08, 0x0C, 0x10, 0x03, 0x11,
            };

            await CompareInputOutput(input_bytes, output_bytes);

        }

        [Test]
        public async Task CanUncompressWithMethodOneWhereLengthIsGreaterThanDistance()
        {
            var input_bytes = new byte[]
            {
                0x17, 0x00, 0x13, 0x00, 0x00, 0x83, // Header (checksum wrong)
                0xFF, 0xFE, // Instructions
                0x01, 0x02, 0x04, 0x08, 0x0C, 0x10, 0x03, 0x11, // Data
                0xF0, 0xFF, // Lenght & Distance
                0x04, // Length
                0x54, 0x38, 0x1C, 0x22, 0x35, 0x41,
            };

            var output_bytes = new byte[]
            {
                0x01, 0x02, 0x04, 0x08, 0x0C, 0x10, 0x03, 0x11,
                0x03, 0x11, 0x03, 0x11, 0x03, 0x11,
                0x54, 0x38, 0x1C, 0x22, 0x35, 0x41,
            };

            await CompareInputOutput(input_bytes, output_bytes);

        }



        [Test]
        public void CanUncompressRealExample()
        {
            var input_file = "Data\\SAMPLE.HSQ";
            var compare_to_file = "Data\\SAMPLE.HSQ.UNPACKED";

            var compareTobytes = File.ReadAllBytes(compare_to_file);

            using (var inputStream = File.OpenRead(input_file))
            {
                var reader = new HsqReader();

                var task = Task.Run(async () =>
                {
                    var unpacked = await reader.UnpackFile(inputStream, false);

                    Assert.That(unpacked.UnCompressedData.Length, Is.EqualTo(compareTobytes.Length));
                    Assert.AreEqual(unpacked.UnCompressedData, compareTobytes);
                });
                task.Wait();
            }
        }

        /*
        [Test]
        public void CanValidateHeader()
        {
            var file = new Mock<IHsqCompressedFile>();

            var jess = new byte[] { 0x5F, 0x61, 0x00, 0x00, 0x25, 0xC6 };
            var bunk = new byte[] { 0xF2, 0x59, 0x00, 0xA6, 0x38, 0x82 };
            var chan = new byte[] { 0x81, 0x4A, 0x00, 0xDE, 0x21, 0xE1 };

            file.Setup(x => x.GetHeaderBytes()).Returns(jess);
            Assert.That(HsqHandler.ValidateHeader(file.Object), Is.True);
            jess[4] = 0x61;
            Assert.That(HsqHandler.ValidateHeader(file.Object), Is.False);

            file.Setup(x => x.GetHeaderBytes()).Returns(bunk);
            Assert.That(HsqHandler.ValidateHeader(file.Object), Is.True);
            bunk[2] = 0x61;
            Assert.That(HsqHandler.ValidateHeader(file.Object), Is.False);

            file.Setup(x => x.GetHeaderBytes()).Returns(chan);
            Assert.That(HsqHandler.ValidateHeader(file.Object), Is.True);
            chan[3] = 0x61;
            Assert.That(HsqHandler.ValidateHeader(file.Object), Is.False);
        }

        [Test]
        public void ValidateOutputSize()
        {
            var file = new Mock<IHsqCompressedFile>();
            var list = new Mock<IList<byte>>();

            var jess = new byte[] { 0x5F, 0x61, 0x00, 0x00, 0x25, 0xC6 };
            var bunk = new byte[] { 0xF2, 0x59, 0x00, 0xA6, 0x38, 0x82 };
            var chan = new byte[] { 0x81, 0x4A, 0x00, 0xDE, 0x21, 0xE1 };

            file.Setup(x => x.GetHeaderBytes()).Returns(jess);
            list.Setup(x => x.Count).Returns(24927);
            Assert.That(HsqHandler.ValidateOutputSize(file.Object, list.Object), Is.True);

            file.Setup(x => x.GetHeaderBytes()).Returns(bunk);
            list.Setup(x => x.Count).Returns(23026);
            Assert.That(HsqHandler.ValidateOutputSize(file.Object, list.Object), Is.True);

            file.Setup(x => x.GetHeaderBytes()).Returns(chan);
            list.Setup(x => x.Count).Returns(19073);
            Assert.That(HsqHandler.ValidateOutputSize(file.Object, list.Object), Is.True);

            file.Setup(x => x.GetHeaderBytes()).Returns(chan);
            list.Setup(x => x.Count).Returns(666);
            Assert.That(HsqHandler.ValidateOutputSize(file.Object, list.Object), Is.False);
        }

        [Test]
        public void CanValidateMadeUpHeaders()
        {
            var file = new Mock<IHsqCompressedFile>();

            var fake1 = new byte[] { 0x10, 0x00, 0x00, 0x14, 0x00, 0x87 };
            var fake2 = new byte[] { 0x0D, 0x00, 0x00, 0x15, 0x00, 0x89 };
            var fake3 = new byte[] { 0x12, 0x00, 0x00, 0x18, 0x00, 0x81 };
            var fake4 = new byte[] { 0x12, 0x00, 0x00, 0x19, 0x00, 0x80 };
            var fake5 = new byte[] { 0x08, 0x00, 0x00, 0x13, 0x00, 0x90 };
            var fake6 = new byte[] { 0x08, 0x00, 0x00, 0x13, 0x00, 0x90 };

            file.Setup(x => x.GetHeaderBytes()).Returns(fake1);
            Assert.That(HsqHandler.ValidateHeader(file.Object), Is.True);

            file.Setup(x => x.GetHeaderBytes()).Returns(fake2);
            Assert.That(HsqHandler.ValidateHeader(file.Object), Is.True);

            file.Setup(x => x.GetHeaderBytes()).Returns(fake3);
            Assert.That(HsqHandler.ValidateHeader(file.Object), Is.True);

            file.Setup(x => x.GetHeaderBytes()).Returns(fake4);
            Assert.That(HsqHandler.ValidateHeader(file.Object), Is.True);

            file.Setup(x => x.GetHeaderBytes()).Returns(fake5);
            Assert.That(HsqHandler.ValidateHeader(file.Object), Is.True);

            file.Setup(x => x.GetHeaderBytes()).Returns(fake6);
            Assert.That(HsqHandler.ValidateHeader(file.Object), Is.True);
        }
    }

    [TestFixture]
    public class HsqCompressedFileTests
    {
        [Test]
        public void CanGetCompressedFileSize()
        {
            var file1bytes = new byte[] { 0xF5, 0x61, 0x00, 0x00, 0x25, 0xC6 }; // JESS.HSQ
            var file1 = new HsqCompressedFile(file1bytes);
            Assert.That(file1.GetCompressedFileSize(), Is.EqualTo(9472));

            var file2bytes = new byte[] { 0xF0, 0xC9, 0x00, 0x17, 0xC2, 0x19 }; // SDB.HSQ
            var file2 = new HsqCompressedFile(file2bytes);
            Assert.That(file2.GetCompressedFileSize(), Is.EqualTo(49687));
        }

        [Test]
        public void CanGetUncompressedFileSize()
        {
            var file1bytes = new byte[] { 0xF5, 0x61, 0x00, 0x00, 0x25, 0xC6 }; // JESS.HSQ
            var file1 = new HsqCompressedFile(file1bytes);
            Assert.That(file1.GetUncompressedFileSize(), Is.EqualTo(25077));

            var file2bytes = new byte[] { 0xF0, 0xC9, 0x00, 0x17, 0xC2, 0x19 }; // SDB.HSQ
            var file2 = new HsqCompressedFile(file2bytes);
            Assert.That(file2.GetUncompressedFileSize(), Is.EqualTo(51696));
        }

        [Test]
        public void CanSeeIfOffsetIsAtEOF()
        {
            var input1 = new byte[]
            {
                0x18, 0x00, 0x10, 0x00, 0x00, 0x83, // Header (mabye checksum is right?)
            };

            var input2 = new byte[]
            {
                0x18, 0x00, 0x10, 0x00, 0x00, 0x83, // Header (mabye checksum is right?)
                0xFF, 0xFF, // Instructions
                0x01, 0x02, 0x04, 0x08, 0x0C, 0x10, 0x03, 0x11, // Data
                0x21, 0x32, 0xA4, 0xC8, 0x1C, 0x1B, 0xB3, 0xF1,
            };

            var file1 = new HsqCompressedFile(input1);
            var file2 = new HsqCompressedFile(input2);

            Assert.That(file1.EOF, Is.True);
            Assert.That(file2.EOF, Is.False);
        }

        [Test]
        public void CanGetNextWord()
        {
            var input = new byte[]
            {
                0x18, 0x00, 0x10, 0x00, 0x00, 0x83, // Header (mabye checksum is right?)
                0xF0, 0xC2, // Instructions (made up)
                0x01, 0x02, 0x04, 0x08, 0x0C, 0x10, 0x03, 0x11, // Data
                0x21, 0x32, 0xA4, 0xC8, 0x1C, 0x1B, 0xB3, 0xF1,
            };

            var file = new HsqCompressedFile(input);

            var word1 = file.GetNextWord();

            Assert.That(word1.Length, Is.EqualTo(2));
            Assert.That(word1[0], Is.EqualTo(0xF0));
            Assert.That(word1[1], Is.EqualTo(0xC2));

            var word2 = file.GetNextWord();

            Assert.That(word1.Length, Is.EqualTo(2));
            Assert.That(word2[0], Is.EqualTo(0x01));
            Assert.That(word2[1], Is.EqualTo(0x02));
        }

        [Test]
        public void GetNextByte()
        {
            var input = new byte[]
            {
                0x18, 0x00, 0x10, 0x00, 0x00, 0x83, // Header (mabye checksum is right?)
                0xF0, 0xC2, // Instructions (made up)
                0x01, 0x02, 0x04, 0x08, 0x0C, 0x10, 0x03, 0x11, // Data
                0x21, 0x32, 0xA4, 0xC8, 0x1C, 0x1B, 0xB3, 0xF1,
            };

            var file = new HsqCompressedFile(input);

            Assert.That(file.GetNextByte(), Is.EqualTo(0xF0));
            Assert.That(file.GetNextByte(), Is.EqualTo(0xC2));
        }
    }

    [TestFixture]
    public class InstructionsReaderTests
    {
        [Test]
        public void CanReadParseInstructionsThatDontIncludeCallsToGetRepeatedData()
        {
            var mock_file = new Mock<IHsqCompressedFile>();
            mock_file.Setup(x => x.GetNextWord()).Returns(new byte[] { 0xFF, 0xFF });

            var reader = new InstructionsReader(mock_file.Object);

            Assert.That(reader.GetNextStep(), Is.TypeOf(typeof(CopyByte))); // 1
            Assert.That(reader.GetNextStep(), Is.TypeOf(typeof(CopyByte))); // 2
            Assert.That(reader.GetNextStep(), Is.TypeOf(typeof(CopyByte))); // 3
            Assert.That(reader.GetNextStep(), Is.TypeOf(typeof(CopyByte))); // 4
            Assert.That(reader.GetNextStep(), Is.TypeOf(typeof(CopyByte))); // 5
            Assert.That(reader.GetNextStep(), Is.TypeOf(typeof(CopyByte))); // 6
            Assert.That(reader.GetNextStep(), Is.TypeOf(typeof(CopyByte))); // 7
            Assert.That(reader.GetNextStep(), Is.TypeOf(typeof(CopyByte))); // 8
            Assert.That(reader.GetNextStep(), Is.TypeOf(typeof(CopyByte))); // 9
            Assert.That(reader.GetNextStep(), Is.TypeOf(typeof(CopyByte))); // 10
            Assert.That(reader.GetNextStep(), Is.TypeOf(typeof(CopyByte))); // 11
            Assert.That(reader.GetNextStep(), Is.TypeOf(typeof(CopyByte))); // 12
            Assert.That(reader.GetNextStep(), Is.TypeOf(typeof(CopyByte))); // 13
            Assert.That(reader.GetNextStep(), Is.TypeOf(typeof(CopyByte))); // 14
            Assert.That(reader.GetNextStep(), Is.TypeOf(typeof(CopyByte))); // 15
            Assert.That(reader.GetNextStep(), Is.TypeOf(typeof(CopyByte))); // 16

        }
    }

    [TestFixture]
    public class DoMethodZeroTests
    {
        [Test]
        public void CanReadHexValue7BCorrectly()
        {
            var intReader = new Mock<IInstructionsReader>();
            var hsqfile = new Mock<IHsqCompressedFile>();

            intReader.Setup(x => x.ReadNextBit()).Returns(false);
            hsqfile.Setup(x => x.GetNextByte()).Returns(0x7B);

            var dmz = new DoMethodZero(intReader.Object, hsqfile.Object);

            Assert.That(dmz.Length, Is.EqualTo(2));
            Assert.That(dmz.Distance, Is.EqualTo(-133));
        }

    }

    [TestFixture]
    public class DoMethodOneTests
    {
        [Test]
        public void CanGetLenghtAndDistance()
        {
            var input_bytes = new byte[]
            {
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0xD2, 0xFF, // Lenght & Distance, 0100 1011 1111 1111
            };

            var input = new HsqCompressedFile(input_bytes);

            var step = new DoMethodOne(input);

            Assert.That(step.EOF, Is.False);
            Assert.That(step.Length, Is.EqualTo(4));
            Assert.That(step.Distance, Is.EqualTo(-6));
        }

        [Test]
        public void CanUseAlternativeLengthByte()
        {
            var input_bytes = new byte[]
            {
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0xC0, 0xFF, // Lenght & Distance, 0000 0011 1111 1111
                0x0F, // Alternative Length byte
            };

            var input = new HsqCompressedFile(input_bytes);

            var step = new DoMethodOne(input);

            Assert.That(step.EOF, Is.False);
            Assert.That(step.Length, Is.EqualTo(17));
            Assert.That(step.Distance, Is.EqualTo(-8));
        }

        [Test]
        public void CanReadEOFMarker()
        {
            var input_bytes = new byte[]
            {
                0x17, 0x00, 0x13, 0x00, 0x00, 0x83, // Header (checksum wrong)
                0xD0, 0xFF, // Lenght & Distance, 000 1011 1111 1111 1
                0x00, // EOF Marker
            };

            var input = new HsqCompressedFile(input_bytes);

            var step = new DoMethodOne(input);

            Assert.That(step.EOF, Is.True);
            Assert.That(step.Length, Is.EqualTo(0));
            Assert.That(step.Distance, Is.EqualTo(0));
        }*/
    }
        
}


