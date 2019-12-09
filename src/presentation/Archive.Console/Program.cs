using System;
using System.Threading;
using Archive.Data;
using Archive.Services;

namespace Archive.ConsoleApp
{
    public class Program
    {
        private static readonly GZipService _gZipService;
        private static readonly CancellationTokenSource _cancellationTokenSource;

        static Program()
        {
            _gZipService = new GZipService();
            _cancellationTokenSource = new CancellationTokenSource();
        }
        
        public static int Main(string[] args)
        {
            if (!CompressionParams.TryParse(args, out var @params))
            {
                Console.WriteLine("Error! Usage: ArchiveTest.exe <compress/decompress> <source path> <destination path>");
                return 1;
            }

            Console.CancelKeyPress += Handler;

            try
            {
                _gZipService.Execute(@params, _cancellationTokenSource.Token);
                return 0;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return 1;
            }
        }
        
        private static void Handler(object sender, ConsoleCancelEventArgs e)
        {
            if (e.SpecialKey == ConsoleSpecialKey.ControlC)
            {
                _cancellationTokenSource.Cancel();
                e.Cancel = true;
            }
        }
    }
}