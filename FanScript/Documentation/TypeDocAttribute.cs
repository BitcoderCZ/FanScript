using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FanScript.Documentation
{
    public sealed class TypeDocAttribute : DocumentationAttribute
    {
        public TypeDocAttribute() : base()
        {
        }

        public string? HowToCreate { get; set; }
    }
}
