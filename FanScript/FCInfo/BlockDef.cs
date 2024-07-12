using MathUtils.Vectors;
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
        public readonly Terminal[] Terminals;
        public Terminal After => Type == BlockType.Active ? Terminals[0] : throw new InvalidOperationException("Only active blocks have Before and After");

        public BlockDef(string _name, ushort _id, BlockType _type, Vector2I _size, params Terminal[] _terminals)
        {
            Name = _name;
            Id = _id;
            Type = _type;
            Size = _size;

            Terminals = _terminals ?? Array.Empty<Terminal>();
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
