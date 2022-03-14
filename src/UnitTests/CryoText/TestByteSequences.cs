using NUnit.Framework;
using CryoDataLib;
using System.Linq;

namespace UnitTests.CryoText
{
    public class TestByteSequences
    {
        [Test]
        public void TestFindPosition()
        {
            var search = new byte[3] { 128, 0, 2 };


            var input1 = new byte[10] { 47, 128, 0, 2, 47, 128, 0, 17, 47, 47 };
            var input2 = new byte[9] { 128, 0, 2, 47, 128, 0, 17, 47, 47 };
            var input3 = new byte[4] { 47, 128, 0, 2 };
            var input4 = new byte[3] { 128, 0, 2 };
            var input5 = new byte[8] { 47, 128, 0, 128, 0, 17, 47, 47 }; //not present

            var pos1 = input1.PositionOf(search);
            var pos2 = input2.PositionOf(search);
            var pos3 = input3.PositionOf(search);
            var pos4 = input4.PositionOf(search);
            var pos5 = input5.PositionOf(search);

            Assert.AreEqual(pos1, 1);
            Assert.AreEqual(pos2, 0);
            Assert.AreEqual(pos3, 1);
            Assert.AreEqual(pos4, 0);
            Assert.True(pos5 < 0);

        }


        [Test]
        public void TestReplace()
        {
            var search = new byte[3] { 128, 0, 2 };
            var replace = new byte[2] { 66, 66 };


            var input1 = new byte[10] { 47, 128, 0, 2, 47, 128, 0, 17, 47, 47 };
            var input2 = new byte[9] { 128, 0, 2, 47, 128, 0, 17, 47, 47 };
            var input3 = new byte[4] { 47, 128, 0, 2 };
            var input4 = new byte[3] { 128, 0, 2 };
            var input5 = new byte[8] { 47, 128, 0, 128, 0, 17, 47, 47 }; //not present

            var output1 = input1.Replace(search, replace);
            var output2 = input2.Replace(search, replace);
            var output3 = input3.Replace(search, replace);
            var output4 = input4.Replace(search, replace);
            var output5 = input5.Replace(search, replace);

            Assert.AreEqual(9, output1.Length);
            Assert.AreEqual(8, output2.Length);
            Assert.AreEqual(3, output3.Length);
            Assert.AreEqual(2, output4.Length);
            Assert.AreEqual(8, output5.Length); //unchanged

            Assert.True(output1[1] == 66 && output1[2] == 66);
            Assert.True(Enumerable.SequenceEqual(input5, output5));
        }
    }
}
