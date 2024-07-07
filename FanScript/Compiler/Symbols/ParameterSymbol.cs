using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FanScript.Compiler.Symbols
{
    public sealed class ParameterSymbol : LocalVariableSymbol
    {
        internal ParameterSymbol(string name, TypeSymbol? type, int ordinal)
            : base(name, isReadOnly: true, type, null)
        {
            Ordinal = ordinal;
        }

        public override SymbolKind Kind => SymbolKind.Parameter;
        public int Ordinal { get; }
    }
}
