using System;
using System.Diagnostics;
using System.IO;
using System.Threading;


namespace ZipperVeeam
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            //start(args);
            test(5);
        }

        public static void test(int count)
        {

            for (int i = 0; i < count; i++)
            {
                string[] q1 = { "compress", "de_comp_srv.log" + i.ToString() /*+ ".iso"*/, "comp_srv.log" + (i).ToString() /*+ ".iso"*/ };
                string[] q2 = { "decompress", "comp_srv.log" + (i).ToString() /*+ ".iso"*/, "de_comp_srv.log" + (i + 1).ToString() /*+ ".iso" */};
                
                start(q1);
                start(q2);
                Thread.Sleep(100);

            }
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
            Console.WriteLine($"End {tempString} ...");
            Console.WriteLine($"Success! Elapsed time: {timer.ElapsedMilliseconds}ms");
            return 0;
        }
    }
}