using System;
using System.Diagnostics;
using System.IO;


namespace ZipperVeeam
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            //start(args);
            Test(5);
        }

        public static void Test(int count)
        {

            for (int i = 0; i < count; i++)
            {
                string[] q1 = { "compress", "de_comp_srv_" + i.ToString() + ".log", "comp_srv_" + (i + 1).ToString() + ".log" };
                //string[] q2 = { "compress", "comp_srv.log" + (i).ToString() /*+ ".iso"*/, "comp_srv.log" + (i+1).ToString() /*+ ".iso"*/ };
                string[] q3 = { "decompress", "comp_srv_" + (i+1).ToString() + ".log", "de_comp_srv_" + (i + 1).ToString() + ".log" };
                //string[] q4 = { "decompress", "de_comp_srv.log" + (i + 1).ToString() /*+ ".iso", "de_comp_srv.log" + (i + 2).ToString() /*+ ".iso" */};

                Start(q1);
                //Thread.Sleep(100);
                //Start(q2);
                //Thread.Sleep(100);
                Start(q3);
                //Thread.Sleep(100);
                //Start(q4);
                //Thread.Sleep(100);

            }
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
            //TODO
            //обработать ошибки в нормальный вид
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