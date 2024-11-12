using System.Globalization;
using System.Text;
using FanScript.Compiler.Exceptions;
using FanScript.Utils;
using MathUtils.Vectors;

/*
function con(nChr) {
                  return nChr > 64 && nChr < 91
                    ? nChr - 65
                    : nChr > 96 && nChr < 123
                    ? nChr - 71
                    : nChr > 47 && nChr < 58
                    ? nChr + 4
                    : nChr === 43
                    ? 62
                    : nChr === 47
                    ? 63
                    : 0;
                }
                function b64d(s, nBlocksSize) {
                  const en = s.replace(/[^A-Za-z0-9+/]/g, "");
                  const inL = en.length;
                  const outL = nBlocksSize
                    ? Math.ceil(((inL * 3 + 1) >> 2) / nBlocksSize) * nBlocksSize
                    : (inL * 3 + 1) >> 2;
                  const bs = new Uint8Array(outL);
                  let m3;
                  let m4;
                  let nUint24 = 0;
                  let outIdx = 0;
                  for (let nInIdx = 0; nInIdx < inL; nInIdx++) {
                    m4 = nInIdx & 3;
                    nUint24 |= con(en.charCodeAt(nInIdx)) << (6 * (3 - m4));
                    if (m4 === 3 || inL - nInIdx === 1) {
                      m3 = 0;
                      while (m3 < 3 && outIdx < outL) {
                        bs[outIdx] = (nUint24 >>> ((16 >>> m3) & 24)) & 255;
                        m3++;
                        outIdx++;
                      }
                      nUint24 = 0;
                    }
                  }
                  return bs;
                }
                var c = "[CODE]";
                var bytes = b64d(c);
                var view = new DataView(bytes.buffer);
                var encoder = new TextDecoder();
                clearLog();
                var i = 0;
                var setLL = view.getInt32(i, true);
                log("Numb blocks: " + setLL);
                i+=4;
                for(var j=0; j < setLL; j++) {
                    var x = view.getInt32(i, true);
                    var y = view.getInt32(i+4, true);
                    var z = view.getInt32(i+8, true);
                    var bId = view.getInt32(i+12, true);
                    i += 16;
                    log("Set block: id: " + bId + " pos: x:" + x + " y: " + y + " z: " + z);
                    setBlock(x, y, z, bId);
                }
                log("Set blocks");
                updateChanges();
                log("Updated changes")
                var setVLL = view.getInt32(i, true);
                log("Numb values: " + setVLL);
                i+=4;
                for(var j=0; j < setVLL; j++) {
                    let x = view.getInt32(i, true);
                    let y = view.getInt32(i+4, true);
                    let z = view.getInt32(i+8, true);
                    let vIndex = view.getInt32(i+12, true);
                    let vt = view.getInt32(i+16, true);
                    i += 20;
                    var v;
                    if (vt == 0){
                        v = view.getFloat32(i, true);
                        i +=4;
                    } else if (vt == 1) {
                        var vl = view.getInt32(i, true);
                        i +=4;
                        var bs = bytes.subarray(i, i + vl);
                        i += vl;
                        v = encoder.decode(bs);
                    } else if (vt == 2) {
                        v = [view.getFloat32(i, true),view.getFloat32(i+4, true),view.getFloat32(i+8, true)];
                        i += 12;
                    }
                    log("Set value: index: " + vIndex + " type: " + vt + " pos: x:" + x + " y: " + y + " z: " + z);
                    setBlockValue(x, y, z, vIndex, v);
                }
                log("Set values");
                updateChanges();
                log("Updated changes");
                var cLL = view.getInt32(i, true);
                log("Numb connections: " + cLL);
                i+=4;
                for(var j=0; j < cLL; j++) {
                    var x1 = view.getInt32(i, true);
                    var y1 = view.getInt32(i+4, true);
                    var z1 = view.getInt32(i+8, true);
                    var x2 = view.getInt32(i+12, true);
                    var y2 = view.getInt32(i+16, true);
                    var z2 = view.getInt32(i+20, true);
                    var ti1 = view.getInt32(i+24, true);
                    var ti2 = view.getInt32(i+28, true);
                    i += 32;
                    log("connect, from: " + x1 + ", " + y1 + ", " + z1 + " from index: " + ti1 + " to: " + x2 + ", " + y2 + ", " + z2 + " to index: " + ti2)
                    connect(x1, y1, z1, ti1, x2, y2, z2, ti2);
                }
                log("Connected");
                updateChanges();
                log("Updated changes, done");
 */

namespace FanScript.Compiler.Emit.BlockBuilders;

public class EditorScriptBlockBuilder : BlockBuilder
{
    public enum Compression
    {
        None,
        Base64,
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="startPos"></param>
    /// <param name="iArgs">Must be null or <see cref="Args"/></param>
    /// <returns>The editor script (js) to build the code</returns>
    /// <exception cref="Exception"></exception>
    public override string Build(Vector3I startPos, IArgs? iArgs)
    {
        Args args = (iArgs as Args) ?? Args.Default;

        Block[] blocks = PreBuild(startPos, sortByPos: true); // sortByPos is requred because of a bug that sometimes deletes objects if they are placed from +Z to -Z, even if they aren't overlaping

        return args.Compression switch
        {
            Compression.None => BuildNormal(blocks),
            Compression.Base64 => BuildBase64(blocks),
            _ => throw new UnknownEnumValueException<Compression>(args.Compression),
        };
    }

    private string BuildNormal(Block[] blocks)
    {
        StringBuilder builder = new StringBuilder();

        if (blocks.Length > 0 && blocks[0].Pos != Vector3I.Zero)
        {
            builder.AppendLine($"setBlock(0,0,0,1);"); // make sure the level origin doesn't shift
        }

        for (int i = 0; i < blocks.Length; i++)
        {
            Block block = blocks[i];
            builder.AppendLine($"setBlock({block.Pos.X},{block.Pos.Y},{block.Pos.Z},{block.Type.Id});");
        }

        builder.AppendLine("updateChanges();");

        for (int i = 0; i < values.Count; i++)
        {
            ValueRecord set = values[i];

            string val = set.Value switch
            {
                null => "null",
                byte b => b.ToString(CultureInfo.InvariantCulture),
                ushort s => s.ToString(CultureInfo.InvariantCulture),
                float f => f.ToString(CultureInfo.InvariantCulture),
                string s => $"\"{s}\"",
                Vector3F v => $"[{v.X.ToString(CultureInfo.InvariantCulture)},{v.Y.ToString(CultureInfo.InvariantCulture)},{v.Z.ToString(CultureInfo.InvariantCulture)}]",
                Rotation r => $"[{r.Value.X.ToString(CultureInfo.InvariantCulture)},{r.Value.Y.ToString(CultureInfo.InvariantCulture)},{r.Value.Z.ToString(CultureInfo.InvariantCulture)}]",
                _ => throw new Exception($"Cannot convert object of type: '{set.Value.GetType()}' to Block Value"),
            };

            builder.AppendLine($"setBlockValue({set.Block.Pos.X},{set.Block.Pos.Y},{set.Block.Pos.Z},{set.ValueIndex},{val});");
        }

        builder.AppendLine("updateChanges();");

        for (int i = 0; i < connections.Count; i++)
        {
            ConnectionRecord con = connections[i];
            Vector3I from = con.From.Pos;
            int fromTerminal = con.From.TerminalIndex;
            Vector3I to = con.To.Pos;
            int toTerminal = con.To.TerminalIndex;
            builder.AppendLine($"connect({from.X},{from.Y},{from.Z},{fromTerminal},{to.X},{to.Y},{to.Z},{toTerminal});");
        }

        builder.AppendLine("updateChanges();");

        return builder.ToString();
    }

    private string BuildBase64(Block[] blocks)
    {
        byte[] bufer;
        using (MemoryStream stream = new MemoryStream())
        using (EditorScriptBase64Writer writer = new EditorScriptBase64Writer(stream))
        {
            bool insertBlockAtZero = blocks.Length > 0 && blocks[0].Pos != Vector3I.Zero;

            writer.WriteInt32(blocks.Length + (insertBlockAtZero ? 1 : 0));

            if (insertBlockAtZero)
            {
                // make sure the level origin doesn't shift
                writer.WriteInt32(0);
                writer.WriteInt32(0);
                writer.WriteInt32(0);
                writer.WriteInt32(1);
            }

            for (int i = 0; i < blocks.Length; i++)
            {
                Block block = blocks[i];
                writer.WriteInt32(block.Pos.X);
                writer.WriteInt32(block.Pos.Y);
                writer.WriteInt32(block.Pos.Z);
                writer.WriteInt32(block.Type.Id);
            }

            writer.WriteInt32(values.Count);
            for (int i = 0; i < values.Count; i++)
            {
                ValueRecord val = values[i];
                writer.WriteInt32(val.Block.Pos.X);
                writer.WriteInt32(val.Block.Pos.Y);
                writer.WriteInt32(val.Block.Pos.Z);
                writer.WriteInt32(val.ValueIndex);

                if (val.Value is byte numB)
                {
                    writer.WriteInt32(0);
                    writer.WriteSingle(numB); // javascript only has float type, so no reason to add a value type (other than saving space)
                }
                else if (val.Value is ushort numS)
                {
                    writer.WriteInt32(0);
                    writer.WriteSingle(numS); // javascript only has float type, so no reason to add a value type (other than saving space)
                }
                else if (val.Value is float numF)
                {
                    writer.WriteInt32(0);
                    writer.WriteSingle(numF);
                }
                else if (val.Value is string s)
                {
                    writer.WriteInt32(1);
                    writer.WriteString(s);
                }
                else if (val.Value is Vector3F v3)
                {
                    writer.WriteInt32(2);
                    writer.WriteSingle(v3.X);
                    writer.WriteSingle(v3.Y);
                    writer.WriteSingle(v3.Z);
                }
                else if (val.Value is Rotation rot)
                {
                    writer.WriteInt32(2);
                    writer.WriteSingle(rot.Value.X);
                    writer.WriteSingle(rot.Value.Y);
                    writer.WriteSingle(rot.Value.Z);
                }
                else if (val.Value is bool b)
                {
                    writer.WriteInt32(3);
                    writer.WriteInt32(b ? 1 : 0);
                }
                else
                {
                    throw new Exception($"Unsuported Value type: {val.Value.GetType()}");
                }
            }

            writer.WriteInt32(connections.Count);
            for (int i = 0; i < connections.Count; i++)
            {
                ConnectionRecord con = connections[i];
                Vector3I from = con.From.Pos;
                int fromTerminal = con.From.TerminalIndex;
                Vector3I to = con.To.Pos;
                int toTerminal = con.To.TerminalIndex;
                writer.WriteInt32(from.X);
                writer.WriteInt32(from.Y);
                writer.WriteInt32(from.Z);
                writer.WriteInt32(to.X);
                writer.WriteInt32(to.Y);
                writer.WriteInt32(to.Z);
                writer.WriteInt32(fromTerminal);
                writer.WriteInt32(toTerminal);
            }

            bufer = new byte[stream.Length];
            stream.Position = 0;
            stream.Read(bufer, 0, bufer.Length);
        }

        string code = Convert.ToBase64String(bufer);
        return
            """
            var t=function(t,e){const n=t.replace(/[^A-Za-z0-9+/]/g,""),a=n.length,g=e?Math.ceil((3*a+1>>2)/e)*e:3*a+1>>2,r=new Uint8Array(g);let I,o,l=0,c=0;for(let t=0;t<a;t++)if(o=3&t,l|=((f=n.charCodeAt(t))>64&&f<91?f-65:f>96&&f<123?f-71:f>47&&f<58?f+4:43===f?62:47===f?63:0)<<6*(3-o),3===o||a-t==1){for(I=0;I<3&&c<g;)r[c]=l>>>(16>>>I&24)&255,I++,c++;l=0}var f;return r}("[CODE]"),e=new DataView(t.buffer),n=new TextDecoder,a=0,g=e.getInt32(a,!0);a+=4;for(var r=0;r<g;r++){var I=e.getInt32(a,!0),o=e.getInt32(a+4,!0),l=e.getInt32(a+8,!0),c=e.getInt32(a+12,!0);a+=16,setBlock(I,o,l,c)}updateChanges();var f=e.getInt32(a,!0);a+=4;for(r=0;r<f;r++){let g=e.getInt32(a,!0),r=e.getInt32(a+4,!0),I=e.getInt32(a+8,!0),o=e.getInt32(a+12,!0),l=e.getInt32(a+16,!0);var v;if(a+=20,0==l)v=e.getFloat32(a,!0),a+=4;else if(1==l){var s=e.getInt32(a,!0);a+=4;var u=t.subarray(a,a+s);a+=s,v=n.decode(u)}else 2==l&&(v=[e.getFloat32(a,!0),e.getFloat32(a+4,!0),e.getFloat32(a+8,!0)],a+=12);setBlockValue(g,r,I,o,v)}updateChanges();var d=e.getInt32(a,!0);a+=4;for(r=0;r<d;r++){var i=e.getInt32(a,!0),h=e.getInt32(a+4,!0),C=e.getInt32(a+8,!0),p=e.getInt32(a+12,!0),w=e.getInt32(a+16,!0),F=e.getInt32(a+20,!0),A=e.getInt32(a+24,!0),D=e.getInt32(a+28,!0);a+=32,connect(i,h,C,A,p,w,F,D)}updateChanges();
            """
            .Replace("[CODE]", code);
    }

    public sealed class Args : IArgs
    {
        public static readonly Args Default = new Args(Compression.Base64);

        public readonly Compression Compression;

        public Args(Compression compression)
        {
            Compression = compression;
        }
    }
}
