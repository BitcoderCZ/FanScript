using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace FanScript.Utils
{
    internal static class ImmutableArrayExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int LengthOrZero<T>(this ImmutableArray<T> array)
            => array.IsDefault ? 0 : array.Length;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddRangeSafe<T>(this ImmutableArray<T>.Builder builder, ImmutableArray<T> items)
        {
            if (!items.IsDefault)
                builder.AddRange(items);
        }
    }
}
