using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FanScript.Documentation
{
    public abstract class OperatorDocAttribute : DocumentationAttribute
    {
        public OperatorDocAttribute() : base()
        {
        }

        public string?[]? CombinationInfos { get; set; }
    }

    public sealed class BinaryOperatorDocAttribute : OperatorDocAttribute
    {
        public BinaryOperatorDocAttribute() : base()
        {
        }
    }

    public sealed class UnaryOperatorDocAttribute : OperatorDocAttribute
    {
        public UnaryOperatorDocAttribute() : base()
        {
        }
    }
}
