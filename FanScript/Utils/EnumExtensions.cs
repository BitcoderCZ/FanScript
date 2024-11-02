namespace FanScript.Utils
{
    internal static class EnumExtensions
    {
        public static bool HasOneOfFlags<T>(this T value, params T[] flags)
            where T : Enum
        {
            for (int i = 0; i < flags.Length; i++)
            {
                if (value.HasFlag(flags[i]))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
