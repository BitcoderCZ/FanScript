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
            => wireType switch
            {
                WireType.Float => WireType.FloatPtr,
                WireType.Vec3 => WireType.Vec3Ptr,
                WireType.Rot => WireType.RotPtr,
                WireType.Bool => WireType.BoolPtr,
                WireType.Obj => WireType.ObjPtr,
                WireType.Con => WireType.ConPtr,
                _ => wireType,
            };

        public static WireType ToNormal(this WireType wireType)
            => wireType switch
            {
                WireType.FloatPtr => WireType.Float,
                WireType.Vec3Ptr => WireType.Vec3,
                WireType.RotPtr => WireType.Rot,
                WireType.BoolPtr => WireType.Bool,
                WireType.ObjPtr => WireType.Obj,
                WireType.ConPtr => WireType.Con,
                _ => wireType,
            };

        public static bool IsPointer(this WireType wireType)
            => wireType switch
            {
                WireType.FloatPtr => true,
                WireType.Vec3Ptr => true,
                WireType.RotPtr => true,
                WireType.BoolPtr => true,
                WireType.ObjPtr => true,
                WireType.ConPtr => true,
                _ => false
            };
    }
}
