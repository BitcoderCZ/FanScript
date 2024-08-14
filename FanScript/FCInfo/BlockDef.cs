using MathUtils.Vectors;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
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

        public Terminal Before => Type == BlockType.Active ? Terminals[Terminals.Length - 1] : throw new InvalidOperationException("Only active blocks have Before and After");
        public readonly ImmutableArray<Terminal> Terminals;
        public Terminal After => Type == BlockType.Active ? Terminals[0] : throw new InvalidOperationException("Only active blocks have Before and After");

        public BlockDef(string _name, ushort _id, BlockType _type, Vector2I _size, params Terminal[] _terminals)
        {
            Name = _name;
            Id = _id;
            Type = _type;
            Size = _size;

            Terminals = _terminals is null ? ImmutableArray<Terminal>.Empty : _terminals.ToImmutableArray();

            initTerminals();
        }

        private void initTerminals()
        {
            int off = Type == BlockType.Active ? 1 : 0;

            int countIn = 0;
            int countOut = 0;

            // count in and out terminals
            for (int i = off; i < Terminals.Length - off; i++)
            {
                if (Terminals[i].Type == TerminalType.In)
                    countIn++;
                else
                    countOut++;
            }

            // if a block has less/more in/out terminals, one of the sides will start higher
            countIn = Size.Y - countIn;
            countOut = Size.Y - countOut;

            int outXPos = Size.X * 8 - 2;

            for (int i = off; i < Terminals.Length - off; i++)
            {
                Terminal terminal = Terminals[i];

                if (terminal.Type == TerminalType.In)
                    terminal.Init(i, new Vector3I(0, 1, countIn++ * 8 + 3));
                else
                    terminal.Init(i, new Vector3I(outXPos, 1, countOut++ * 8 + 3));
            }

            if (Type == BlockType.Active)
            {
                After.Init(0, new Vector3I(3, 1, 0));
                Before.Init(Terminals.Length - 1, new Vector3I(3, 1, Size.Y * 8 - 2));
            }
        }

        public Terminal GetTerminal(string name)
        {
            foreach (Terminal terminal in Terminals)
                if (terminal.Name == name)
                    return terminal;

            throw new KeyNotFoundException($"Terminal with name '{name}' isn't on block '{Name}'");
        }

        public override string ToString()
            => $"{{LabelName: {Name}, Id: {Id}, Type: {Type}, Size: {Size}}}";

        public static bool operator ==(BlockDef a, BlockDef b)
            => a?.Equals(b) ?? b is null;
        public static bool operator !=(BlockDef a, BlockDef b)
            => !a?.Equals(b) ?? b is not null;

        public override int GetHashCode()
            => Id;

        public override bool Equals(object? obj)
        {
            if (obj is BlockDef other)
                return Equals(other);
            else
                return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(BlockDef other)
            => other is null ? false : other.Id == Id;
    }
}
