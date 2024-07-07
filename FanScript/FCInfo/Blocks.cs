using MathUtils.Vectors;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FanScript.FCInfo
{
    public static class Blocks
    {
        public static bool PositionsLoaded { get; private set; } = false;

        public static void LoadPositions()
        {
            List<FieldInfo> _fields = new List<FieldInfo>();
            Type[] innerTypes = typeof(Blocks).GetNestedTypes(BindingFlags.Static | BindingFlags.Public);
            for (int i = 0; i < innerTypes.Length; i++)
                _fields.AddRange(innerTypes[i].GetFields(BindingFlags.Static | BindingFlags.Public));

            Dictionary<ushort, DefBlock> fields = new Dictionary<ushort, DefBlock>();
            foreach (FieldInfo field in _fields)
                if (field.FieldType == typeof(DefBlock))
                {
                    DefBlock val = (DefBlock)field.GetValue(null)!;
                    fields.Add(val.Id, val);
                }

            TerminalInfo[]? infos = JsonConvert.DeserializeObject<TerminalInfo[]>(File.ReadAllText(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "terminals.json")));

            if (infos is null) throw new Exception("terminals.json is corrupted");

            for (int i = 0; i < infos.Length; i++)
            {
                TerminalInfo info = infos[i];
                if (!fields.ContainsKey(info.Id))
                    continue;

                DefBlock block = fields[info.Id];
                if (block.Terminals.Length != info.Positions.Length)
                    throw new Exception($"Terminals.Length ({block.Terminals.Length}) != Positions.Length ({info.Positions.Length}) for block: {block}");

                for (int j = 0; j < info.Positions.Length; j++)
                    block.Terminals[j].Pos = info.Positions[j];

                fields.Remove(info.Id);
            }

            PositionsLoaded = true;
        }

        // Fake block, used by labels
        public static DefBlock Nop { get; private set; } = new DefBlock("Nop", ushort.MaxValue, (BlockType)(-1), new Vector2I(-1, -1), new Terminal(0, WireType.Void, TerminalType.Out), new Terminal(1, WireType.Void, TerminalType.In));

        public static class Objects
        {
            public static readonly DefBlock SetPos = new DefBlock("Set Position", 282, BlockType.Active, new Vector2I(2, 3), new Terminal(0, WireType.Void, TerminalType.Out), new Terminal(1, WireType.Rot, TerminalType.In, "Rotation"), new Terminal(2, WireType.Vec3, TerminalType.In, "Position"), new Terminal(3, WireType.Obj, TerminalType.In, "Object"), new Terminal(4, WireType.Void, TerminalType.In));
        }

        public static class Control
        {
            public static readonly DefBlock If = new DefBlock("If", 234, BlockType.Active, new Vector2I(2, 2), new Terminal(0, WireType.Void, TerminalType.Out), new Terminal(1, WireType.Void, TerminalType.Out, "False"), new Terminal(2, WireType.Void, TerminalType.Out, "True"), new Terminal(3, WireType.Bool, TerminalType.In, "Condition"), new Terminal(4, WireType.Void, TerminalType.In));
            public static readonly DefBlock PlaySensor = new DefBlock("Play Sensor", 238, BlockType.Active, new Vector2I(2, 2), new Terminal(0, WireType.Void, TerminalType.Out), new Terminal(1, WireType.Void, TerminalType.Out, "On Play"), new Terminal(2, WireType.Void, TerminalType.In));
        }

        public static class Math
        {
            public static DefBlock GetEquals(WireType type)
            {
                switch (type)
                {
                    case WireType.Float:
                        return Equals_Number;
                    case WireType.Vec3:
                        return Equals_Vector;
                    case WireType.Bool:
                        return Equals_Bool;
                    case WireType.Obj:
                        return Equals_Object;
                    default:
                        throw new Exception($"Unsuported WireType: {type}");
                }
            }

            public static readonly DefBlock Negate = new DefBlock("Negate", 90, BlockType.Pasive, new Vector2I(2, 1), new Terminal(0, WireType.Float, TerminalType.Out, "-Num"), new Terminal(1, WireType.Float, TerminalType.In, "Num"));
            public static readonly DefBlock Not = new DefBlock("Not", 144, BlockType.Pasive, new Vector2I(2, 1), new Terminal(0, WireType.Bool, TerminalType.Out, "Not Tru"), new Terminal(1, WireType.Bool, TerminalType.In, "Tru"));

            public static readonly DefBlock Add_Number = new DefBlock("Add Numbers", 92, BlockType.Pasive, new Vector2I(2, 2), new Terminal(0, WireType.Float, TerminalType.Out, "Num1 + Num2"), new Terminal(1, WireType.Float, TerminalType.In, "Num2"), new Terminal(2, WireType.Float, TerminalType.In, "Num1"));
            public static readonly DefBlock Add_Vector = new DefBlock("Add Vectors", 96, BlockType.Pasive, new Vector2I(2, 2), new Terminal(0, WireType.Vec3, TerminalType.Out, "Vec1 + Vec2"), new Terminal(1, WireType.Vec3, TerminalType.In, "Vec2"), new Terminal(2, WireType.Vec3, TerminalType.In, "Vec1"));
            public static readonly DefBlock Subtract_Number = new DefBlock("Subtract Numbers", 100, BlockType.Pasive, new Vector2I(2, 2), new Terminal(0, WireType.Float, TerminalType.Out, "Num1 - Num2"), new Terminal(1, WireType.Float, TerminalType.In, "Num2"), new Terminal(2, WireType.Float, TerminalType.In, "Num1"));
            public static readonly DefBlock Subtract_Vector = new DefBlock("Subtract Vectors", 104, BlockType.Pasive, new Vector2I(2, 2), new Terminal(0, WireType.Vec3, TerminalType.Out, "Vec1 - Vec2"), new Terminal(1, WireType.Vec3, TerminalType.In, "Vec2"), new Terminal(2, WireType.Vec3, TerminalType.In, "Vec1"));
            public static readonly DefBlock Multiply_Number = new DefBlock("Multiply", 108, BlockType.Pasive, new Vector2I(2, 2), new Terminal(0, WireType.Float, TerminalType.Out, "Num1 * Num2"), new Terminal(1, WireType.Float, TerminalType.In, "Num2"), new Terminal(2, WireType.Float, TerminalType.In, "Num1"));
            public static readonly DefBlock Multiply_Vector = new DefBlock("Scale", 112, BlockType.Pasive, new Vector2I(2, 2), new Terminal(0, WireType.Vec3, TerminalType.Out, "Vec * Num"), new Terminal(1, WireType.Float, TerminalType.In, "Num"),  new Terminal(2, WireType.Vec3, TerminalType.In, "Vec"));
            public static readonly DefBlock Multiply_Rotation = new DefBlock("Combine", 120, BlockType.Pasive, new Vector2I(2, 2), new Terminal(0, WireType.Rot, TerminalType.Out, "Rot1 * Rot2"), new Terminal(1, WireType.Rot, TerminalType.In, "Rot2"), new Terminal(2, WireType.Rot, TerminalType.In, "Rot1"));
            public static readonly DefBlock Divide_Number = new DefBlock("Divide", 124, BlockType.Pasive, new Vector2I(2, 2), new Terminal(0, WireType.Float, TerminalType.Out, "Num1 + Num2"), new Terminal(1, WireType.Float, TerminalType.In, "Num2"), new Terminal(2, WireType.Float, TerminalType.In, "Num1"));
            public static readonly DefBlock Modulo_Number = new DefBlock("Modulo", 172, BlockType.Pasive, new Vector2I(2, 2), new Terminal(0, WireType.Float, TerminalType.Out, "mod(a,b)"), new Terminal(1, WireType.Float, TerminalType.In, "b"), new Terminal(2, WireType.Float, TerminalType.In, "a"));

            public static readonly DefBlock Equals_Number = new DefBlock("Equals Numbers", 132, BlockType.Pasive, new Vector2I(2, 2), new Terminal(0, WireType.Bool, TerminalType.Out, "Num1 = Num2"), new Terminal(1, WireType.Float, TerminalType.In, "Num2"), new Terminal(2, WireType.Float, TerminalType.In, "Num1"));
            public static readonly DefBlock Equals_Vector = new DefBlock("Equals Vectors", 136, BlockType.Pasive, new Vector2I(2, 2), new Terminal(0, WireType.Bool, TerminalType.Out, "Vec1 = Vec2"), new Terminal(1, WireType.Vec3, TerminalType.In, "Vec2"), new Terminal(2, WireType.Vec3, TerminalType.In, "Vec1"));
            public static readonly DefBlock Equals_Object = new DefBlock("Equals Objects", 140, BlockType.Pasive, new Vector2I(2, 2), new Terminal(0, WireType.Bool, TerminalType.Out, "Obj1 = Obj2"), new Terminal(1, WireType.Obj, TerminalType.In, "Obj2"), new Terminal(2, WireType.Obj, TerminalType.In, "Obj1"));
            public static readonly DefBlock Equals_Bool = new DefBlock("Equals Truths", 421, BlockType.Pasive, new Vector2I(2, 2), new Terminal(0, WireType.Bool, TerminalType.Out, "Tru1 = Tru2"), new Terminal(1, WireType.Bool, TerminalType.In, "Tru2"), new Terminal(2, WireType.Bool, TerminalType.In, "Tru1"));

            public static readonly DefBlock LogicalAnd = new DefBlock("AND", 146, BlockType.Pasive, new Vector2I(2, 2), new Terminal(0, WireType.Bool, TerminalType.Out, "Tru1 & Tru2"), new Terminal(1, WireType.Bool, TerminalType.In, "Tru2"), new Terminal(2, WireType.Bool, TerminalType.In, "Tru1"));
            public static readonly DefBlock LogicalOr = new DefBlock("OR", 417, BlockType.Pasive, new Vector2I(2, 2), new Terminal(0, WireType.Bool, TerminalType.Out, "Tru1 | Tru2"), new Terminal(1, WireType.Bool, TerminalType.In, "Tru2"), new Terminal(2, WireType.Bool, TerminalType.In, "Tru1"));

            public static readonly DefBlock Less = new DefBlock("Less Than", 128, BlockType.Pasive, new Vector2I(2, 2), new Terminal(0, WireType.Bool, TerminalType.Out, "Num1 < Num2"), new Terminal(1, WireType.Float, TerminalType.In, "Num2"), new Terminal(2, WireType.Float, TerminalType.In, "Num1"));
            public static readonly DefBlock Greater = new DefBlock("Greater Than", 481, BlockType.Pasive, new Vector2I(2, 2), new Terminal(0, WireType.Bool, TerminalType.Out, "Num1 > Num2"), new Terminal(1, WireType.Float, TerminalType.In, "Num2"), new Terminal(2, WireType.Float, TerminalType.In, "Num1"));

            public static readonly DefBlock Make_Vector = new DefBlock("Make Vector", 150, BlockType.Pasive, new Vector2I(2, 3), new Terminal(0, WireType.Vec3, TerminalType.Out, "Vector"), new Terminal(1, WireType.Float, TerminalType.In, "Z"), new Terminal(2, WireType.Float, TerminalType.In, "y"), new Terminal(3, WireType.Float, TerminalType.In, "X"));
            public static readonly DefBlock Break_Vector = new DefBlock("Break Vector", 156, BlockType.Pasive, new Vector2I(2, 3), new Terminal(0, WireType.Float, TerminalType.Out, "Z"), new Terminal(1, WireType.Float, TerminalType.Out, "y"), new Terminal(2, WireType.Float, TerminalType.Out, "X"), new Terminal(3, WireType.Vec3, TerminalType.In, "Vector"));
            public static readonly DefBlock Make_Rotation = new DefBlock("Make Rotation", 162, BlockType.Pasive, new Vector2I(2, 3), new Terminal(0, WireType.Rot, TerminalType.Out, "Rotation"), new Terminal(1, WireType.Float, TerminalType.In, "Z angle"), new Terminal(2, WireType.Float, TerminalType.In, "y angle"), new Terminal(3, WireType.Float, TerminalType.In, "X angle"));
            public static readonly DefBlock Break_Rotation = new DefBlock("Break Rotation", 442, BlockType.Pasive, new Vector2I(2, 3), new Terminal(0, WireType.Float, TerminalType.Out, "Z angle"), new Terminal(1, WireType.Float, TerminalType.Out, "y angle"), new Terminal(2, WireType.Float, TerminalType.Out, "X angle"), new Terminal(3, WireType.Rot, TerminalType.In, "Rotation"));
        }

        public static class Values
        {
            public static DefBlock GetValue(object value)
            {
                if (value is float)
                    return Number;
                else if (value is bool b)
                    return b ? True : False;
                else
                    throw new Exception($"Value doesn't exist for Type '{value.GetType()}',");
            }
            public static DefBlock GetInspect(WireType type)
            {
                switch (type)
                {
                    case WireType.Float:
                        return Inspect_Number;
                    case WireType.Bool:
                        return Inspect_Truth;
                    case WireType.Vec3:
                        return Inspect_Vector;
                    case WireType.Rot:
                        return Inspect_Rotation;
                    default:
                        throw new Exception($"Cannot get inspect for WireType: \"{Enum.GetName(typeof(WireType), type)}\"");
                }
            }

            public static readonly DefBlock Number = new DefBlock("Number", 36, BlockType.Value, new Vector2I(2, 1), new Terminal(0, WireType.Float, TerminalType.Out, "Number"));
            public static readonly DefBlock Vector = new DefBlock("Vector", 38, BlockType.Value, new Vector2I(2, 2), new Terminal(0, WireType.Vec3, TerminalType.Out, "Vector"));
            public static readonly DefBlock Rotation = new DefBlock("Rotation", 42, BlockType.Value, new Vector2I(2, 2), new Terminal(0, WireType.Rot, TerminalType.Out, "Rotation"));
            public static readonly DefBlock True = new DefBlock("True", 449, BlockType.Value, new Vector2I(2, 1), new Terminal(0, WireType.Bool, TerminalType.Out, "True"));
            public static readonly DefBlock False = new DefBlock("False", 451, BlockType.Value, new Vector2I(2, 1), new Terminal(0, WireType.Bool, TerminalType.Out, "False"));

            public static readonly DefBlock Inspect_Number = new DefBlock("Inspect Number", 16, BlockType.Active, new Vector2I(2, 2), new Terminal(0, WireType.Void, TerminalType.Out, "After"), new Terminal(1, WireType.Float, TerminalType.In, "Number"), new Terminal(2, WireType.Void, TerminalType.In, "Before"));
            public static readonly DefBlock Inspect_Vector = new DefBlock("Inspect Vector", 20, BlockType.Active, new Vector2I(2, 2), new Terminal(0, WireType.Void, TerminalType.Out, "After"), new Terminal(1, WireType.Vec3, TerminalType.In, "Vector"), new Terminal(2, WireType.Void, TerminalType.In, "Before"));
            public static readonly DefBlock Inspect_Rotation = new DefBlock("Inspect Rotation", 24, BlockType.Active, new Vector2I(2, 2), new Terminal(0, WireType.Void, TerminalType.Out, "After"), new Terminal(1, WireType.Rot, TerminalType.In, "Rotation"), new Terminal(2, WireType.Void, TerminalType.In, "Before"));
            public static readonly DefBlock Inspect_Truth = new DefBlock("Inspect Truth", 28, BlockType.Active, new Vector2I(2, 2), new Terminal(0, WireType.Void, TerminalType.Out, "After"), new Terminal(1, WireType.Bool, TerminalType.In, "Truth"), new Terminal(2, WireType.Void, TerminalType.In, "Before"));
        }

        public static class Variables
        {
            public static DefBlock Get_Variable(WireType type)
            {
                switch (type)
                {
                    case WireType.Float:
                        return Variable_Num;
                    case WireType.Bool:
                        return Variable_Tru;
                    case WireType.Vec3:
                        return Variable_Vec;
                    case WireType.Rot:
                        return Variable_Rot;
                    case WireType.Obj:
                        return Variable_Obj;
                    default:
                        throw new Exception($"Get_variable doesn't exist for WireType \"{Enum.GetName(typeof(WireType), type)}\"");
                }
            }
            public static DefBlock GetSet_Variable(WireType type)
            {
                switch (type)
                {
                    case WireType.Float:
                        return Set_Variable_Num;
                    case WireType.Bool:
                        return Set_Variable_Tru;
                    case WireType.Vec3:
                        return Set_Variable_Vec;
                    case WireType.Rot:
                        return Set_Variable_Rot;
                    case WireType.Obj:
                        return Set_Variable_Obj;
                    default:
                        throw new Exception($"Set_Variable doesn't exist for WireType \"{Enum.GetName(typeof(WireType), type)}\"");
                }
            }
            public static DefBlock GetList(WireType type)
            {
                switch (type)
                {
                    case WireType.Float:
                        return List_Num;
                    case WireType.Bool:
                        return List_Tru;
                    case WireType.Vec3:
                        return List_Vec;
                    case WireType.Rot:
                        return List_Rot;
                    case WireType.Obj:
                        return List_Obj;
                    default:
                        throw new Exception($"List doesn't exist for WireType \"{Enum.GetName(typeof(WireType), type)}\"");
                }
            }
            public static DefBlock GetSet_Ptr(WireType type)
            {
                switch (type)
                {
                    case WireType.Float:
                        return Set_Ptr_Num;
                    case WireType.Bool:
                        return Set_Ptr_Tru;
                    case WireType.Vec3:
                        return Set_Ptr_Vec;
                    case WireType.Rot:
                        return Set_Ptr_Rot;
                    case WireType.Obj:
                        return Set_Ptr_Obj;
                    default:
                        throw new Exception($"Set_Ptr doesn't exist for WireType \"{Enum.GetName(typeof(WireType), type)}\"");
                }
            }

            #region variable
            public static readonly DefBlock Variable_Num = new DefBlock("Variable", 46, BlockType.Pasive, new Vector2I(2, 1), new Terminal(0, WireType.FloatPtr, TerminalType.Out, "Number"));
            public static readonly DefBlock Variable_Vec = new DefBlock("Variable", 48, BlockType.Pasive, new Vector2I(2, 1), new Terminal(0, WireType.Vec3Ptr, TerminalType.Out, "Vector"));
            public static readonly DefBlock Variable_Rot = new DefBlock("Variable", 50, BlockType.Pasive, new Vector2I(2, 1), new Terminal(0, WireType.RotPtr, TerminalType.Out, "Rotation"));
            public static readonly DefBlock Variable_Tru = new DefBlock("Variable", 52, BlockType.Pasive, new Vector2I(2, 1), new Terminal(0, WireType.BoolPtr, TerminalType.Out, "Truth"));
            public static readonly DefBlock Variable_Obj = new DefBlock("Variable", 54, BlockType.Pasive, new Vector2I(2, 1), new Terminal(0, WireType.ObjPtr, TerminalType.Out, "Object"));
            #endregion
            #region set_variable
            public static readonly DefBlock Set_Variable_Num = new DefBlock("Set Variable", 428, BlockType.Active, new Vector2I(2, 1), new Terminal(0, WireType.Void, TerminalType.Out, "After"), new Terminal(1, WireType.Float, TerminalType.In, "Value"), new Terminal(2, WireType.Void, TerminalType.In, "Before"));
            public static readonly DefBlock Set_Variable_Vec = new DefBlock("Set Variable", 430, BlockType.Active, new Vector2I(2, 1), new Terminal(0, WireType.Void, TerminalType.Out, "After"), new Terminal(1, WireType.Vec3, TerminalType.In, "Value"), new Terminal(2, WireType.Void, TerminalType.In, "Before"));
            public static readonly DefBlock Set_Variable_Rot = new DefBlock("Set Variable", 432, BlockType.Active, new Vector2I(2, 1), new Terminal(0, WireType.Void, TerminalType.Out, "After"), new Terminal(1, WireType.Rot, TerminalType.In, "Value"), new Terminal(2, WireType.Void, TerminalType.In, "Before"));
            public static readonly DefBlock Set_Variable_Tru = new DefBlock("Set Variable", 434, BlockType.Active, new Vector2I(2, 1), new Terminal(0, WireType.Void, TerminalType.Out, "After"), new Terminal(1, WireType.Bool, TerminalType.In, "Value"), new Terminal(2, WireType.Void, TerminalType.In, "Before"));
            public static readonly DefBlock Set_Variable_Obj = new DefBlock("Set Variable", 436, BlockType.Active, new Vector2I(2, 1), new Terminal(0, WireType.Void, TerminalType.Out, "After"), new Terminal(1, WireType.Obj, TerminalType.In, "Value"), new Terminal(2, WireType.Void, TerminalType.In, "Before"));
            #endregion
            #region set_ptr
            // the big setters that take in variable
            public static readonly DefBlock Set_Ptr_Num = new DefBlock("Set Number", 58, BlockType.Active, new Vector2I(2, 2), new Terminal(0, WireType.Void, TerminalType.Out, "After"), new Terminal(1, WireType.Float, TerminalType.In, "Value"), new Terminal(2, WireType.FloatPtr, TerminalType.In, "Variable"), new Terminal(3, WireType.Void, TerminalType.In, "Before"));
            public static readonly DefBlock Set_Ptr_Vec = new DefBlock("Set Vector", 62, BlockType.Active, new Vector2I(2, 2), new Terminal(0, WireType.Void, TerminalType.Out, "After"), new Terminal(1, WireType.Vec3, TerminalType.In, "Value"), new Terminal(2, WireType.Vec3Ptr, TerminalType.In, "Variable"), new Terminal(3, WireType.Void, TerminalType.In, "Before"));
            public static readonly DefBlock Set_Ptr_Rot = new DefBlock("Set Rotation", 66, BlockType.Active, new Vector2I(2, 2), new Terminal(0, WireType.Void, TerminalType.Out, "After"), new Terminal(1, WireType.Rot, TerminalType.In, "Value"), new Terminal(2, WireType.RotPtr, TerminalType.In, "Variable"), new Terminal(3, WireType.Void, TerminalType.In, "Before"));
            public static readonly DefBlock Set_Ptr_Tru = new DefBlock("Set Truth", 70, BlockType.Active, new Vector2I(2, 2), new Terminal(0, WireType.Void, TerminalType.Out, "After"), new Terminal(1, WireType.Bool, TerminalType.In, "Value"), new Terminal(2, WireType.BoolPtr, TerminalType.In, "Variable"), new Terminal(3, WireType.Void, TerminalType.In, "Before"));
            public static readonly DefBlock Set_Ptr_Obj = new DefBlock("Set Object", 74, BlockType.Active, new Vector2I(2, 2), new Terminal(0, WireType.Void, TerminalType.Out, "After"), new Terminal(1, WireType.Obj, TerminalType.In, "Value"), new Terminal(2, WireType.ObjPtr, TerminalType.In, "Variable"), new Terminal(3, WireType.Void, TerminalType.In, "Before"));
            #endregion
            #region list
            public static readonly DefBlock List_Num = new DefBlock("List Number", 82, BlockType.Pasive, new Vector2I(2, 2), new Terminal(0, WireType.FloatPtr, TerminalType.Out, "Element"), new Terminal(1, WireType.Float, TerminalType.In, "Index"), new Terminal(2, WireType.FloatPtr, TerminalType.In, "Variable"));
            public static readonly DefBlock List_Vec = new DefBlock("List Vector", 461, BlockType.Pasive, new Vector2I(2, 2), new Terminal(0, WireType.ObjPtr, TerminalType.Out, "Element"), new Terminal(1, WireType.Float, TerminalType.In, "Index"), new Terminal(2, WireType.ObjPtr, TerminalType.In, "Variable"));
            public static readonly DefBlock List_Rot = new DefBlock("List Rotation", 465, BlockType.Pasive, new Vector2I(2, 2), new Terminal(0, WireType.RotPtr, TerminalType.Out, "Element"), new Terminal(1, WireType.Float, TerminalType.In, "Index"), new Terminal(2, WireType.RotPtr, TerminalType.In, "Variable"));
            public static readonly DefBlock List_Tru = new DefBlock("List Truth", 469, BlockType.Pasive, new Vector2I(2, 2), new Terminal(0, WireType.BoolPtr, TerminalType.Out, "Element"), new Terminal(1, WireType.Float, TerminalType.In, "Index"), new Terminal(2, WireType.BoolPtr, TerminalType.In, "Variable"));
            public static readonly DefBlock List_Obj = new DefBlock("List Object", 86, BlockType.Pasive, new Vector2I(2, 2), new Terminal(0, WireType.ObjPtr, TerminalType.Out, "Element"), new Terminal(1, WireType.Float, TerminalType.In, "Index"), new Terminal(2, WireType.ObjPtr, TerminalType.In, "Variable"));
            #endregion
            public static readonly DefBlock PlusPlusFloat = new DefBlock("Increase Number", 556, BlockType.Active, new Vector2I(2, 1), new Terminal(0, WireType.Void, TerminalType.Out, "After"), new Terminal(1, WireType.Float, TerminalType.In, "Variable"), new Terminal(2, WireType.Void, TerminalType.In, "Before"));
            public static readonly DefBlock MinusMinusFloat = new DefBlock("Decrease Number", 558, BlockType.Active, new Vector2I(2, 1), new Terminal(0, WireType.Void, TerminalType.Out, "After"), new Terminal(1, WireType.Float, TerminalType.In, "Variable"), new Terminal(2, WireType.Void, TerminalType.In, "Before"));
        }

        // used by terminals.json
        private class TerminalInfo
        {
            public ushort Id;
            public Vector3I[] Positions = null!;
        }
    }
}
