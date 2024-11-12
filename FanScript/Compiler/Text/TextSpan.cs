using System.Diagnostics.CodeAnalysis;

namespace FanScript.Compiler.Text;

public readonly struct TextSpan
{
    public TextSpan(int start, int length)
    {
        Start = start;
        Length = length;
    }

    public int Start { get; }

    public int Length { get; }

    public int End => Start + Length;

    public static TextSpan operator +(TextSpan a, int b)
        => new TextSpan(a.Start + b, a.Length);

    public static TextSpan operator -(TextSpan a, int b)
        => new TextSpan(a.Start - b, a.Length);

    public static bool operator ==(TextSpan a, TextSpan b)
        => a.Start == b.Start && a.Length == b.Length;

    public static bool operator !=(TextSpan a, TextSpan b)
        => a.Start != b.Start || a.Length != b.Length;

    public static TextSpan FromBounds(int start, int end)
        => new TextSpan(start, end - start);

    public static TextSpan Combine(TextSpan va1, TextSpan va2)
        => FromBounds(
                Math.Min(va1.Start, va2.Start),
                Math.Max(va1.End, va2.End));

    public bool OverlapsWith(TextSpan span)
        => Start < span.End && End > span.Start;

    public override string ToString() => $"{Start}..{End}";

    public override int GetHashCode()
        => Start ^ Length;

    public override bool Equals([NotNullWhen(true)] object? obj)
        => obj is TextSpan other && this == other;
}
