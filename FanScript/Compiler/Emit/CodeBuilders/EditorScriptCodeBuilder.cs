using FanScript.FCInfo;
using FanScript.Utils;
using MathUtils.Vectors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

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
i+=4;
for(var j=0; j < setLL; j++) {
    var x = view.getInt32(i, true);
    var y = view.getInt32(i+4, true);
    var bId = view.getInt32(i+8, true);
    i += 12;
    setBlock(x, 0, y, bId);
}
updateChanges();
var setVLL = view.getInt32(i, true);
i+=4;
for(var j=0; j < setVLL; j++) {
    var x = view.getInt32(i, true);
    var y = view.getInt32(i+4, true);
    var vIndex = view.getInt32(i+8, true);
    var vt = view.getInt32(i+12, true);
    i += 16;
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
    setBlockValue(x, 0, y, vIndex, v);
}
updateChanges();
var cLL = view.getInt32(i, true);
i+=4;
for(var j=0; j < cLL; j++) {
    var x1 = view.getInt32(i, true);
    var y1 = view.getInt32(i+4, true);
    var x2 = view.getInt32(i+8, true);
    var y2 = view.getInt32(i+12, true);
    var ti1 = view.getInt32(i+16, true);
    var ti2 = view.getInt32(i+20, true);
    i += 24;
    connect(x1, 0, y1, ti1, x2, 0, y2, ti2);
}
updateChanges();
 */

namespace FanScript.Compiler.Emit.CodeBuilders
{
    public class EditorScriptCodeBuilder : CodeBuilder
    {
        public override BuildPlatformInfo PlatformInfo => 0;

        public EditorScriptCodeBuilder(IBlockPlacer blockPlacer) : base(blockPlacer)
        {
        }

        public override object Build(Vector3I startPos, params object[] args)
        {
            PreBuild(startPos);

            Compression compression = Compression.Base64;

            if (args.Length > 0 && args[0] is Compression comp)
                compression = comp;

            switch (compression)
            {
                case Compression.None:
                    return buildNormal();
                case Compression.Base64:
                    return buildBase64();
                default:
                    throw new Exception($"Unsuported compression: {compression}");
            }
        }

        private string buildNormal()
        {
            StringBuilder builder = new StringBuilder();

            for (int i = 0; i < setBlocks.Count; i++)
            {
                SetBlock set = setBlocks[i];
                builder.AppendLine($"setBlock({set.Block.Pos.X},{set.Block.Pos.Y},{set.Block.Pos.Z}, {set.Id});");
            }

            builder.AppendLine("updateChanges();");

            for (int i = 0; i < setValues.Count; i++)
            {
                SetValue set = setValues[i];
                builder.AppendLine($"setBlockValue({set.Block.Pos.X},{set.Block.Pos.Y}, {set.Block.Pos.Z},{set.ValueIndex},{set.Value.ToBlockValue()});");
            }

            builder.AppendLine("updateChanges();");

            for (int i = 0; i < makeConnections.Count; i++)
            {
                MakeConnection con = makeConnections[i];
                builder.AppendLine($"connect({con.Block1.Pos.X},{con.Block1.Pos.Y}, {con.Block1.Pos.Z},{con.Terminal1.Index},{con.Block2.Pos.X},{con.Block2.Pos.Y}, {con.Block2.Pos.Z},{con.Terminal2.Index});");
            }

            builder.AppendLine("updateChanges();");

            return builder.ToString();
        }
        private string buildBase64()
        {
            byte[] bufer;
            using (MemoryStream stream = new MemoryStream())
            using (Utils.SaveWriter writer = new Utils.SaveWriter(stream))
            {
                writer.WriteInt32(setBlocks.Count);
                for (int i = 0; i < setBlocks.Count; i++)
                {
                    SetBlock val = setBlocks[i];
                    writer.WriteInt32(val.Block.Pos.X);
                    writer.WriteInt32(val.Block.Pos.Y);
                    writer.WriteInt32(val.Block.Pos.Z);
                    writer.WriteInt32(val.Id);
                }

                writer.WriteInt32(setValues.Count);
                for (int i = 0; i < setValues.Count; i++)
                {
                    SetValue val = setValues[i];
                    writer.WriteInt32(val.Block.Pos.X);
                    writer.WriteInt32(val.Block.Pos.Y);
                    writer.WriteInt32(val.Block.Pos.Z);
                    writer.WriteInt32(val.ValueIndex);

                    if (val.Value is float f)
                    {
                        writer.WriteInt32(0);
                        writer.WriteSingle(f);
                    }
                    else if (val.Value is string s)
                    {
                        writer.WriteInt32(1);
                        writer.WriteString(s);
                    }
                    else if (val.Value is Vector3 v3)
                    {
                        writer.WriteInt32(2);
                        writer.WriteSingle(v3.X);
                        writer.WriteSingle(v3.Y);
                        writer.WriteSingle(v3.Z);
                    }
                    else if (val.Value is bool b)
                    {
                        writer.WriteInt32(3);
                        writer.WriteInt32(b ? 1 : 0);
                    }
                    else
                        throw new Exception($"Unsuported Value type: {val.Value.GetType()}");
                }

                writer.WriteInt32(makeConnections.Count);
                for (int i = 0; i < makeConnections.Count; i++)
                {
                    MakeConnection val = makeConnections[i];
                    writer.WriteInt32(val.Block1.Pos.X);
                    writer.WriteInt32(val.Block1.Pos.Y);
                    writer.WriteInt32(val.Block1.Pos.Z);
                    writer.WriteInt32(val.Block2.Pos.X);
                    writer.WriteInt32(val.Block2.Pos.Y);
                    writer.WriteInt32(val.Block2.Pos.Z);
                    writer.WriteInt32(val.Terminal1.Index);
                    writer.WriteInt32(val.Terminal2.Index);
                }

                bufer = new byte[stream.Length];
                stream.Position = 0;
                stream.Read(bufer, 0, bufer.Length);
            }

            string code = Convert.ToBase64String(bufer);
            return @"function con(e){return e>64&&e<91?e-65:e>96&&e<123?e-71:e>47&&e<58?e+4:43===e?62:47===e?63:0}function b64d(e,t){let v=e.replace(/[^A-Za-z0-9+/]/g,""""),$=v.length,n=t?Math.ceil((3*$+1>>2)/t)*t:3*$+1>>2,w=new Uint8Array(n),g,r,I=0,a=0;for(let o=0;o<$;o++)if(r=3&o,I|=con(v.charCodeAt(o))<<6*(3-r),3===r||$-o==1){for(g=0;g<3&&a<n;)w[a]=I>>>(16>>>g&24)&255,g++,a++;I=0}return w}var c=""[CODE]"",bytes=b64d(c),view=new DataView(bytes.buffer),encoder=new TextDecoder;clearLog();var i=0,setLL=view.getInt32(i,!0);i+=4;for(var j=0;j<setLL;j++){var e=view.getInt32(i,!0),t=view.getInt32(i+4,!0),v=view.getInt32(i+8,!0),$=view.getInt32(i+12,!0);log(""ID: ""+$+"" pos: x:""+e+"" y: ""+t+"" z: ""+v),i+=16,setBlock(e,t,v,$)}updateChanges();var setVLL=view.getInt32(i,!0);i+=4;for(var j=0;j<setVLL;j++){var n,e=view.getInt32(i,!0),t=view.getInt32(i+4,!0),v=view.getInt32(i+8,!0),w=view.getInt32(i+12,!0),g=view.getInt32(i+16,!0);if(log(""vIndex: ""+w+"" valType: ""+g+"" pos: x:""+e+"" y: ""+t+"" z: ""+v),i+=20,0==g)n=view.getFloat32(i,!0),i+=4;else if(1==g){var r=view.getInt32(i,!0);i+=4;var I=bytes.subarray(i,i+r);i+=r,n=encoder.decode(I)}else 2==g?(n=[view.getFloat32(i,!0),view.getFloat32(i+4,!0),view.getFloat32(i+8,!0)],i+=12):3==g&&(n=1==view.getInt32(i,!0),i+=4);setBlockValue(e,t,v,w,n)}updateChanges();var cLL=view.getInt32(i,!0);i+=4;for(var j=0;j<cLL;j++){var a=view.getInt32(i,!0),o=view.getInt32(i+4,!0),_=view.getInt32(i+8,!0),f=view.getInt32(i+12,!0),l=view.getInt32(i+16,!0),s=view.getInt32(i+20,!0),L=view.getInt32(i+24,!0),d=view.getInt32(i+28,!0);i+=32,connect(a,o,_,L,f,l,s,d)}updateChanges();"
                .Replace("[CODE]", code);
        }

        public enum Compression
        {
            None,
            Base64,
        }
    }
}
