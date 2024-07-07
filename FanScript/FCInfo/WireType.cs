using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FanScript.FCInfo
{
    // Ptr - Pointer
    public enum WireType : byte
    {
        Error = 0,
        Void = 1,
        Float = 2,
        FloatPtr = 3,
        Vec3 = 4,
        Vec3Ptr = 5,
        Rot = 6,
        RotPtr = 7,
        Bool = 8,
        BoolPtr = 9,
        Obj = 10,
        ObjPtr = 11,
        Con = 12,
        ConPtr = 12,
    }
}
