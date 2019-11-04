using System;
using System.Diagnostics;
using System.IO;



namespace ZipperVeeam
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            start(args);
        }


        public static void start(string[] args)
        {
            if (args.Length != 3) { ZipperVeeam.HandlerGzip.PrintUsage(); return; }

            if (!File.Exists(args[1]))
            {
                Console.WriteLine("File not found!");
                return 1;
            }
            Console.WriteLine("Start");
            var timer = new Stopwatch();
            var pgzip = new ParallelGZipArchiver();

            ZipperVeeam.HandlerGzip handler = new ZipperVeeam.HandlerGzip();

            try
            {
                if (File.Exists(args[2]))
                    File.Delete(args[2]);
                Console.WriteLine("Processing...");
                timer.Start();
                switch (args[0])
                {
                    case "compress":
                        handler.Сompress(args[1], args[2]);
                        break;

                    case "decompress":
                        handler.Decompress(args[1], args[2]);
                        break;

                    default:
                        ZipperVeeam.HandlerGzip.PrintUsage();
                        return;
                }
            }
            catch (Exception e)
            {
                ZipperVeeam.HandlerGzip.PrintError(e);
                Environment.Exit(1);
            }
            timer.Stop();
            Console.WriteLine($"Success! Elapsed time: {timer.ElapsedMilliseconds}ms");
        }
    }
}