using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FanScript.Documentation
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Enum, AllowMultiple = true, Inherited = true)]
    public abstract class DocumentationAttribute : Attribute
    {
        protected DocumentationAttribute()
        {
        }

        public string? NameOverwrite { get; set; }
        public string? Info { get; set; }

        public string[]? Remarks { get; set; }
        public string? Examples { get; set; }
        public string[]? Related { get; set; }
    }
}
