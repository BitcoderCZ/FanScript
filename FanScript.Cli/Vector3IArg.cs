using CommandLine;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FanScript.Cli
{
    public class Vector3IArg
    {
        public readonly int X;
        public readonly int Y;
        public readonly int Z;

        public Vector3IArg(string str)
        {
            string[] split = str.Split(',', 4);

            if (split.Length != 3)
                throw new ArgumentException("Vector must have 3 values.");

            if (!int.TryParse(split[0], CultureInfo.InvariantCulture, out X))
                throw new ArgumentException("Values must be valid integers, X is not.");

            if (!int.TryParse(split[1], CultureInfo.InvariantCulture, out Y))
                throw new ArgumentException("Values must be valid integers, Y is not.");

            if (!int.TryParse(split[2], CultureInfo.InvariantCulture, out Z))
                throw new ArgumentException("Values must be valid integers, Z is not.");
        }
    }
}
