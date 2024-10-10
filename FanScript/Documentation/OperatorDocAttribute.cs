using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FanScript.Documentation
{
    public abstract class OperatorDocAttribute : DocumentationAttribute
    {
        public OperatorDocAttribute(string name) : base(name)
        {
        }

        public string?[]? CombinationInfos { get; set; }
    }

    public sealed class BinaryOperatorDocAttribute : OperatorDocAttribute
    {
        public BinaryOperatorDocAttribute(string name) : base(name)
        {
        }
    }

    public sealed class UnaryOperatorDocAttribute : OperatorDocAttribute
    {
        public UnaryOperatorDocAttribute(string name) : base(name)
        {
        }
    }
}
