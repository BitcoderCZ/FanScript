using FanScript.Compiler;
using FanScript.Compiler.Exceptions;
using MathUtils.Vectors;

namespace FanScript.FCInfo
{
    public static class Blocks
    {
        public static BlockDef Stone = new BlockDef("Stone Block", 1, BlockType.NonScript, new Vector2I(1, 1));
        public static BlockDef Shrub = new BlockDef("Shrub", 500, BlockType.NonScript, new Vector2I(1, 1));

        public static (BlockDef Def, Terminal In, Terminal Out)? GetPassthrough(WireType type)
        {
            switch (type)
            {
                case WireType.Bool:
                    return (Math.LogicalOr, Math.LogicalOr.Terminals["Tru1"], Math.LogicalOr.Terminals["Tru1 | Tru2"]);
                case WireType.Float:
                    return (Math.Add_Number, Math.Add_Number.Terminals["Num1"], Math.Add_Number.Terminals["Num1 + Num2"]);
                case WireType.Vec3:
                    return (Math.Add_Vector, Math.Add_Vector.Terminals["Vec1"], Math.Add_Vector.Terminals["Vec1 + Vec2"]);
                case WireType.Rot:
                    return (Math.Multiply_Rotation, Math.Multiply_Rotation.Terminals["Rot1"], Math.Multiply_Rotation.Terminals["Rot1 * Rot2"]);
                default:
                    {
                        if (type.IsPointer())
                        {
                            BlockDef def = Variables.ListByType(type);
                            return (def, def.Terminals["Variable"], def.Terminals["Element"]);
                        }

                        return null;
                    }
            }
        }

        public static class Game
        {
            public static readonly BlockDef Win = new BlockDef("Win", 252, BlockType.Active, new Vector2I(2, 2), new Terminal(WireType.Void, TerminalType.Out), new Terminal(WireType.Void, TerminalType.In));
            public static readonly BlockDef Lose = new BlockDef("Lose", 256, BlockType.Active, new Vector2I(2, 2), new Terminal(WireType.Void, TerminalType.Out), new Terminal(WireType.Void, TerminalType.In));
            public static readonly BlockDef SetScore = new BlockDef("Set Score", 260, BlockType.Active, new Vector2I(2, 2), new Terminal(WireType.Void, TerminalType.Out), new Terminal(WireType.Float, TerminalType.In, "Coins"), new Terminal(WireType.Float, TerminalType.In, "Score"), new Terminal(WireType.Void, TerminalType.In));
            public static readonly BlockDef SetCamera = new BlockDef("Set Camera", 268, BlockType.Active, new Vector2I(2, 3), new Terminal(WireType.Void, TerminalType.Out), new Terminal(WireType.Float, TerminalType.In, "Range"), new Terminal(WireType.Rot, TerminalType.In, "Rotation"), new Terminal(WireType.Vec3, TerminalType.In, "Position"), new Terminal(WireType.Void, TerminalType.In));
            public static readonly BlockDef SetLight = new BlockDef("Set Light", 274, BlockType.Active, new Vector2I(2, 2), new Terminal(WireType.Void, TerminalType.Out), new Terminal(WireType.Rot, TerminalType.In, "Rotation"), new Terminal(WireType.Vec3, TerminalType.In, "Position"), new Terminal(WireType.Void, TerminalType.In));
            public static readonly BlockDef ScreenSize = new BlockDef("Screen Size", 220, BlockType.Pasive, new Vector2I(2, 2), new Terminal(WireType.Float, TerminalType.Out, "Height"), new Terminal(WireType.Float, TerminalType.Out, "Width"));
            public static readonly BlockDef Accelerometer = new BlockDef("Accelerometer", 224, BlockType.Pasive, new Vector2I(2, 2), new Terminal(WireType.Vec3, TerminalType.Out, "Direction"));
            public static readonly BlockDef CurrentFrame = new BlockDef("Current Frame", 564, BlockType.Value, new Vector2I(2, 1), new Terminal(WireType.Float, TerminalType.Out, "Counter"));
            public static readonly BlockDef MenuItem = new BlockDef("Menu Item", 584, BlockType.Active, new Vector2I(2, 2), new Terminal(WireType.Void, TerminalType.Out), new Terminal(WireType.Obj, TerminalType.In, "Picture"), new Terminal(WireType.FloatPtr, TerminalType.In, "Variable"), new Terminal(WireType.Void, TerminalType.In));
        }

        public static class Objects
        {
            public static readonly BlockDef GetPos = new BlockDef("Get Position", 278, BlockType.Pasive, new Vector2I(2, 2), new Terminal(WireType.Rot, TerminalType.Out, "Rotation"), new Terminal(WireType.Vec3, TerminalType.Out, "Position"), new Terminal(WireType.Obj, TerminalType.In, "Object"));
            public static readonly BlockDef SetPos = new BlockDef("Set Position", 282, BlockType.Active, new Vector2I(2, 3), new Terminal(WireType.Void, TerminalType.Out), new Terminal(WireType.Rot, TerminalType.In, "Rotation"), new Terminal(WireType.Vec3, TerminalType.In, "Position"), new Terminal(WireType.Obj, TerminalType.In, "Object"), new Terminal(WireType.Void, TerminalType.In));
            public static readonly BlockDef Raycast = new BlockDef("Raycast", 228, BlockType.Pasive, new Vector2I(2, 3), new Terminal(WireType.Obj, TerminalType.Out, "Hit Obj"), new Terminal(WireType.Vec3, TerminalType.Out, "Hit Pos"), new Terminal(WireType.Bool, TerminalType.Out, "Hit?"), new Terminal(WireType.Vec3, TerminalType.In, "To"), new Terminal(WireType.Vec3, TerminalType.In, "From"));
            public static readonly BlockDef GetSize = new BlockDef("Get Size", 489, BlockType.Pasive, new Vector2I(2, 2), new Terminal(WireType.Vec3, TerminalType.Out, "Max"), new Terminal(WireType.Vec3, TerminalType.Out, "Min"), new Terminal(WireType.Obj, TerminalType.In, "Object"));
            public static readonly BlockDef SetVisible = new BlockDef("Set Visible", 306, BlockType.Active, new Vector2I(2, 2), new Terminal(WireType.Void, TerminalType.Out), new Terminal(WireType.Bool, TerminalType.In, "Visible"), new Terminal(WireType.Obj, TerminalType.In, "Object"), new Terminal(WireType.Void, TerminalType.In));
            public static readonly BlockDef CreateObject = new BlockDef("Create Object", 316, BlockType.Active, new Vector2I(2, 2), new Terminal(WireType.Void, TerminalType.Out), new Terminal(WireType.Obj, TerminalType.Out, "Copy"), new Terminal(WireType.Obj, TerminalType.In, "Object"), new Terminal(WireType.Void, TerminalType.In));
            public static readonly BlockDef DestroyObject = new BlockDef("Destroy Object", 320, BlockType.Active, new Vector2I(2, 2), new Terminal(WireType.Void, TerminalType.Out), new Terminal(WireType.Obj, TerminalType.In, "Object"), new Terminal(WireType.Void, TerminalType.In));
        }

        public static class Sound
        {
            public static readonly BlockDef PlaySound = new BlockDef("Play Sound", 264, BlockType.Active, new Vector2I(2, 2), new Terminal(WireType.Void, TerminalType.Out), new Terminal(WireType.Float, TerminalType.Out, "Channel"), new Terminal(WireType.Float, TerminalType.In, "Pitch"), new Terminal(WireType.Float, TerminalType.In, "Volume"), new Terminal(WireType.Void, TerminalType.In));
            public static readonly BlockDef StopSound = new BlockDef("Stop Sound", 397, BlockType.Active, new Vector2I(2, 2), new Terminal(WireType.Void, TerminalType.Out), new Terminal(WireType.Float, TerminalType.In, "Channel"), new Terminal(WireType.Void, TerminalType.In));
            public static readonly BlockDef VolumePitch = new BlockDef("VolumePitch", 391, BlockType.Active, new Vector2I(2, 3), new Terminal(WireType.Void, TerminalType.Out), new Terminal(WireType.Float, TerminalType.In, "Pitch"), new Terminal(WireType.Float, TerminalType.In, "Volume"), new Terminal(WireType.Float, TerminalType.In, "Channel"), new Terminal(WireType.Void, TerminalType.In));
        }

        public static class Physics
        {
            public static readonly BlockDef AddForce = new BlockDef("Add Force", 298, BlockType.Active, new Vector2I(2, 4), new Terminal(WireType.Void, TerminalType.Out), new Terminal(WireType.Vec3, TerminalType.In, "Torque"), new Terminal(WireType.Vec3, TerminalType.In, "Apply at"), new Terminal(WireType.Vec3, TerminalType.In, "Force"), new Terminal(WireType.Obj, TerminalType.In, "Object"), new Terminal(WireType.Void, TerminalType.In));
            public static readonly BlockDef GetVelocity = new BlockDef("Get Velocity", 288, BlockType.Pasive, new Vector2I(2, 2), new Terminal(WireType.Vec3, TerminalType.Out, "Spin"), new Terminal(WireType.Vec3, TerminalType.Out, "Velocity"), new Terminal(WireType.Obj, TerminalType.In, "Object"));
            public static readonly BlockDef SetVelocity = new BlockDef("Set Velocity", 292, BlockType.Active, new Vector2I(2, 3), new Terminal(WireType.Void, TerminalType.Out), new Terminal(WireType.Vec3, TerminalType.In, "Spin"), new Terminal(WireType.Vec3, TerminalType.In, "Velocity"), new Terminal(WireType.Obj, TerminalType.In, "Object"), new Terminal(WireType.Void, TerminalType.In));
            public static readonly BlockDef SetLocked = new BlockDef("Set Locked", 310, BlockType.Active, new Vector2I(2, 3), new Terminal(WireType.Void, TerminalType.Out), new Terminal(WireType.Vec3, TerminalType.In, "Rotation"), new Terminal(WireType.Vec3, TerminalType.In, "Position"), new Terminal(WireType.Obj, TerminalType.In, "Object"), new Terminal(WireType.Void, TerminalType.In));
            public static readonly BlockDef SetMass = new BlockDef("Set Mass", 328, BlockType.Active, new Vector2I(2, 2), new Terminal(WireType.Void, TerminalType.Out), new Terminal(WireType.Float, TerminalType.In, "Mass"), new Terminal(WireType.Obj, TerminalType.In, "Object"), new Terminal(WireType.Void, TerminalType.In));
            public static readonly BlockDef SetFriction = new BlockDef("Set Friction", 332, BlockType.Active, new Vector2I(2, 2), new Terminal(WireType.Void, TerminalType.Out), new Terminal(WireType.Float, TerminalType.In, "Friction"), new Terminal(WireType.Obj, TerminalType.In, "Object"), new Terminal(WireType.Void, TerminalType.In));
            public static readonly BlockDef SetBounciness = new BlockDef("Set Bounciness", 336, BlockType.Active, new Vector2I(2, 2), new Terminal(WireType.Void, TerminalType.Out), new Terminal(WireType.Float, TerminalType.In, "Bounciness"), new Terminal(WireType.Obj, TerminalType.In, "Object"), new Terminal(WireType.Void, TerminalType.In));
            public static readonly BlockDef SetGravity = new BlockDef("Set Gravity", 324, BlockType.Active, new Vector2I(2, 2), new Terminal(WireType.Void, TerminalType.Out), new Terminal(WireType.Vec3, TerminalType.In, "Gravity"), new Terminal(WireType.Void, TerminalType.In));
            public static readonly BlockDef AddConstraint = new BlockDef("Add Constraint", 340, BlockType.Active, new Vector2I(2, 3), new Terminal(WireType.Void, TerminalType.Out), new Terminal(WireType.Con, TerminalType.Out, "Constraint"), new Terminal(WireType.Vec3, TerminalType.In, "Pivot"), new Terminal(WireType.Obj, TerminalType.In, "Part"), new Terminal(WireType.Obj, TerminalType.In, "Base"), new Terminal(WireType.Void, TerminalType.In));
            public static readonly BlockDef LinearLimits = new BlockDef("Linear Limits", 346, BlockType.Active, new Vector2I(2, 3), new Terminal(WireType.Void, TerminalType.Out), new Terminal(WireType.Vec3, TerminalType.In, "Upper"), new Terminal(WireType.Vec3, TerminalType.In, "Lower"), new Terminal(WireType.Con, TerminalType.In, "Constraint"), new Terminal(WireType.Void, TerminalType.In));
            public static readonly BlockDef AngularLimits = new BlockDef("Angular Limits", 352, BlockType.Active, new Vector2I(2, 3), new Terminal(WireType.Void, TerminalType.Out), new Terminal(WireType.Vec3, TerminalType.In, "Upper"), new Terminal(WireType.Vec3, TerminalType.In, "Lower"), new Terminal(WireType.Con, TerminalType.In, "Constraint"), new Terminal(WireType.Void, TerminalType.In));
            public static readonly BlockDef LinearSpring = new BlockDef("Linear Spring", 358, BlockType.Active, new Vector2I(2, 3), new Terminal(WireType.Void, TerminalType.Out), new Terminal(WireType.Vec3, TerminalType.In, "Damping"), new Terminal(WireType.Vec3, TerminalType.In, "Stiffness"), new Terminal(WireType.Con, TerminalType.In, "Constraint"), new Terminal(WireType.Void, TerminalType.In));
            public static readonly BlockDef AngularSpring = new BlockDef("Angular Spring", 364, BlockType.Active, new Vector2I(2, 3), new Terminal(WireType.Void, TerminalType.Out), new Terminal(WireType.Vec3, TerminalType.In, "Damping"), new Terminal(WireType.Vec3, TerminalType.In, "Stiffness"), new Terminal(WireType.Con, TerminalType.In, "Constraint"), new Terminal(WireType.Void, TerminalType.In));
            public static readonly BlockDef LinearMotor = new BlockDef("Linear Motor", 370, BlockType.Active, new Vector2I(2, 3), new Terminal(WireType.Void, TerminalType.Out), new Terminal(WireType.Vec3, TerminalType.In, "Force"), new Terminal(WireType.Vec3, TerminalType.In, "Speed"), new Terminal(WireType.Con, TerminalType.In, "Constraint"), new Terminal(WireType.Void, TerminalType.In));
            public static readonly BlockDef AngularMotor = new BlockDef("Angular Motor", 376, BlockType.Active, new Vector2I(2, 3), new Terminal(WireType.Void, TerminalType.Out), new Terminal(WireType.Vec3, TerminalType.In, "Force"), new Terminal(WireType.Vec3, TerminalType.In, "Speed"), new Terminal(WireType.Con, TerminalType.In, "Constraint"), new Terminal(WireType.Void, TerminalType.In));
        }

        public static class Control
        {
            public static readonly BlockDef If = new BlockDef("If", 234, BlockType.Active, new Vector2I(2, 2), new Terminal(WireType.Void, TerminalType.Out), new Terminal(WireType.Void, TerminalType.Out, "False"), new Terminal(WireType.Void, TerminalType.Out, "True"), new Terminal(WireType.Bool, TerminalType.In, "Condition"), new Terminal(WireType.Void, TerminalType.In));
            public static readonly BlockDef PlaySensor = new BlockDef("Play Sensor", 238, BlockType.Active, new Vector2I(2, 2), new Terminal(WireType.Void, TerminalType.Out), new Terminal(WireType.Void, TerminalType.Out, "On Play"), new Terminal(WireType.Void, TerminalType.In));
            public static readonly BlockDef LateUpdate = new BlockDef("Late Update", 566, BlockType.Active, new Vector2I(2, 2), new Terminal(WireType.Void, TerminalType.Out), new Terminal(WireType.Void, TerminalType.Out, "After Physics"), new Terminal(WireType.Void, TerminalType.In));
            public static readonly BlockDef BoxArtSensor = new BlockDef("Box Art Sensor", 409, BlockType.Active, new Vector2I(2, 2), new Terminal(WireType.Void, TerminalType.Out), new Terminal(WireType.Void, TerminalType.Out, "On Screenshot"), new Terminal(WireType.Void, TerminalType.In));
            public static readonly BlockDef TouchSensor = new BlockDef("Touch Sensor", 242, BlockType.Active, new Vector2I(2, 3), new Terminal(WireType.Void, TerminalType.Out), new Terminal(WireType.Float, TerminalType.Out, "Screen Y"), new Terminal(WireType.Float, TerminalType.Out, "Screen X"), new Terminal(WireType.Void, TerminalType.Out, "Touched"), new Terminal(WireType.Void, TerminalType.In));
            public static readonly BlockDef SwipeSensor = new BlockDef("Swipe Sensor", 248, BlockType.Active, new Vector2I(2, 2), new Terminal(WireType.Void, TerminalType.Out), new Terminal(WireType.Vec3, TerminalType.Out, "Direction"), new Terminal(WireType.Void, TerminalType.Out, "Swiped"), new Terminal(WireType.Void, TerminalType.In));
            public static readonly BlockDef Button = new BlockDef("Button", 588, BlockType.Active, new Vector2I(2, 2), new Terminal(WireType.Void, TerminalType.Out), new Terminal(WireType.Void, TerminalType.Out, "Button"), new Terminal(WireType.Void, TerminalType.In));
            public static readonly BlockDef Joystick = new BlockDef("Joystick", 592, BlockType.Active, new Vector2I(2, 2), new Terminal(WireType.Void, TerminalType.Out), new Terminal(WireType.Vec3, TerminalType.Out, "Joy Dir"), new Terminal(WireType.Void, TerminalType.In));
            public static readonly BlockDef Collision = new BlockDef("Collision", 401, BlockType.Active, new Vector2I(2, 4), new Terminal(WireType.Void, TerminalType.Out), new Terminal(WireType.Vec3, TerminalType.Out, "Normal"), new Terminal(WireType.Float, TerminalType.Out, "Impulse"), new Terminal(WireType.Obj, TerminalType.Out, "2nd Object"), new Terminal(WireType.Void, TerminalType.Out, "Collided"), new Terminal(WireType.Obj, TerminalType.In, "1st Object"), new Terminal(WireType.Void, TerminalType.In));
            public static readonly BlockDef Loop = new BlockDef("Loop", 560, BlockType.Active, new Vector2I(2, 2), new Terminal(WireType.Void, TerminalType.Out), new Terminal(WireType.Float, TerminalType.Out, "Counter"), new Terminal(WireType.Void, TerminalType.Out, "Do"), new Terminal(WireType.Float, TerminalType.In, "Stop"), new Terminal(WireType.Float, TerminalType.In, "Start"), new Terminal(WireType.Void, TerminalType.In));
        }

        public static class Math
        {
#pragma warning disable SA1310 // Field names should not contain underscore
            public static readonly BlockDef Negate = new BlockDef("Negate", 90, BlockType.Pasive, new Vector2I(2, 1), new Terminal(WireType.Float, TerminalType.Out, "-Num"), new Terminal(WireType.Float, TerminalType.In, "Num"));
#pragma warning restore SA1310
            public static readonly BlockDef Not = new BlockDef("Not", 144, BlockType.Pasive, new Vector2I(2, 1), new Terminal(WireType.Bool, TerminalType.Out, "Not Tru"), new Terminal(WireType.Bool, TerminalType.In, "Tru"));
            public static readonly BlockDef Inverse = new BlockDef("Inverse", 440, BlockType.Pasive, new Vector2I(2, 1), new Terminal(WireType.Rot, TerminalType.Out, "Rot Inverse"), new Terminal(WireType.Rot, TerminalType.In, "Rot"));

#pragma warning disable SA1310 // Field names should not contain underscore
            public static readonly BlockDef Add_Number = new BlockDef("Add Numbers", 92, BlockType.Pasive, new Vector2I(2, 2), new Terminal(WireType.Float, TerminalType.Out, "Num1 + Num2"), new Terminal(WireType.Float, TerminalType.In, "Num2"), new Terminal(WireType.Float, TerminalType.In, "Num1"));
            public static readonly BlockDef Add_Vector = new BlockDef("Add Vectors", 96, BlockType.Pasive, new Vector2I(2, 2), new Terminal(WireType.Vec3, TerminalType.Out, "Vec1 + Vec2"), new Terminal(WireType.Vec3, TerminalType.In, "Vec2"), new Terminal(WireType.Vec3, TerminalType.In, "Vec1"));
            public static readonly BlockDef Subtract_Number = new BlockDef("Subtract Numbers", 100, BlockType.Pasive, new Vector2I(2, 2), new Terminal(WireType.Float, TerminalType.Out, "Num1 - Num2"), new Terminal(WireType.Float, TerminalType.In, "Num2"), new Terminal(WireType.Float, TerminalType.In, "Num1"));
            public static readonly BlockDef Subtract_Vector = new BlockDef("Subtract Vectors", 104, BlockType.Pasive, new Vector2I(2, 2), new Terminal(WireType.Vec3, TerminalType.Out, "Vec1 - Vec2"), new Terminal(WireType.Vec3, TerminalType.In, "Vec2"), new Terminal(WireType.Vec3, TerminalType.In, "Vec1"));
            public static readonly BlockDef Multiply_Number = new BlockDef("Multiply", 108, BlockType.Pasive, new Vector2I(2, 2), new Terminal(WireType.Float, TerminalType.Out, "Num1 * Num2"), new Terminal(WireType.Float, TerminalType.In, "Num2"), new Terminal(WireType.Float, TerminalType.In, "Num1"));
            public static readonly BlockDef Multiply_Vector = new BlockDef("Scale", 112, BlockType.Pasive, new Vector2I(2, 2), new Terminal(WireType.Vec3, TerminalType.Out, "Vec * Num"), new Terminal(WireType.Float, TerminalType.In, "Num"), new Terminal(WireType.Vec3, TerminalType.In, "Vec"));
            public static readonly BlockDef Rotate_Vector = new BlockDef("Rotate", 116, BlockType.Pasive, new Vector2I(2, 2), new Terminal(WireType.Vec3, TerminalType.Out, "Rot * Vec"), new Terminal(WireType.Rot, TerminalType.In, "Rot"), new Terminal(WireType.Vec3, TerminalType.In, "Vec"));
            public static readonly BlockDef Multiply_Rotation = new BlockDef("Combine", 120, BlockType.Pasive, new Vector2I(2, 2), new Terminal(WireType.Rot, TerminalType.Out, "Rot1 * Rot2"), new Terminal(WireType.Rot, TerminalType.In, "Rot2"), new Terminal(WireType.Rot, TerminalType.In, "Rot1"));
            public static readonly BlockDef Divide_Number = new BlockDef("Divide", 124, BlockType.Pasive, new Vector2I(2, 2), new Terminal(WireType.Float, TerminalType.Out, "Num1 + Num2"), new Terminal(WireType.Float, TerminalType.In, "Num2"), new Terminal(WireType.Float, TerminalType.In, "Num1"));
            public static readonly BlockDef Modulo_Number = new BlockDef("Modulo", 172, BlockType.Pasive, new Vector2I(2, 2), new Terminal(WireType.Float, TerminalType.Out, "mod(a,b)"), new Terminal(WireType.Float, TerminalType.In, "b"), new Terminal(WireType.Float, TerminalType.In, "a"));
            public static readonly BlockDef Power = new BlockDef("Power", 457, BlockType.Pasive, new Vector2I(2, 2), new Terminal(WireType.Float, TerminalType.Out, "Base ^ Exponent"), new Terminal(WireType.Float, TerminalType.In, "Exponent"), new Terminal(WireType.Float, TerminalType.In, "Base"));

            public static readonly BlockDef Equals_Number = new BlockDef("Equals Numbers", 132, BlockType.Pasive, new Vector2I(2, 2), new Terminal(WireType.Bool, TerminalType.Out, "Num1 = Num2"), new Terminal(WireType.Float, TerminalType.In, "Num2"), new Terminal(WireType.Float, TerminalType.In, "Num1"));
            public static readonly BlockDef Equals_Vector = new BlockDef("Equals Vectors", 136, BlockType.Pasive, new Vector2I(2, 2), new Terminal(WireType.Bool, TerminalType.Out, "Vec1 = Vec2"), new Terminal(WireType.Vec3, TerminalType.In, "Vec2"), new Terminal(WireType.Vec3, TerminalType.In, "Vec1"));
            public static readonly BlockDef Equals_Object = new BlockDef("Equals Objects", 140, BlockType.Pasive, new Vector2I(2, 2), new Terminal(WireType.Bool, TerminalType.Out, "Obj1 = Obj2"), new Terminal(WireType.Obj, TerminalType.In, "Obj2"), new Terminal(WireType.Obj, TerminalType.In, "Obj1"));
            public static readonly BlockDef Equals_Bool = new BlockDef("Equals Truths", 421, BlockType.Pasive, new Vector2I(2, 2), new Terminal(WireType.Bool, TerminalType.Out, "Tru1 = Tru2"), new Terminal(WireType.Bool, TerminalType.In, "Tru2"), new Terminal(WireType.Bool, TerminalType.In, "Tru1"));
#pragma warning restore SA1310 // Field names should not contain underscore

            public static readonly BlockDef LogicalAnd = new BlockDef("AND", 146, BlockType.Pasive, new Vector2I(2, 2), new Terminal(WireType.Bool, TerminalType.Out, "Tru1 & Tru2"), new Terminal(WireType.Bool, TerminalType.In, "Tru2"), new Terminal(WireType.Bool, TerminalType.In, "Tru1"));
            public static readonly BlockDef LogicalOr = new BlockDef("OR", 417, BlockType.Pasive, new Vector2I(2, 2), new Terminal(WireType.Bool, TerminalType.Out, "Tru1 | Tru2"), new Terminal(WireType.Bool, TerminalType.In, "Tru2"), new Terminal(WireType.Bool, TerminalType.In, "Tru1"));

            public static readonly BlockDef Less = new BlockDef("Less Than", 128, BlockType.Pasive, new Vector2I(2, 2), new Terminal(WireType.Bool, TerminalType.Out, "Num1 < Num2"), new Terminal(WireType.Float, TerminalType.In, "Num2"), new Terminal(WireType.Float, TerminalType.In, "Num1"));
            public static readonly BlockDef Greater = new BlockDef("Greater Than", 481, BlockType.Pasive, new Vector2I(2, 2), new Terminal(WireType.Bool, TerminalType.Out, "Num1 > Num2"), new Terminal(WireType.Float, TerminalType.In, "Num2"), new Terminal(WireType.Float, TerminalType.In, "Num1"));

            public static readonly BlockDef Random = new BlockDef("Random", 168, BlockType.Pasive, new Vector2I(2, 2), new Terminal(WireType.Float, TerminalType.Out, "Random"), new Terminal(WireType.Float, TerminalType.In, "Max"), new Terminal(WireType.Float, TerminalType.In, "Min"));

            public static readonly BlockDef RandomSeed = new BlockDef("Random Seed", 485, BlockType.Active, new Vector2I(2, 2), new Terminal(WireType.Void, TerminalType.Out), new Terminal(WireType.Float, TerminalType.In, "Seed"), new Terminal(WireType.Void, TerminalType.In));

            public static readonly BlockDef Min = new BlockDef("Min", 176, BlockType.Pasive, new Vector2I(2, 2), new Terminal(WireType.Float, TerminalType.Out, "Smaller"), new Terminal(WireType.Float, TerminalType.In, "Num2"), new Terminal(WireType.Float, TerminalType.In, "Num1"));
            public static readonly BlockDef Max = new BlockDef("Min", 180, BlockType.Pasive, new Vector2I(2, 2), new Terminal(WireType.Float, TerminalType.Out, "Bigger"), new Terminal(WireType.Float, TerminalType.In, "Num2"), new Terminal(WireType.Float, TerminalType.In, "Num1"));

            public static readonly BlockDef Sin = new BlockDef("Sin", 413, BlockType.Pasive, new Vector2I(2, 1), new Terminal(WireType.Float, TerminalType.Out, "Sin(Num)"), new Terminal(WireType.Float, TerminalType.In, "Num"));
            public static readonly BlockDef Cos = new BlockDef("Cos", 453, BlockType.Pasive, new Vector2I(2, 1), new Terminal(WireType.Float, TerminalType.Out, "Cos(Num)"), new Terminal(WireType.Float, TerminalType.In, "Num"));
            public static readonly BlockDef Round = new BlockDef("Round", 184, BlockType.Pasive, new Vector2I(2, 1), new Terminal(WireType.Float, TerminalType.Out, "Rounded"), new Terminal(WireType.Float, TerminalType.In, "Number"));
            public static readonly BlockDef Floor = new BlockDef("Floor", 186, BlockType.Pasive, new Vector2I(2, 1), new Terminal(WireType.Float, TerminalType.Out, "Floor"), new Terminal(WireType.Float, TerminalType.In, "Number"));
            public static readonly BlockDef Ceiling = new BlockDef("Ceiling", 188, BlockType.Pasive, new Vector2I(2, 1), new Terminal(WireType.Float, TerminalType.Out, "Ceiling"), new Terminal(WireType.Float, TerminalType.In, "Number"));
            public static readonly BlockDef Absolute = new BlockDef("Absolute", 455, BlockType.Pasive, new Vector2I(2, 1), new Terminal(WireType.Float, TerminalType.Out, "|Num|"), new Terminal(WireType.Float, TerminalType.In, "Num"));

            public static readonly BlockDef Logarithm = new BlockDef("Logarithm", 580, BlockType.Pasive, new Vector2I(2, 2), new Terminal(WireType.Float, TerminalType.Out, "Logarithm"), new Terminal(WireType.Float, TerminalType.In, "Base"), new Terminal(WireType.Float, TerminalType.In, "Number"));

            public static readonly BlockDef Normalize = new BlockDef("Normalize", 578, BlockType.Pasive, new Vector2I(2, 1), new Terminal(WireType.Vec3, TerminalType.Out, "Normalized"), new Terminal(WireType.Vec3, TerminalType.In, "Vector"));

            public static readonly BlockDef DotProduct = new BlockDef("Dot Product", 570, BlockType.Pasive, new Vector2I(2, 2), new Terminal(WireType.Float, TerminalType.Out, "Dot Product"), new Terminal(WireType.Vec3, TerminalType.In, "Vector"), new Terminal(WireType.Vec3, TerminalType.In, "Vector"));
            public static readonly BlockDef CrossProduct = new BlockDef("Cross Product", 574, BlockType.Pasive, new Vector2I(2, 2), new Terminal(WireType.Vec3, TerminalType.Out, "Cross Product"), new Terminal(WireType.Vec3, TerminalType.In, "Vector"), new Terminal(WireType.Vec3, TerminalType.In, "Vector"));
            public static readonly BlockDef Distance = new BlockDef("Distance", 190, BlockType.Pasive, new Vector2I(2, 2), new Terminal(WireType.Float, TerminalType.Out, "Distance"), new Terminal(WireType.Vec3, TerminalType.In, "Vector"), new Terminal(WireType.Vec3, TerminalType.In, "Vector"));

            public static readonly BlockDef Lerp = new BlockDef("LERP", 194, BlockType.Pasive, new Vector2I(2, 3), new Terminal(WireType.Rot, TerminalType.Out, "Rotation"), new Terminal(WireType.Float, TerminalType.In, "Amount"), new Terminal(WireType.Rot, TerminalType.In, "To"), new Terminal(WireType.Rot, TerminalType.In, "From"));

            public static readonly BlockDef AxisAngle = new BlockDef("Axis Angle", 200, BlockType.Pasive, new Vector2I(2, 2), new Terminal(WireType.Rot, TerminalType.Out, "Rotation"), new Terminal(WireType.Float, TerminalType.In, "Angle"), new Terminal(WireType.Vec3, TerminalType.In, "Axis"));

            public static readonly BlockDef ScreenToWorld = new BlockDef("Screen To World", 216, BlockType.Pasive, new Vector2I(2, 2), new Terminal(WireType.Vec3, TerminalType.Out, "World Far"), new Terminal(WireType.Vec3, TerminalType.Out, "World Near"), new Terminal(WireType.Float, TerminalType.In, "Screen Y"), new Terminal(WireType.Float, TerminalType.In, "Screen X"));
            public static readonly BlockDef WorldToScreen = new BlockDef("World To Screen", 477, BlockType.Pasive, new Vector2I(2, 2), new Terminal(WireType.Float, TerminalType.Out, "Screen Y"), new Terminal(WireType.Float, TerminalType.Out, "Screen X"), new Terminal(WireType.Vec3, TerminalType.In, "World Pos"));

            public static readonly BlockDef LookRotation = new BlockDef("Look Rotation", 204, BlockType.Pasive, new Vector2I(2, 2), new Terminal(WireType.Rot, TerminalType.Out, "Rotation"), new Terminal(WireType.Vec3, TerminalType.In, "Up"), new Terminal(WireType.Vec3, TerminalType.In, "Direction"));

            public static readonly BlockDef LineVsPlane = new BlockDef("Line vs Plane", 208, BlockType.Pasive, new Vector2I(2, 4), new Terminal(WireType.Vec3, TerminalType.Out, "Intersection"), new Terminal(WireType.Vec3, TerminalType.In, "Plane Normal"), new Terminal(WireType.Vec3, TerminalType.In, "Plane Point"), new Terminal(WireType.Vec3, TerminalType.In, "Line To"), new Terminal(WireType.Vec3, TerminalType.In, "Line From"));

#pragma warning disable SA1310 // Field names should not contain underscore
            public static readonly BlockDef Make_Vector = new BlockDef("Make Vector", 150, BlockType.Pasive, new Vector2I(2, 3), new Terminal(WireType.Vec3, TerminalType.Out, "Vector"), new Terminal(WireType.Float, TerminalType.In, "Z"), new Terminal(WireType.Float, TerminalType.In, "Y"), new Terminal(WireType.Float, TerminalType.In, "X"));
            public static readonly BlockDef Break_Vector = new BlockDef("Break Vector", 156, BlockType.Pasive, new Vector2I(2, 3), new Terminal(WireType.Float, TerminalType.Out, "Z"), new Terminal(WireType.Float, TerminalType.Out, "Y"), new Terminal(WireType.Float, TerminalType.Out, "X"), new Terminal(WireType.Vec3, TerminalType.In, "Vector"));
            public static readonly BlockDef Make_Rotation = new BlockDef("Make Rotation", 162, BlockType.Pasive, new Vector2I(2, 3), new Terminal(WireType.Rot, TerminalType.Out, "Rotation"), new Terminal(WireType.Float, TerminalType.In, "Z angle"), new Terminal(WireType.Float, TerminalType.In, "Y angle"), new Terminal(WireType.Float, TerminalType.In, "X angle"));
            public static readonly BlockDef Break_Rotation = new BlockDef("Break Rotation", 442, BlockType.Pasive, new Vector2I(2, 3), new Terminal(WireType.Float, TerminalType.Out, "Z angle"), new Terminal(WireType.Float, TerminalType.Out, "Y angle"), new Terminal(WireType.Float, TerminalType.Out, "X angle"), new Terminal(WireType.Rot, TerminalType.In, "Rotation"));
#pragma warning restore SA1310 // Field names should not contain underscore

            public static BlockDef EqualsByType(WireType type)
                => type.ToNormal() switch
                {
                    WireType.Float => Equals_Number,
                    WireType.Vec3 => Equals_Vector,
                    WireType.Bool => Equals_Bool,
                    WireType.Obj => Equals_Object,
                    _ => throw new UnknownEnumValueException<WireType>(type),
                };

            public static BlockDef BreakByType(WireType type)
                => type.ToNormal() switch
                {
                    WireType.Vec3 => Break_Vector,
                    WireType.Rot => Break_Rotation,
                    _ => throw new UnknownEnumValueException<WireType>(type),
                };

            public static BlockDef MakeByType(WireType type)
                => type.ToNormal() switch
                {
                    WireType.Vec3 => Make_Vector,
                    WireType.Rot => Make_Rotation,
                    _ => throw new UnknownEnumValueException<WireType>(type),
                };
        }

        public static class Values
        {
            public static readonly BlockDef Number = new BlockDef("Number", 36, BlockType.Value, new Vector2I(2, 1), new Terminal(WireType.Float, TerminalType.Out, "Number"));
            public static readonly BlockDef Vector = new BlockDef("Vector", 38, BlockType.Value, new Vector2I(2, 2), new Terminal(WireType.Vec3, TerminalType.Out, "Vector"));
            public static readonly BlockDef Rotation = new BlockDef("Rotation", 42, BlockType.Value, new Vector2I(2, 2), new Terminal(WireType.Rot, TerminalType.Out, "Rotation"));
            public static readonly BlockDef True = new BlockDef("True", 449, BlockType.Value, new Vector2I(2, 1), new Terminal(WireType.Bool, TerminalType.Out, "True"));
            public static readonly BlockDef False = new BlockDef("False", 451, BlockType.Value, new Vector2I(2, 1), new Terminal(WireType.Bool, TerminalType.Out, "False"));

            public static readonly BlockDef Comment = new BlockDef("Comment", 15, BlockType.Value, new Vector2I(1, 1));

#pragma warning disable SA1310 // Field names should not contain underscore
            public static readonly BlockDef Inspect_Number = new BlockDef("Inspect Number", 16, BlockType.Active, new Vector2I(2, 2), new Terminal(WireType.Void, TerminalType.Out, "After"), new Terminal(WireType.Float, TerminalType.In, "Number"), new Terminal(WireType.Void, TerminalType.In, "Before"));
            public static readonly BlockDef Inspect_Vector = new BlockDef("Inspect Vector", 20, BlockType.Active, new Vector2I(2, 2), new Terminal(WireType.Void, TerminalType.Out, "After"), new Terminal(WireType.Vec3, TerminalType.In, "Vector"), new Terminal(WireType.Void, TerminalType.In, "Before"));
            public static readonly BlockDef Inspect_Rotation = new BlockDef("Inspect Rotation", 24, BlockType.Active, new Vector2I(2, 2), new Terminal(WireType.Void, TerminalType.Out, "After"), new Terminal(WireType.Rot, TerminalType.In, "Rotation"), new Terminal(WireType.Void, TerminalType.In, "Before"));
            public static readonly BlockDef Inspect_Truth = new BlockDef("Inspect Truth", 28, BlockType.Active, new Vector2I(2, 2), new Terminal(WireType.Void, TerminalType.Out, "After"), new Terminal(WireType.Bool, TerminalType.In, "Truth"), new Terminal(WireType.Void, TerminalType.In, "Before"));
            public static readonly BlockDef Inspect_Object = new BlockDef("Inspect Object", 32, BlockType.Active, new Vector2I(2, 2), new Terminal(WireType.Void, TerminalType.Out, "After"), new Terminal(WireType.Obj, TerminalType.In, "Object"), new Terminal(WireType.Void, TerminalType.In, "Before"));
#pragma warning restore SA1310 // Field names should not contain underscore

            public static BlockDef ValueByType(object value)
                => value switch
                {
                    float => Number,
                    bool b => b ? True : False,
                    Vector3F => Vector,
                    Compiler.Rotation => Rotation,
                    _ => throw new Exception($"Value doesn't exist for Type '{value.GetType()}',"),
                };

            public static BlockDef InspectByType(WireType type)
                => type.ToNormal() switch
                {
                    WireType.Float => Inspect_Number,
                    WireType.Bool => Inspect_Truth,
                    WireType.Vec3 => Inspect_Vector,
                    WireType.Rot => Inspect_Rotation,
                    WireType.Obj => Inspect_Object,
                    _ => throw new UnknownEnumValueException<WireType>(type),
                };
        }

        public static class Variables
        {
#pragma warning disable SA1310 // Field names should not contain underscore
            #region variable
            public static readonly BlockDef Variable_Num = new BlockDef("Variable", 46, BlockType.Pasive, new Vector2I(2, 1), new Terminal(WireType.FloatPtr, TerminalType.Out, "Number"));
            public static readonly BlockDef Variable_Vec = new BlockDef("Variable", 48, BlockType.Pasive, new Vector2I(2, 1), new Terminal(WireType.Vec3Ptr, TerminalType.Out, "Vector"));
            public static readonly BlockDef Variable_Rot = new BlockDef("Variable", 50, BlockType.Pasive, new Vector2I(2, 1), new Terminal(WireType.RotPtr, TerminalType.Out, "Rotation"));
            public static readonly BlockDef Variable_Tru = new BlockDef("Variable", 52, BlockType.Pasive, new Vector2I(2, 1), new Terminal(WireType.BoolPtr, TerminalType.Out, "Truth"));
            public static readonly BlockDef Variable_Obj = new BlockDef("Variable", 54, BlockType.Pasive, new Vector2I(2, 1), new Terminal(WireType.ObjPtr, TerminalType.Out, "Object"));
            public static readonly BlockDef Variable_Con = new BlockDef("Variable", 56, BlockType.Pasive, new Vector2I(2, 1), new Terminal(WireType.ConPtr, TerminalType.Out, "Constraint"));
            #endregion
            #region set_variable
            public static readonly BlockDef Set_Variable_Num = new BlockDef("Set Variable", 428, BlockType.Active, new Vector2I(2, 1), new Terminal(WireType.Void, TerminalType.Out, "After"), new Terminal(WireType.Float, TerminalType.In, "Value"), new Terminal(WireType.Void, TerminalType.In, "Before"));
            public static readonly BlockDef Set_Variable_Vec = new BlockDef("Set Variable", 430, BlockType.Active, new Vector2I(2, 1), new Terminal(WireType.Void, TerminalType.Out, "After"), new Terminal(WireType.Vec3, TerminalType.In, "Value"), new Terminal(WireType.Void, TerminalType.In, "Before"));
            public static readonly BlockDef Set_Variable_Rot = new BlockDef("Set Variable", 432, BlockType.Active, new Vector2I(2, 1), new Terminal(WireType.Void, TerminalType.Out, "After"), new Terminal(WireType.Rot, TerminalType.In, "Value"), new Terminal(WireType.Void, TerminalType.In, "Before"));
            public static readonly BlockDef Set_Variable_Tru = new BlockDef("Set Variable", 434, BlockType.Active, new Vector2I(2, 1), new Terminal(WireType.Void, TerminalType.Out, "After"), new Terminal(WireType.Bool, TerminalType.In, "Value"), new Terminal(WireType.Void, TerminalType.In, "Before"));
            public static readonly BlockDef Set_Variable_Obj = new BlockDef("Set Variable", 436, BlockType.Active, new Vector2I(2, 1), new Terminal(WireType.Void, TerminalType.Out, "After"), new Terminal(WireType.Obj, TerminalType.In, "Value"), new Terminal(WireType.Void, TerminalType.In, "Before"));
            public static readonly BlockDef Set_Variable_Con = new BlockDef("Set Variable", 438, BlockType.Active, new Vector2I(2, 1), new Terminal(WireType.Void, TerminalType.Out, "After"), new Terminal(WireType.Con, TerminalType.In, "Value"), new Terminal(WireType.Void, TerminalType.In, "Before"));
            #endregion
            #region set_ptr

            // the big setters that take in variable
            public static readonly BlockDef Set_Ptr_Num = new BlockDef("Set Number", 58, BlockType.Active, new Vector2I(2, 2), new Terminal(WireType.Void, TerminalType.Out, "After"), new Terminal(WireType.Float, TerminalType.In, "Value"), new Terminal(WireType.FloatPtr, TerminalType.In, "Variable"), new Terminal(WireType.Void, TerminalType.In, "Before"));
            public static readonly BlockDef Set_Ptr_Vec = new BlockDef("Set Vector", 62, BlockType.Active, new Vector2I(2, 2), new Terminal(WireType.Void, TerminalType.Out, "After"), new Terminal(WireType.Vec3, TerminalType.In, "Value"), new Terminal(WireType.Vec3Ptr, TerminalType.In, "Variable"), new Terminal(WireType.Void, TerminalType.In, "Before"));
            public static readonly BlockDef Set_Ptr_Rot = new BlockDef("Set Rotation", 66, BlockType.Active, new Vector2I(2, 2), new Terminal(WireType.Void, TerminalType.Out, "After"), new Terminal(WireType.Rot, TerminalType.In, "Value"), new Terminal(WireType.RotPtr, TerminalType.In, "Variable"), new Terminal(WireType.Void, TerminalType.In, "Before"));
            public static readonly BlockDef Set_Ptr_Tru = new BlockDef("Set Truth", 70, BlockType.Active, new Vector2I(2, 2), new Terminal(WireType.Void, TerminalType.Out, "After"), new Terminal(WireType.Bool, TerminalType.In, "Value"), new Terminal(WireType.BoolPtr, TerminalType.In, "Variable"), new Terminal(WireType.Void, TerminalType.In, "Before"));
            public static readonly BlockDef Set_Ptr_Obj = new BlockDef("Set Object", 74, BlockType.Active, new Vector2I(2, 2), new Terminal(WireType.Void, TerminalType.Out, "After"), new Terminal(WireType.Obj, TerminalType.In, "Value"), new Terminal(WireType.ObjPtr, TerminalType.In, "Variable"), new Terminal(WireType.Void, TerminalType.In, "Before"));
            public static readonly BlockDef Set_Ptr_Con = new BlockDef("Set Constraint", 78, BlockType.Active, new Vector2I(2, 2), new Terminal(WireType.Void, TerminalType.Out, "After"), new Terminal(WireType.Obj, TerminalType.In, "Value"), new Terminal(WireType.ConPtr, TerminalType.In, "Variable"), new Terminal(WireType.Void, TerminalType.In, "Before"));
            #endregion
            #region list
            public static readonly BlockDef List_Num = new BlockDef("List Number", 82, BlockType.Pasive, new Vector2I(2, 2), new Terminal(WireType.FloatPtr, TerminalType.Out, "Element"), new Terminal(WireType.Float, TerminalType.In, "Index"), new Terminal(WireType.FloatPtr, TerminalType.In, "Variable"));
            public static readonly BlockDef List_Vec = new BlockDef("List Vector", 461, BlockType.Pasive, new Vector2I(2, 2), new Terminal(WireType.ObjPtr, TerminalType.Out, "Element"), new Terminal(WireType.Float, TerminalType.In, "Index"), new Terminal(WireType.ObjPtr, TerminalType.In, "Variable"));
            public static readonly BlockDef List_Rot = new BlockDef("List Rotation", 465, BlockType.Pasive, new Vector2I(2, 2), new Terminal(WireType.RotPtr, TerminalType.Out, "Element"), new Terminal(WireType.Float, TerminalType.In, "Index"), new Terminal(WireType.RotPtr, TerminalType.In, "Variable"));
            public static readonly BlockDef List_Tru = new BlockDef("List Truth", 469, BlockType.Pasive, new Vector2I(2, 2), new Terminal(WireType.BoolPtr, TerminalType.Out, "Element"), new Terminal(WireType.Float, TerminalType.In, "Index"), new Terminal(WireType.BoolPtr, TerminalType.In, "Variable"));
            public static readonly BlockDef List_Obj = new BlockDef("List Object", 86, BlockType.Pasive, new Vector2I(2, 2), new Terminal(WireType.ObjPtr, TerminalType.Out, "Element"), new Terminal(WireType.Float, TerminalType.In, "Index"), new Terminal(WireType.ObjPtr, TerminalType.In, "Variable"));
            public static readonly BlockDef List_Con = new BlockDef("List Constraint", 473, BlockType.Pasive, new Vector2I(2, 2), new Terminal(WireType.ConPtr, TerminalType.Out, "Element"), new Terminal(WireType.Float, TerminalType.In, "Index"), new Terminal(WireType.ConPtr, TerminalType.In, "Variable"));
            #endregion
#pragma warning restore SA1310 // Field names should not contain underscore
            public static readonly BlockDef PlusPlusFloat = new BlockDef("Increase Number", 556, BlockType.Active, new Vector2I(2, 1), new Terminal(WireType.Void, TerminalType.Out, "After"), new Terminal(WireType.Float, TerminalType.In, "Variable"), new Terminal(WireType.Void, TerminalType.In, "Before"));
            public static readonly BlockDef MinusMinusFloat = new BlockDef("Decrease Number", 558, BlockType.Active, new Vector2I(2, 1), new Terminal(WireType.Void, TerminalType.Out, "After"), new Terminal(WireType.Float, TerminalType.In, "Variable"), new Terminal(WireType.Void, TerminalType.In, "Before"));

            public static BlockDef VariableByType(WireType type)
                => type.ToNormal() switch
                {
                    WireType.Float => Variable_Num,
                    WireType.Bool => Variable_Tru,
                    WireType.Vec3 => Variable_Vec,
                    WireType.Rot => Variable_Rot,
                    WireType.Obj => Variable_Obj,
                    WireType.Con => Variable_Con,
                    _ => throw new UnknownEnumValueException<WireType>(type),
                };

            public static BlockDef Set_VariableByType(WireType type)
                => type.ToNormal() switch
                {
                    WireType.Float => Set_Variable_Num,
                    WireType.Bool => Set_Variable_Tru,
                    WireType.Vec3 => Set_Variable_Vec,
                    WireType.Rot => Set_Variable_Rot,
                    WireType.Obj => Set_Variable_Obj,
                    WireType.Con => Set_Variable_Con,
                    _ => throw new UnknownEnumValueException<WireType>(type),
                };

            public static BlockDef ListByType(WireType type)
                => type.ToNormal() switch
                {
                    WireType.Float => List_Num,
                    WireType.Bool => List_Tru,
                    WireType.Vec3 => List_Vec,
                    WireType.Rot => List_Rot,
                    WireType.Obj => List_Obj,
                    WireType.Con => List_Con,
                    _ => throw new UnknownEnumValueException<WireType>(type),
                };

            public static BlockDef Set_PtrByType(WireType type)
                => type.ToNormal() switch
                {
                    WireType.Float => Set_Ptr_Num,
                    WireType.Bool => Set_Ptr_Tru,
                    WireType.Vec3 => Set_Ptr_Vec,
                    WireType.Rot => Set_Ptr_Rot,
                    WireType.Obj => Set_Ptr_Obj,
                    WireType.Con => Set_Ptr_Rot,
                    _ => throw new UnknownEnumValueException<WireType>(type),
                };
        }
    }
}
