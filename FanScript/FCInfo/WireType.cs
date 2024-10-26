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
        ConPtr = 13,
    }

    public static class WireTypeE
    {
        public static WireType ToPointer(this WireType wireType)
            => wireType == WireType.Error ? wireType : (WireType)((int)wireType | 1);

        public static WireType ToNormal(this WireType wireType)
            => wireType == WireType.Void ? wireType : (WireType)((int)wireType & (int.MaxValue ^ 1));

        public static bool IsPointer(this WireType wireType)
            => wireType == WireType.Void ? false : ((int)wireType & 1) == 1;
    }
}
