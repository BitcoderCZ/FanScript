using FanScript.Compiler.Symbols;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FanScript.Compiler
{
    internal class Constant
    {
        public readonly TypeSymbol Type;
        public readonly string Name;
        public readonly object Value;

        public Constant(TypeSymbol type, string name, object value)
        {
            Type = type;
            Name = name;
            Value = value;
        }
    }

    internal static class Constants
    {
        public static readonly Constant BUTTON_TYPE_DIRECTION = new Constant(TypeSymbol.Float, "BUTTON_TYPE_DIRECTION", 0f);
        public static readonly Constant BUTTON_TYPE_BUTTON = new Constant(TypeSymbol.Float, "BUTTON_TYPE_BUTTON", 1.00000012f); // just '1f' doesn't work with GameFileCodeBuilder for some reason, ig float imprecision and rounding down

        private static IEnumerable<Constant>? cache;
        public static IEnumerable<Constant> GetAll()
            => cache ??= typeof(Constants)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Select(field => field.GetValue(null) as Constant)
            .Where(val => val is Constant)
            .Select(constant => constant!);
    }
}
