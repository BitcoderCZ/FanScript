using FanScript.Compiler.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
