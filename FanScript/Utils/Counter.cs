// <copyright file="Counter.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using System.Numerics;

namespace FanScript.Utils;

internal readonly struct Counter
{
	public readonly ulong Value;

	private const int MaxValPerChar = 63;
	private const int Shift = 6;

	public Counter(ulong value)
	{
		Value = value;
	}

	public static implicit operator ulong(Counter c)
		=> c.Value;

	public static explicit operator Counter(ulong val)
		=> new Counter(val);

	public static Counter operator +(Counter a, Counter b)
		=> new Counter(a.Value + b.Value);

	public static Counter operator +(Counter a, ulong b)
		=> new Counter(a.Value + b);

	public static Counter operator +(Counter a, uint b)
		=> new Counter(a.Value + b);

	public static Counter operator -(Counter a, Counter b)
		=> new Counter(a.Value - b.Value);

	public static Counter operator -(Counter a, ulong b)
		=> new Counter(a.Value - b);

	public static Counter operator -(Counter a, uint b)
		=> new Counter(a.Value - b);

	public static Counter operator ++(Counter a)
		=> new Counter(a.Value + 1);

	public static Counter operator --(Counter a)
		=> new Counter(a.Value - 1);

	public override string ToString()
	{
		if (Value == 0)
		{
			return "0";
		}

		char[] chars = new char[(int)Math.Ceiling((64 - BitOperations.LeadingZeroCount(Value)) / (float)Shift)];

		ulong val = Value;
		int i = chars.Length - 1;
		do
		{
			ulong mod = val & MaxValPerChar;
			chars[i--] = Convert(mod);
			val >>= Shift;
		} while (val > 0);

		return new string(chars);
	}

	private static char Convert(ulong val)
		=> val switch
		{
			0 => '0',
			1 => '1',
			2 => '2',
			3 => '3',
			4 => '4',
			5 => '5',
			6 => '6',
			7 => '7',
			8 => '8',
			9 => '9',
			< 36 => (char)(val + 87),// a, b, c, ...
			62 => '(',
			63 => ')',
			_ => (char)(val + 29),// A, B, C, ...
		};
}
