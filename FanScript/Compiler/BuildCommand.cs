using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FanScript.Compiler
{
    public enum BuildCommand
    {
        Highlight,
        EndHighlight,
    }

    public static class BuildCommandE
    {
        private static Dictionary<string, BuildCommand>? _commandByName;
        private static Dictionary<string, BuildCommand> commandByName => _commandByName ??= Enum.GetNames<BuildCommand>()
            .Zip(Enum.GetValues<BuildCommand>())
            .ToDictionary(item => item.First.ToLowerInvariant(), item => item.Second);

        public static BuildCommand? Parse(string str)
        {
            if (commandByName.TryGetValue(str, out var command))
                return command;
            else
                return null;
        }
    }
}
