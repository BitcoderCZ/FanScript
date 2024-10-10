using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FanScript.Documentation
{
    public sealed class TypeDocAttribute : DocumentationAttribute
    {
        public TypeDocAttribute(string name) : base(name)
        {
        }

        public string? HowToCreate { get; set; }
    }
}
