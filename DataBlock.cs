﻿using System;

namespace ZipperVeeam
{
    internal class DataBlock
    {
        public int ID { get; }

        private byte[] _data;
        public byte[] Data { get => _data; set => _data = value;
        }

        public int Size => Data?.Length ?? 0;

        public DataBlock(int id, byte[] data)
        {
            ID = id;
            Data = data;
        }

        public void Resize(int newSize)
        {
            Array.Resize(ref _data, newSize);
        }

        public override string ToString() => $"ID: {ID}";

    }
}
