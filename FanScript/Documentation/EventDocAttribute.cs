using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FanScript.Documentation
{
    public sealed class EventDocAttribute : DocumentationAttribute
    {
        public EventDocAttribute(string name) : base(name)
        {
        }

        public string?[]? ParamInfos { get; set; }
    }
}
