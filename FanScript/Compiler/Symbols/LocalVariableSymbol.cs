using FanScript.Compiler.Binding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FanScript.Compiler.Symbols
{
    public class LocalVariableSymbol : VariableSymbol
    {
        internal LocalVariableSymbol(string name, bool isReadOnly, TypeSymbol? type, BoundConstant? constant)
            : base(name, isReadOnly, type, constant)
        {
        }

        public override SymbolKind Kind => SymbolKind.LocalVariable;
    }
}
