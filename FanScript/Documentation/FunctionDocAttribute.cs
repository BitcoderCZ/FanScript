using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FanScript.Documentation
{
    public sealed class FunctionDocAttribute : DocumentationAttribute
    {
        public FunctionDocAttribute() : base()
        {
        }

        public string? ReturnValueInfo { get; set; }
        public string?[]? ParameterInfos { get; set; }
    }
}
