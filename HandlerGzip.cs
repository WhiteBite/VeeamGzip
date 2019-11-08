using System;
using System.IO;

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
        public void Decompress(string source, string destination)
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
        //public void Сompress(string source, string destination)
        //{
        //    using (_source = new FileStream(source, FileMode.Open))
        //    using (_destination = new FileStream(destination, FileMode.CreateNew))
        //    {
        //        if (!pgzip.Compress(_source, _destination))
        //        {
        //            HandlerGzip.PrintError(pgzip.Exception);
        //            Environment.Exit(1);
        //        }
        //    }
        //}
    }
}
