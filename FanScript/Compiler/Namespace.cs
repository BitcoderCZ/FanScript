using FanScript.Utils;
using System.Diagnostics.CodeAnalysis;

namespace FanScript.Compiler
{
    public readonly struct Namespace
    {
        public readonly string Value;
        public readonly int Length;

        public Namespace(ReadOnlySpan<char> value)
        {
            if (value.IsEmpty)
                throw new ArgumentNullException(nameof(value));
            else if (value.Contains(" ".AsSpan(), StringComparison.Ordinal))
                throw new ArgumentException($"{nameof(value)} contains invalid characters.");

            Value = new string(value.Trim('/')).ToLowerInvariant();
            Length = Value.AsSpan().Count('/') + 1;
        }

        /// <summary>
        /// Only use with validated values!
        /// </summary>
        /// <param name="value"></param>
        private Namespace(string value)
        {
            Value = value;
        }

        public Namespace Slice(int index)
            => Slice(index, Length - index);
        public Namespace Slice(int index, int length)
        {
            if (index == 0 && length == Length)
                return this;
            else if (length <= 0)
                throw new ArgumentOutOfRangeException(nameof(length));

            ReadOnlySpan<char> val = Value.AsSpan();

            int startIndex = val.IndexOf('/', index);
            if (startIndex == -1)
                throw new ArgumentOutOfRangeException(nameof(index));

            val = val.Slice(startIndex + 1);

            int endIndex = val.IndexOf('/', length - 1);
            if (endIndex == -1)
                throw new ArgumentOutOfRangeException(nameof(index));

            return new Namespace(new string(val.Slice(0, endIndex)));
        }

        public Namespace CapitalizeFirst()
        {
            string[] split = Value.Split('/');

            for (int i = 0; i < split.Length; i++)
                split[i] = split[i].ToUpperFirst();

            return new Namespace(string.Join('/', split));
        }

        public static Namespace operator +(Namespace a, string b)
            => new Namespace(a.Value + "/" + b.ToLowerInvariant());
        public static Namespace operator +(Namespace a, Namespace b)
            => new Namespace(a.Value + "/" + b);

        public static bool operator ==(Namespace a, Namespace b)
            => a.Value == b.Value;
        public static bool operator !=(Namespace a, Namespace b)
            => a.Value != b.Value;

        public override int GetHashCode()
            => Value.GetHashCode();

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj is Namespace @namespace)
                return @namespace == this;
            else
                return false;
        }

        public override string ToString()
            => Value;
    }
}
