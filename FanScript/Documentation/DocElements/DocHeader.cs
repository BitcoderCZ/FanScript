using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FanScript.Documentation.DocElements
{
    public sealed class DocHeader : DocElement
    {
        public DocHeader(ImmutableArray<DocArg> arguments, DocElement value, int level)
            : base(arguments, value)
        {
            Value = value;
            Level = level;
        }

        public override DocElement Value { get; }
        public int Level { get; }
    }
}
