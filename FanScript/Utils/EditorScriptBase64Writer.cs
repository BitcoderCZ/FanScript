﻿using System.Text;

namespace FanScript.Utils
{
    internal class EditorScriptBase64Writer : IDisposable
    {
        private Stream stream;

        public long Position { get => stream.Position; set => stream.Position = value; }

        public long Length => stream.Length;

        public EditorScriptBase64Writer(byte[] bytes)
        {
            stream = new MemoryStream(bytes);
            if (!stream.CanWrite)
                throw new Exception("Can't write to stream");
            Position = 0;
        }

        public EditorScriptBase64Writer(Stream stream)
        {
            this.stream = stream;
            if (!this.stream.CanWrite)
                throw new Exception("Can't write to stream");
            Position = 0;
        }

        public EditorScriptBase64Writer(string path)
        {
            stream = new FileStream(path, FileMode.Create, FileAccess.Write);
            if (!stream.CanWrite)
                throw new Exception("Can't write to stream");
            Position = 0;
        }

        public void Reset() => stream.Position = 0;

        public void WriteBytes(byte[] bytes) => WriteBytes(bytes, 0, bytes.Length);
        public void WriteBytes(byte[] bytes, int offset, int count)
        {
            stream.Write(bytes, offset, count);
        }

        public void WriteInt8(sbyte value)
        {
            WriteBytes(new byte[] { (byte)value });
        }

        public void WriteUInt8(byte value)
        {
            WriteBytes(new byte[] { value });
        }

        public void WriteInt16(Int16 value)
        {
            WriteBytes(BitConverter.GetBytes(value));
        }

        public void WriteUInt16(UInt16 value)
        {
            WriteBytes(BitConverter.GetBytes(value));
        }

        public void WriteInt32(Int32 value)
        {
            WriteBytes(BitConverter.GetBytes(value));
        }

        public void WriteUInt32(UInt32 value)
        {
            WriteBytes(BitConverter.GetBytes(value));
        }

        public void WriteInt64(Int64 value)
        {
            WriteBytes(BitConverter.GetBytes(value));
        }

        public void WriteUInt64(UInt64 value)
        {
            WriteBytes(BitConverter.GetBytes(value));
        }

        public void WriteSingle(Single value)
        {
            WriteBytes(BitConverter.GetBytes(value));
        }

        public void WriteString(string value)
        {
            if (value.Length > UInt16.MaxValue)
                throw new Exception($"Value(length:{value.Length}) is longer than {UInt16.MaxValue}");
            byte[] bytes = Encoding.UTF8.GetBytes(value);
            WriteInt32((UInt16)bytes.Length);
            WriteBytes(bytes);
        }

        public void Flush() => stream.Flush();

        public void Dispose()
        {
            stream.Close();
            stream.Dispose();
        }
    }
}
