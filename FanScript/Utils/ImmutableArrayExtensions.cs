using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace FanScript.Utils;

internal static class ImmutableArrayExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int LengthOrZero<T>(this ImmutableArray<T> array)
        => array.IsDefault ? 0 : array.Length;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AddRangeSafe<T>(this ImmutableArray<T>.Builder builder, ImmutableArray<T> items)
    {
        if (!items.IsDefault)
        {
            builder.AddRange(items);
        }
    }
}
