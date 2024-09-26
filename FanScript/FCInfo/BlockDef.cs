using FanScript.Utils;
using MathUtils.Vectors;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace FanScript.FCInfo
{
    /// <summary>
    /// Definition of a Fancade block
    /// </summary>
    public class BlockDef
    {
        public readonly string Name;
        public readonly ushort Id;
        public readonly BlockType Type;
        public readonly Vector2I Size;

        public bool IsGroup => Size != Vector2I.One;

        public Terminal Before => Type == BlockType.Active ? TerminalArray[^1] : throw new InvalidOperationException("Only active blocks have Before and After");
        public readonly ImmutableArray<Terminal> TerminalArray;
        public readonly Indexable<string, Terminal> Terminals;
        public Terminal After => Type == BlockType.Active ? TerminalArray[0] : throw new InvalidOperationException("Only active blocks have Before and After");

        public BlockDef(string name, ushort id, BlockType type, Vector2I size, params Terminal[] terminals)
        {
            Name = name;
            Id = id;
            Type = type;
            Size = size;

            TerminalArray = terminals is null ? ImmutableArray<Terminal>.Empty : terminals.ToImmutableArray();

            initTerminals();

            this.Terminals = new Indexable<string, Terminal>(termName =>
            {
                for (int i = 0; i < TerminalArray.Length; i++)
                    if (TerminalArray[i].Name == termName)
                        return TerminalArray[i];

                throw new KeyNotFoundException($"Terminal '{termName}' wasn't found.");
            });
        }

        private void initTerminals()
        {
            int off = Type == BlockType.Active ? 1 : 0;

            int countIn = 0;
            int countOut = 0;

            // count in and out terminals
            for (int i = off; i < TerminalArray.Length - off; i++)
            {
                if (TerminalArray[i].Type == TerminalType.In)
                    countIn++;
                else
                    countOut++;
            }

            // if a block has less/more in/out terminals, one of the sides will start higher
            countIn = Size.Y - countIn;
            countOut = Size.Y - countOut;

            int outXPos = Size.X * 8 - 2;

            for (int i = off; i < TerminalArray.Length - off; i++)
            {
                Terminal terminal = TerminalArray[i];

                if (terminal.Type == TerminalType.In)
                    terminal.Init(i, new Vector3I(0, 1, countIn++ * 8 + 3));
                else
                    terminal.Init(i, new Vector3I(outXPos, 1, countOut++ * 8 + 3));
            }

            if (Type == BlockType.Active)
            {
                After.Init(0, new Vector3I(3, 1, 0));
                Before.Init(TerminalArray.Length - 1, new Vector3I(3, 1, Size.Y * 8 - 2));
            }
        }

        public Terminal GetTerminal(string name)
        {
            foreach (Terminal terminal in TerminalArray)
                if (terminal.Name == name)
                    return terminal;

            throw new KeyNotFoundException($"Terminal with name '{name}' isn't on block '{Name}'");
        }

        public override string ToString()
            => $"{{Name: {Name}, Id: {Id}, Type: {Type}, Size: {Size}}}";

        public static bool operator ==(BlockDef a, BlockDef b)
            => a?.Equals(b) ?? b is null;
        public static bool operator !=(BlockDef a, BlockDef b)
            => !a?.Equals(b) ?? b is not null;

        public override int GetHashCode()
            => Id;

        public override bool Equals(object? obj)
        {
            if (obj is BlockDef other)
                return other.Id == Id;
            else
                return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(BlockDef other)
            => other is null ? false : other.Id == Id;
    }
}
