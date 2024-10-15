using System.Numerics;

namespace FanScript.Utils
{
    internal readonly struct Counter
    {
        const int maxValPerChar = 63;
        const int shift = 6;

        public readonly ulong Value;

        public Counter(ulong value)
        {
            Value = value;
        }

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

        public static implicit operator ulong(Counter c)
            => c.Value;
        public static explicit operator Counter(ulong val)
            => new Counter(val);

        private char convert(ulong val)
        {
            switch (val)
            {
                case 0:
                    return '0';
                case 1:
                    return '1';
                case 2:
                    return '2';
                case 3:
                    return '3';
                case 4:
                    return '4';
                case 5:
                    return '5';
                case 6:
                    return '6';
                case 7:
                    return '7';
                case 8:
                    return '8';
                case 9:
                    return '9';
                case < 36:
                    return (char)(val + 87); // a, b, c, ...
                case 62:
                    return '(';
                case 63:
                    return ')';
                default:
                    return (char)(val + 29); // A, B, C, ...
            }
        }

        public override string ToString()
        {
            if (Value == 0)
                return "0";

            char[] chars = new char[(int)Math.Ceiling((float)(64 - BitOperations.LeadingZeroCount(Value)) / (float)shift)];

            ulong val = Value;
            int i = chars.Length - 1;
            do
            {
                ulong mod = val & maxValPerChar;
                chars[i--] = convert(mod);
                val >>= shift;
            } while (val > 0);

            return new string(chars);
        }
    }
}
