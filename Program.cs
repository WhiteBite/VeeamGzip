using System;
using System.Diagnostics;
using System.IO;


namespace ZipperVeeam
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            Start(args);
        }

        public static int Start(string[] args)
        {
            if (args.Length != 3) { InfoPrinter.PrintUsage(); return 0; }

            if (!File.Exists(args[1]))
            {
                Console.WriteLine($"Error! {args[0]}ed failed! ");
                Console.WriteLine($"File {args[1]} not found!");
                return 1;
            }

            Console.WriteLine("Start");
            var timer = new Stopwatch();
            ParallelGZipArchiver pgzip = new ParallelGZipArchiver();
            string tempString = $"{ args[0] } source =\"{args[1]}\"  to \" {args[2]}\"";
            try
            {
                if (File.Exists(args[2]))
                    File.Delete(args[2]);
                Console.WriteLine("Processing...");
                Console.WriteLine($"Start {tempString} ...");
                timer.Start();
                switch (args[0])
                {
                    case "compress":
                        pgzip.Compress(args[1], args[2]);
                        break;

                    case "decompress":
                        pgzip.Decompress(args[1], args[2]);
                        break;

                    default:
                        InfoPrinter.PrintUsage();
                        return 0;
                }
            }
            catch (Exception e)
            {
                InfoPrinter.PrintError(e);
                Environment.Exit(1);
            }
            timer.Stop();
            Console.WriteLine($"End {tempString} ...");
            Console.WriteLine($"Success! Elapsed time: {timer.ElapsedMilliseconds}ms");
            long firstFileSize = new FileInfo(args[1]).Length;
            long secondFileSize = new FileInfo(args[2]).Length;

            Console.WriteLine($"File before {args[0]} = {firstFileSize}\nFile after {args[0]} =  {secondFileSize}");
            if (args[0] == "compress")
                if (firstFileSize < secondFileSize)
                {
                    Console.WriteLine($"Compression did not give a better result! File size increased!");
                    ModeSelector.Action(args[2], ModeSelector.IncreaseAction.Delete);
                }

            return 0;
        }
    }
}