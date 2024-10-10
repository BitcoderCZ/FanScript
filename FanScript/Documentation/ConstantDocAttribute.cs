using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FanScript.Documentation
{
    public sealed class ConstantDocAttribute : DocumentationAttribute
    {
        public ConstantDocAttribute(string name) : base(name)
        {
        }

        public string[]? ValueInfos { get; set; }
    }
}
