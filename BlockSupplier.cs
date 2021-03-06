﻿using System;
using System.IO;

namespace ZipperVeeam
{
    internal abstract class BlockSupplier
    {
        protected Stream SourceStream { get; }

        protected int PartNumber { get; set; }

        protected BlockSupplier(Stream sourceStream)
        {
            SourceStream = sourceStream;
        }
        public abstract DataBlock Next();
    }
    internal class NonCompressedBlockSupplier : BlockSupplier
    {
        private readonly int _blockSize;

        public NonCompressedBlockSupplier(Stream sourceStream, int blockSize) : base(sourceStream)
        {
            _blockSize = blockSize;
        }

        public override DataBlock Next()
        {
            lock (SourceStream)
            {
                var dataBlock = new DataBlock(PartNumber++, new byte[_blockSize]);
                int bytesRead, offset = 0;


                do
                {
                    bytesRead = SourceStream.Read(dataBlock.Data, offset, dataBlock.Size - offset);
                    offset += bytesRead;
                }
                while (offset != dataBlock.Size && bytesRead != 0);

                dataBlock.Resize(offset);

                return dataBlock;
            }
        }
    }

    internal class GZipCompressedBlockSupplier : BlockSupplier
    {
        public GZipCompressedBlockSupplier(Stream sourceStream) : base(sourceStream)
        {
        }

        public override DataBlock Next()
        {
            lock (SourceStream)
            {
                var buf = new byte[Constants.HeaderSize];
                var bytesRead = SourceStream.Read(buf, 0, buf.Length);
                if (bytesRead == 0) return new DataBlock(PartNumber++, new byte[0]);
                if (buf[0] != Constants.HeaderByte1 || buf[1] != Constants.HeaderByte2 || buf[2] != Constants.CompressionMethodDeflate)
                    throw new InvalidDataException("Archive is not valid or it was not created by this program.");

                var blockSize = BitConverter.ToInt32(buf, sizeof(int));
                buf = new byte[blockSize];

                SourceStream.Position -= Constants.HeaderSize;
                var offset = 0;
                do
                {
                    bytesRead = SourceStream.Read(buf, offset, buf.Length - offset);
                    offset += bytesRead;
                }
                while (offset != blockSize && bytesRead != 0);

                return new DataBlock(PartNumber++, buf);
            }
        }
    }
}
