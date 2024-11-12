using System;
using System.Text;

namespace FanScript.LangServer.Utils;

internal static class Extensions
{
    public static string Repeat(this string s, int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);

        StringBuilder builder = new StringBuilder(s.Length * count);
        for (int i = 0; i < count; i++)
            builder.Append(s);

        return builder.ToString();
    }
}
