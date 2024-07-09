using MathUtils.Vectors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FanScript.Compiler
{
    /// <summary>
    /// Wrapper for Vector3F, used in binding to distinguish between vec3 and rot
    /// </summary>
    internal sealed class Rotation
    {
        public readonly Vector3F Value;

        public Rotation(Vector3F value)
        {
            Value = value;
        }
    }
}
