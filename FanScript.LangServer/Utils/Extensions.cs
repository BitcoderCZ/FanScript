using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FanScript.LangServer.Utils
{
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
}
