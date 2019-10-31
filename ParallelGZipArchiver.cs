using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace GZipTest
{


    // Класс-архиватор, который по сути является адаптером к классу ParallelByteArrayTransformer
    // и позволяет использовать его для упаковки/распаковки потоков байтов.
    // Поддерживает отмену и может сообщить об ошибках, возникших в ходе работы (через поле).
    // Содержит запасной алгоритм распаковки файлов (однопоточный) на случай неудачи
    // многопоточного алгоритма.
    public class ParallelGZipArchiver 
    {
        public Exception Exception { get; protected set; }
        private ParallelByteArrayTransformer _transformer = new ParallelByteArrayTransformer();

        public bool Compress(Stream source, Stream destination)
        {
            var blockSupplier = new NonCompressedBlockSupplier(source, 1024 * 1024);

            ParallelByteArrayTransformer.ConsumeMethod consumeMethod = 
                (DataBlock dataBlock) => destination.Write(dataBlock.Data, 0, dataBlock.Size);


            // Сжимает блок и в поле MTIME заголовка записывает размер выходного потока
            ParallelByteArrayTransformer.TransformMethod compressMethod =
                (DataBlock dataBlock) =>
                {
                    
                    using (var ms = new MemoryStream())
                    {
                        using (var gzip = new GZipStream(ms, CompressionMode.Compress, true))
                        {
                            gzip.Write(dataBlock.Data, 0, dataBlock.Size);
                        }
                        ms.Seek(4, 0);
                        ms.Write(BitConverter.GetBytes((int)ms.Length), 0, 4);
                        dataBlock.Data = ms.ToArray();
                    }
                };

            if (_transformer.Transform(blockSupplier, compressMethod, consumeMethod))
                return true;
            else
            {
                Exception = new OperationCanceledException("Operation was cancelled by user."); ;
                return false;
            }
        }

        public bool Decompress(Stream source, Stream destination)
        {
            long _srcpos = source.Position;
            long _dstpos = destination.Position;

            var blockSupplier = new GZipCompressedBlockSupplier(source);

            ParallelByteArrayTransformer.ConsumeMethod consumeMethod =
                (DataBlock dataBlock) => destination.Write(dataBlock.Data, 0, dataBlock.Size);

            ParallelByteArrayTransformer.TransformMethod transformMethod =
                (DataBlock dataBlock) =>
                {
                    using (var ms = new MemoryStream(dataBlock.Data))
                    using (var gzip = new GZipStream(ms, CompressionMode.Decompress))
                    {
                        var outbuf = new byte[ms.Length * 2];
                        int bytesRead = 1, offset = 0;

                        while (bytesRead > 0)
                        {
                            bytesRead = gzip.Read(outbuf, offset, outbuf.Length - offset);
                            offset += bytesRead;

                            if (offset == outbuf.Length) Array.Resize(ref outbuf, outbuf.Length * 2);
                        }

                        Array.Resize(ref outbuf, offset);
                        dataBlock.Data = outbuf;
                    }
                };

            if (_transformer.Transform(blockSupplier, transformMethod, consumeMethod))
                return true;
            else
            {
                Exception = new OperationCanceledException("Operation was cancelled by request from user."); ;

                if (Exception is IOException ||
                    Exception is InvalidOperationException ||
                    Exception is ObjectDisposedException)
                {
                    return false;
                }

                Console.WriteLine("Main algorythm failed. Let's try other way.");

                source.Position = _srcpos;
                destination.Position = _dstpos;

                return BackupDecompress(source, destination);
            }
        }

        private bool BackupDecompress(Stream source, Stream destination)
        {
            try
            {
                var buf = new byte[1024*1024];
                var bytesRead = 1;

                while (source.Position != source.Length)
                {
                    using (var gzip = new GZipStream(source, CompressionMode.Decompress, true))
                    {
                        while (bytesRead > 0)
                        {
                            bytesRead = gzip.Read(buf, 0, buf.Length);
                            destination.Write(buf, 0, bytesRead);
                        }
                    }
                }
                return true;
            }
            catch (Exception e)
            {
                Exception = e;
                return false;
            }
        }

        public void Cancel()
        {
            _transformer.Cancel();
        }
    }
}
