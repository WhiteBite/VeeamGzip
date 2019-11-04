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
//++ b.В классе ParallelByteArrayTransformer также происходит смешение ответственностей: управление потоками, работа с очередями и т.д.
//Можно ли это исправить?
//3)++  Требуется продемонстрировать ООП, а не функциональный подход.Это касается следующих моментов:
//++ a.ParallelByteArrayTransformer.TransformMethod.
//++ b.ParallelByteArrayTransformer.ConsumeMethod.
//++ c.Использование анонимных делегатов для этих методов.
//4) В коде большое количество «магических чисел». Требуется исправить это.
//5) При повторных сжатиях файлов их размер начинает ощутимо расти.Можно ли исправить это?

namespace ZipperVeeam
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            // Console.ReadKey();
            start(args);
            //test2(34);
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