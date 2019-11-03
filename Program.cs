using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using ZipperVeeam;

//1) При декомпрессии ни ресурсы CPU, ни ресурсы диска не используются на 100%. Это приводит к не оптимальному времени работы.Можно ли это исправить?
//2) Проблемы с ООП:
//++  a.В классе Program происходит смешение ответственностей: обработка аргументов, управление потоками и т.д.
//b.В классе ParallelByteArrayTransformer также происходит смешение ответственностей: управление потоками, работа с очередями и т.д.
//Можно ли это исправить?
//3)++  Требуется продемонстрировать ООП, а не функциональный подход.Это касается следующих моментов:
//++ a.ParallelByteArrayTransformer.TransformMethod.
//++ b.ParallelByteArrayTransformer.ConsumeMethod.
//++ c.Использование анонимных делегатов для этих методов.
//4) В коде большое количество «магических чисел». Требуется исправить это.
//5) При повторных сжатиях файлов их размер начинает ощутимо расти.Можно ли исправить это?

namespace GZipTest
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            start(args);
            //test(4);
        }

        public static void test(int count = 1)
        {
            for (int i = 0; i < count; i++)
            {
                string[] q = { "compress", "file" + i.ToString(), "cFile" + (i).ToString() };
                start(q);
                string[] q2 = { "decompress", "cFile" + i.ToString(), "file" + (i + 1).ToString() };
                Thread.Sleep(100);
                start(q2);
            }
        }
        public static void start(string[] args)
        {
            if (args.Length != 3) { ZipperVeeam.HandlerGzip.PrintUsage(); return; }

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
                        Constants.isCompress = true;
                        handler.Сompress(args[1], args[2]);
                        break;

                    case "decompress":
                        Constants.isCompress = false;
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








//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.IO;
//using System.IO.Compression;
//using System.Linq;
//using System.Threading;

//namespace GZipTest
//{
//    internal class Program
//    {
//        public static void Main(string[] args)
//        {
//            if (args.Length != 3) { PrintUsage(); return; }

//            Console.WriteLine("Start");
//            var timer = new Stopwatch();
//            var pgzip = new ParallelGZipArchiver(); 

//            try
//            {
//                Console.WriteLine("Processing...");
//                timer.Start();
//                switch (args[0])
//                {
//                    case "compress":
//                        using (FileStream source = new FileStream(args[1], FileMode.Open))
//                        using (FileStream destination = new FileStream(args[2], FileMode.CreateNew))
//                        {
//                            if (!pgzip.Compress(source, destination))
//                            {
//                                PrintError(pgzip.Exception);
//                                Environment.Exit(1);
//                            }
//                        }
//                        break;

//                    case "decompress":
//                        using (FileStream source = new FileStream(args[1], FileMode.Open))
//                        using (FileStream destination = new FileStream(args[2], FileMode.CreateNew))
//                        {
//                            if (!pgzip.Decompress(source, destination))
//                            {
//                                PrintError(pgzip.Exception);
//                                Environment.Exit(1);
//                            }
//                        }

//                        break;

//                    default:
//                        PrintUsage();
//                        return;
//                }
//            }
//            catch (Exception e)
//            {
//                PrintError(e);
//                Environment.Exit(1);
//            }
//            timer.Stop();
//            Console.WriteLine($"Success! Elapsed time: {timer.ElapsedMilliseconds}ms");
//        }

//        public static void PrintError(Exception e)
//        {
//            Console.WriteLine("Error!");
//            Console.WriteLine(e.GetType().ToString());
//            Console.WriteLine($"{e.Message}\nIn {e.Source}.{e.TargetSite}");
//            Console.WriteLine(e.StackTrace);
//            if (e.InnerException != null) PrintError(e.InnerException);
//        }
//        /// <summary>
//        /// menu
//        /// </summary>
//        public static void PrintUsage()
//        {
//            Console.WriteLine("Use: compress|decompress source destination");
//        }
//    }
//}