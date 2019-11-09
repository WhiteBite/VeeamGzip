using System;
using System.IO;

namespace ZipperVeeam
{
    internal static class ModeSelector
    {
        public enum IncreaseAction
        {
            Delete,
            Nothing,
            Copy
        }

        public static void Action(string file2, IncreaseAction mode = IncreaseAction.Nothing, string file1 = "")
        {   
            switch (mode)
            {
                case IncreaseAction.Delete:
                    File.Delete(file2);
                    break;
                case IncreaseAction.Copy:
                    if (File.Exists(file1) && file2.Length != 0)
                        File.Copy(file1, file2);
                    else
                        Console.WriteLine("Error copying file! Check path or name of second file");
                    break;
                case IncreaseAction.Nothing:
                    Console.WriteLine("Nothing will be done...");
                    break;
            }
        }
    }
}
