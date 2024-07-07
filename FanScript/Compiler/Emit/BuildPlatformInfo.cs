using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FanScript.Compiler.Emit
{
    [Flags]
    public enum BuildPlatformInfo : byte
    {
        CanGetBlocks = 0b_0000_0001,
        CanCreateCustomBlocks = 0b_0000_0010,
    }
}
