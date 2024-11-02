using FanScript.Documentation.Attributes;

namespace FanScript.Compiler
{
    public enum BuildCommand
    {
        [BuildCommandDoc(
            Info = """
            Starts block highlight, all blocks between this and <link type="build_command">endHighlight</> will get placed at the front of the level.
            """,
            Related = [
                """
                <link type="build_command">endHighlight</>
                """
            ])]
        Highlight,
        [BuildCommandDoc(
            Info = """
            Ends block highlight, started by <link type="build_command">highlight</>.
            """,
            Related = [
                """
                <link type="build_command">highlight</>
                """
            ])]
        EndHighlight,
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1649:File name should match first type name", Justification = "Not sure why it doesn't, BuildCommand is clearly first")]
    public static class BuildCommandE
    {
        private static Dictionary<string, BuildCommand>? _commandByName;

        private static Dictionary<string, BuildCommand> CommandByName => _commandByName ??= Enum.GetNames<BuildCommand>()
            .Zip(Enum.GetValues<BuildCommand>())
            .ToDictionary(item => item.First.ToLowerInvariant(), item => item.Second);

        public static BuildCommand? Parse(string str)
            => CommandByName.TryGetValue(str, out var command) ? command : null;
    }
}
