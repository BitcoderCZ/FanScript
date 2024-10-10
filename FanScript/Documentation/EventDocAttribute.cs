using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FanScript.Documentation
{
    public sealed class EventDocAttribute : DocumentationAttribute
    {
        public EventDocAttribute() : base()
        {
        }

        public string?[]? ParamInfos { get; set; }
    }
}
