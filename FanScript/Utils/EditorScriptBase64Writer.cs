// <copyright file="EditorScriptBase64Writer.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using System.Text;

namespace FanScript.Utils;

internal class EditorScriptBase64Writer : IDisposable
{
	private readonly Stream _stream;

	public EditorScriptBase64Writer(byte[] bytes)
	{
		_stream = new MemoryStream(bytes);
		if (!_stream.CanWrite)
		{
			throw new Exception("Can't write to stream");
		}

		Position = 0;
	}

	public EditorScriptBase64Writer(Stream stream)
	{
		_stream = stream;
		if (!_stream.CanWrite)
		{
			throw new Exception("Can't write to stream");
		}

		Position = 0;
	}

	public EditorScriptBase64Writer(string path)
	{
		_stream = new FileStream(path, FileMode.Create, FileAccess.Write);
		if (!_stream.CanWrite)
		{
			throw new Exception("Can't write to stream");
		}

		Position = 0;
	}

	public long Position { get => _stream.Position; set => _stream.Position = value; }

	public long Length => _stream.Length;

	public void Reset()
		=> _stream.Position = 0;

	public void WriteBytes(byte[] bytes)
		=> WriteBytes(bytes, 0, bytes.Length);

	public void WriteBytes(byte[] bytes, int offset, int count)
		=> _stream.Write(bytes, offset, count);

	public void WriteInt8(sbyte value)
		=> WriteBytes([(byte)value]);

	public void WriteUInt8(byte value)
		=> WriteBytes([value]);

	public void WriteInt16(short value)
		=> WriteBytes(BitConverter.GetBytes(value));

	public void WriteUInt16(ushort value)
		=> WriteBytes(BitConverter.GetBytes(value));

	public void WriteInt32(int value)
		=> WriteBytes(BitConverter.GetBytes(value));

	public void WriteUInt32(uint value)
		=> WriteBytes(BitConverter.GetBytes(value));

	public void WriteInt64(long value)
		=> WriteBytes(BitConverter.GetBytes(value));

	public void WriteUInt64(ulong value)
		=> WriteBytes(BitConverter.GetBytes(value));

	public void WriteSingle(float value)
		=> WriteBytes(BitConverter.GetBytes(value));

	public void WriteString(string value)
	{
		if (value.Length > ushort.MaxValue)
		{
			throw new Exception($"Value(length:{value.Length}) is longer than {ushort.MaxValue}");
		}

		byte[] bytes = Encoding.UTF8.GetBytes(value);
		WriteInt32((ushort)bytes.Length);
		WriteBytes(bytes);
	}

	public void Flush()
		=> _stream.Flush();

	public void Dispose()
	{
		_stream.Close();
		_stream.Dispose();
	}
}
