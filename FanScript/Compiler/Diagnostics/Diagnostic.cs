using FanScript.Compiler.Text;

namespace FanScript.Compiler.Diagnostics
{
    public sealed class Diagnostic
    {
        public readonly bool IsError;
        public readonly TextLocation Location;
        public readonly string Message;
        public readonly bool IsWarning;

        private Diagnostic(bool isError, TextLocation location, string message)
        {
            IsError = isError;
            Location = location;
            Message = message;
            IsWarning = !IsError;
        }

        public static Diagnostic Error(TextLocation location, string message)
            => new Diagnostic(isError: true, location, message);

        public static Diagnostic Warning(TextLocation location, string message)
            => new Diagnostic(isError: false, location, message);

        public override string ToString() => Message;
    }
}
