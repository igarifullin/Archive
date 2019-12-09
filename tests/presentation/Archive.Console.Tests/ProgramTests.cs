using NUnit.Framework;
using Archive.ConsoleApp;

namespace Archive.Console.Tests
{
    public class ProgramTests
    {
        [TestCase("decomp path1.txt path2.txt")]
        [TestCase("compr path1.txt path2.txt")]
        [TestCase("compress path1.txt")]
        [TestCase("decompress path1.txt")]
        [TestCase("compress path1.txt ")]
        public void Main_CheckWrongArguments(string input)
        {
            // arrange
            var args = input.Split(" ");
            
            // act
            int result = 0;
            Assert.DoesNotThrow(() => result = Program.Main(args));
            
            // assert
            Assert.That(result, Is.EqualTo(1));
        }
    }
}