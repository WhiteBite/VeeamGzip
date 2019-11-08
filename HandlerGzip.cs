using System;
using System.IO;

namespace ZipperVeeam
{
    internal static class HandlerGzip
    {
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
    }
}
