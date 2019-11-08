using System;
using System.IO;
using System.IO.Compression;

namespace ZipperVeeam
{

    public class ParallelGZipArchiver
    {
        public Exception Exception { get; protected set; }
        private ParallelByteArrayTransformer _transformer = new ParallelByteArrayTransformer();

        public static FileStream _source;
        public static FileStream _destination;

        public bool Compress(string source, string destination)
        {
            using (_source = new FileStream(source, FileMode.Open))
            using (_destination = new FileStream(destination, FileMode.CreateNew))
            {
                var blockSupplier = new NonCompressedBlockSupplier(_source, Constants.BufSize);
                // Сжимает блок и в поле MTIME заголовка записывает размер выходного потока
                if (_transformer.Transform(blockSupplier, _destination, true))
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
            using (_source = new FileStream(source, FileMode.Open))
            using (_destination = new FileStream(destination, FileMode.CreateNew))
            {
                var blockSupplier = new GZipCompressedBlockSupplier(_source);
                if (_transformer.Transform(blockSupplier, _destination, false))
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