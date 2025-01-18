using System.Globalization;

namespace FanScript.Cli;

public class int3Arg
{
	public readonly int X;
	public readonly int Y;
	public readonly int Z;

	public int3Arg(string str)
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

	public override string ToString()
		=> $"{X},{Y},{Z}";
}
