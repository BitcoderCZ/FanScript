using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FanScript.Compiler.Symbols
{
    public abstract class Symbol
    {
        private protected Symbol(string name)
        {
            Name = name;
        }

        public abstract SymbolKind Kind { get; }
        public string Name { get; }

        public void WriteTo(TextWriter writer)
            => SymbolPrinter.WriteTo(this, writer);

        public override string ToString()
        {
            using (var writer = new StringWriter())
            {
                WriteTo(writer);
                return writer.ToString();
            }
        }
    }
}
