using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Archive.Data;
using NUnit.Framework;

namespace Archive.Services.Tests
{
    public class GZipServiceTests
    {
        private CompressionParams _params;
        private GZipService _sut;
        
        [SetUp]
        public void Setup()
        {
            _params = new CompressionParams();
            _sut = CreateSut();
        }

        private static GZipService CreateSut() => new GZipService();

        [Test]
        public void Execute_WrongCommand()
        {
            // arrange #1
            _params.CommandType = (CommandType)5;
            
            // act
            var exception = Assert.Throws<InvalidOperationException>(() => _sut.Execute(_params, CancellationToken.None));
            
            // assert
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception.Message, Is.EqualTo("Unknown command type"));
        }

        [Test]
        public void CompressDecompress_ConsistencyTest()
        {
            // common arrange
            var input = "input.txt";
            var output = "output.txt";
            var testInputText = "test text";
            
            if (File.Exists(input))
                File.Delete(input);
            if (File.Exists(output))
                File.Delete(output);

            File.AppendAllText(input, testInputText);
            
            // arrange #1
            _params.CommandType = CommandType.Compress;
            _params.InputPath = input;
            _params.OutputPath = output;
            
            // act #1
            _sut.Execute(_params, CancellationToken.None);

            // arrange #2
            _params.InputPath = output;
            _params.OutputPath = "result.txt";
            _params.CommandType = CommandType.Decompress;
            if (File.Exists(_params.OutputPath))
                File.Delete(_params.OutputPath);

            // act #2
            _sut.Execute(_params, CancellationToken.None);

            // assert
            var result = File.ReadAllText(_params.OutputPath);
            
            Assert.That(result, Is.EqualTo(testInputText));
        }

        [TestCase(1024L * 1024 * 1024)]
        [TestCase(2048L * 1024 * 1024)]
        [TestCase(4096L * 1024 * 1024)]
        public void CompressDecompress_TestLargeFile(long fileSize)
        {
            // common arrange
            var input = "input.txt";
            var output = "output.txt";
            var result = "result.txt";
            var sw = new Stopwatch();

            if (File.Exists(input))
                File.Delete(input);
            if (File.Exists(output))
                File.Delete(output);

            GenerateLargeFile(input, fileSize);

            // arrange #1
            _params.CommandType = CommandType.Compress;
            _params.InputPath = input;
            _params.OutputPath = output;

            // act #1
            sw.Start();
            _sut.Execute(_params, CancellationToken.None);

            // arrange #2
            _params.InputPath = output;
            _params.OutputPath = result;
            _params.CommandType = CommandType.Decompress;
            if (File.Exists(_params.OutputPath))
                File.Delete(_params.OutputPath);

            // act #2
            _sut.Execute(_params, CancellationToken.None);
            Console.WriteLine($"Execution time is {sw.Elapsed}");

            // assert
            var originalHash = CalculateFileMD5(input);
            var resultHash = CalculateFileMD5(result);

            // comparing file hashes
            Assert.That(resultHash, Is.EqualTo(originalHash));
        }
        
        [TestCase(4096L * 1024 * 1024)]
        public void CompressDecompress_TestCancellationToken(long fileSize)
        {
            // arrange
            var cancellationTokenSource = new CancellationTokenSource();
            var input = "input.txt";
            var output = "output.txt";

            if (File.Exists(input))
                File.Delete(input);
            if (File.Exists(output))
                File.Delete(output);

            GenerateLargeFile(input, fileSize);
            _params.CommandType = CommandType.Compress;
            _params.InputPath = input;
            _params.OutputPath = output;

            // act
            var task1 = Task.Factory.StartNew(() => _sut.Execute(_params, cancellationTokenSource.Token));
            var task2 = Task.Factory.StartNew(() => cancellationTokenSource.Cancel());
            Assert.DoesNotThrowAsync(async() => await Task.WhenAll(task1, task2));
            
            // assert
            Assert.That(File.Exists(_params.OutputPath), Is.False);
        }

        private static void GenerateLargeFile(string filename, long fileSize)
        {
            FileStream fs = new FileStream(filename, FileMode.CreateNew);
            fs.Seek(fileSize, SeekOrigin.Begin);
            fs.WriteByte(0);
            fs.Close();
        }

        private static string CalculateFileMD5(string filename)
        {
            using (var md5 = MD5.Create())
            using (var stream = File.OpenRead(filename))
            {
                var hash = md5.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }
    }
}