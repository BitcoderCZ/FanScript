using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FanScript.Cli
{
    internal enum ErrorCode
    {
        None,
        UnknownError,
        CompilationErrors = 10,
        FileNotFound = 20,
        InvalidBuildPos,
    }
}
