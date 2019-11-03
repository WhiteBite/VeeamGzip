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



            // Сжимает блок и в поле MTIME заголовка записывает размер выходного потока


            if (_transformer.Transform(blockSupplier,destination))
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

            if (_transformer.Transform(blockSupplier,  destination))
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
                var buf = new byte[1024 * 1024];
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
