using System;
using System.Diagnostics;
using System.IO;



namespace ZipperVeeam
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            // start(args);
            test(2);
        }

        public static void test(int count)
        {

            //for (int i = 0; i < count; i++)
            // {
            //   string[] q1 = { "compress", "decompressed_test" + i.ToString() + ".iso", "compressed_test" + (i).ToString() + ".iso" };
            //   start(q1);
            int i = 2;
       //     string[] q1 = { "compress", "decompressed_test" + i.ToString() + ".iso", "compressed_test" + (i).ToString() + ".iso" };
       //     start(q1);
            string[] q2 = { "decompress", "compressed_test" + (i).ToString() + ".iso", "decompressed_test" + (i + 1).ToString() + ".iso" };
            start(q2);


            //  }
        }
        public static int start(string[] args)
        {
            if (args.Length != 3) { ZipperVeeam.HandlerGzip.PrintUsage(); return 0; }

            if (!File.Exists(args[1]))
            {
                Console.WriteLine("File not found!");
                return 1;
            }
            Console.WriteLine("Start");
            var timer = new Stopwatch();
            ZipperVeeam.HandlerGzip handler = new ZipperVeeam.HandlerGzip();

            ParallelGZipArchiver pgzip = new ParallelGZipArchiver();

            string tempstring = $"{ args[0] } source =\"{args[1]}\"  to \" {args[2]}\"";
            try
            {
                if (File.Exists(args[2]))
                    File.Delete(args[2]);
                Console.WriteLine("Processing...");
                Console.WriteLine($"Start {tempstring} ...");
                timer.Start();
                switch (args[0])
                {
                    case "compress":
                        pgzip.Compress(args[1], args[2]);
                        break;

                    case "decompress":
                        handler.Decompress(args[1], args[2]);
                        break;

                    default:
                        ZipperVeeam.HandlerGzip.PrintUsage();
                        return 0;
                }
            }
            catch (Exception e)
            {
                ZipperVeeam.HandlerGzip.PrintError(e);
                Environment.Exit(1);
            }
            timer.Stop();
            Console.WriteLine($"End {tempstring} ...");
            Console.WriteLine($"Success! Elapsed time: {timer.ElapsedMilliseconds}ms");
            return 0;
        }
    }
}