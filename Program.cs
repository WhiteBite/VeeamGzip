using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;

namespace GZipTest
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length != 3) { PrintUsage(); return; }

            Console.WriteLine("Start");
            var timer = new Stopwatch();
            var pgzip = new ParallelGZipArchiver(); 

            try
            {
                Console.WriteLine("Processing...");
                timer.Start();
                switch (args[0])
                {
                    case "compress":
                        using (FileStream source = new FileStream(args[1], FileMode.Open))
                        using (FileStream destination = new FileStream(args[2], FileMode.CreateNew))
                        {
                            if (!pgzip.Compress(source, destination))
                            {
                                PrintError(pgzip.Exception);
                                Environment.Exit(1);
                            }
                        }
                        break;

                    case "decompress":
                        using (FileStream source = new FileStream(args[1], FileMode.Open))
                        using (FileStream destination = new FileStream(args[2], FileMode.CreateNew))
                        {
                            if (!pgzip.Decompress(source, destination))
                            {
                                PrintError(pgzip.Exception);
                                Environment.Exit(1);
                            }
                        }

                        break;

                    default:
                        PrintUsage();
                        return;
                }
            }
            catch (Exception e)
            {
                PrintError(e);
                Environment.Exit(1);
            }
            timer.Stop();
            Console.WriteLine($"Success! Elapsed time: {timer.ElapsedMilliseconds}ms");
        }
        
        public static void PrintError(Exception e)
        {
            Console.WriteLine("Error!");
            Console.WriteLine(e.GetType().ToString());
            Console.WriteLine($"{e.Message}\nIn {e.Source}.{e.TargetSite}");
            Console.WriteLine(e.StackTrace);
            if (e.InnerException != null) PrintError(e.InnerException);
        }
        /// <summary>
        /// menu
        /// </summary>
        public static void PrintUsage()
        {
            Console.WriteLine("Use: compress|decompress source destination");
        }
    }
}