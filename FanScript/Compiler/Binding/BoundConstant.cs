using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FanScript.Compiler.Binding
{
    internal sealed class BoundConstant
    {
        public BoundConstant(object value)
        {
            Value = value;
        }

        public object Value { get; }
    }
}
