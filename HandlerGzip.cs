using ZipperVeeam;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
//1) При декомпрессии ни ресурсы CPU, ни ресурсы диска не используются на 100%. Это приводит к не оптимальному времени работы.Можно ли это исправить?
//2) Проблемы с ООП:
//a.В классе Program происходит смешение ответственностей: обработка аргументов, управление потоками и т.д.
//b.В классе ParallelByteArrayTransformer также происходит смешение ответственностей: управление потоками, работа с очередями и т.д.
//Можно ли это исправить?
//3) Требуется продемонстрировать ООП, а не функциональный подход.Это касается следующих моментов:
//a.ParallelByteArrayTransformer.TransformMethod.
//b.ParallelByteArrayTransformer.ConsumeMethod.
//c.Использование анонимных делегатов для этих методов.
//4) В коде большое количество «магических чисел». Требуется исправить это.
//5) При повторных сжатиях файлов их размер начинает ощутимо расти.Можно ли исправить это?
namespace ZipperVeeam
{
    class HandlerGzip
    {
        //TODO
        public static FileStream _source;
        public static FileStream _destination;
        ParallelGZipArchiver pgzip = new ParallelGZipArchiver();

        public static void PrintUsage()
        {
            Console.WriteLine("Use: compress|decompress source destination");
        }
        public static void PrintError(Exception e)
        {
            Console.WriteLine("Error!");
            Console.WriteLine(e.GetType().ToString());
            Console.WriteLine($"{e.Message}\nIn {e.Source}.{e.TargetSite}");
            Console.WriteLine(e.StackTrace);
            if (e.InnerException != null) PrintError(e.InnerException);
        }
        public void Decompress(string source,string destination)
        {
            using (_source = new FileStream(source, FileMode.Open))
            using (_destination = new FileStream(destination, FileMode.CreateNew))
            {
                if (!pgzip.Decompress(_source, _destination))
                {
                    PrintError(pgzip.Exception);
                    Environment.Exit(1);
                }
            }
        }
        public void Сompress(string source,string destination)
        {
            using (_source = new FileStream(source, FileMode.Open))
            using (_destination = new FileStream(destination, FileMode.CreateNew))
            {
                if (!pgzip.Compress(_source, _destination))
                {
                    HandlerGzip.PrintError(pgzip.Exception);
                    Environment.Exit(1);
                }
            }
        }
    }
}
