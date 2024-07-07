using FanScript.Compiler.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FanScript.Compiler.Diagnostics
{
    public sealed class Diagnostic
    {
        private Diagnostic(bool isError, TextLocation location, string message)
        {
            IsError = isError;
            Location = location;
            Message = message;
            IsWarning = !IsError;
        }

        public readonly bool IsError;
        public readonly TextLocation Location;
        public readonly string Message;
        public readonly bool IsWarning;

        public override string ToString() => Message;

        public static Diagnostic Error(TextLocation location, string message)
            => new Diagnostic(isError: true, location, message);

        public static Diagnostic Warning(TextLocation location, string message)
            => new Diagnostic(isError: false, location, message);
    }
}
