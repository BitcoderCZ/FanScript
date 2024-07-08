using FanScript.Compiler.Diagnostics;
using System.Collections.Immutable;

namespace FanScript.Compiler
{
    public static class DiagnosticExtensions
    {
        public static bool HasErrors(this ImmutableArray<Diagnostic> diagnostics)
            => diagnostics.Any(d => d.IsError);

        public static bool HasErrors(this IEnumerable<Diagnostic> diagnostics)
            => diagnostics.Any(d => d.IsError);
    }
}
