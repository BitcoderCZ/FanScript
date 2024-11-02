using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FanScript.Utils
{
    public interface ITextWritable
    {
        void WriteTo(TextWriter writer);
    }
}
