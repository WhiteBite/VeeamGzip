using System;
using System.IO;

namespace ZipperVeeam
{

    public class ParallelGZipArchiver
    {
        public Exception Exception { get; protected set; }
        private readonly ParallelByteArrayTransformer _transformer = new ParallelByteArrayTransformer();

        public static FileStream Source;
        public static FileStream Destination;

        public bool Compress(string source, string destination)
        {
            using (Source = new FileStream(source, FileMode.Open))
            using (Destination = new FileStream(destination, FileMode.CreateNew))
            {
                var blockSupplier = new NonCompressedBlockSupplier(Source, Constants.BufSize);
                // Сжимает блок и в поле MTIME заголовка записывает размер выходного потока
                if (_transformer.Transform(blockSupplier, Destination, true))
                    return true;
                else
                {
                    Exception = new OperationCanceledException("Operation was cancelled by user.");
                    return false;
                }
            }
        }

        public bool Decompress(string source, string destination)
        {
            using (Source = new FileStream(source, FileMode.Open))
            using (Destination = new FileStream(destination, FileMode.CreateNew))
            {
                var blockSupplier = new GZipCompressedBlockSupplier(Source);
                if (_transformer.Transform(blockSupplier, Destination, false))
                    return true;
                else
                {
                    Exception = new OperationCanceledException("Operation was cancelled by request from user.");
                    return false;
                }
            }
        }
    }
}