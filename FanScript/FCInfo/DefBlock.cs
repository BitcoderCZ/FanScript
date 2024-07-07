﻿using MathUtils.Vectors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace FanScript.FCInfo
{
    public class DefBlock
    {
        public readonly string Name;
        public readonly ushort Id;
        public readonly BlockType Type;
        public readonly Vector2I Size;

        public Terminal Before => Terminals[Terminals.Length - 1];
        public readonly Terminal[] Terminals;
        public Terminal After => Terminals[0];

        public DefBlock(string _name, ushort _id, BlockType _type, Vector2I _size, params Terminal[] _terminals)
        {
            Name = _name;
            Id = _id;
            Type = _type;
            Size = _size;

            Terminals = _terminals ?? Array.Empty<Terminal>();
        }

        public override string ToString()
            => $"{{Name: {Name}, Id: {Id}, Type: {Type}, Size: {Size}}}";

        public static bool operator ==(DefBlock a, DefBlock b)
            => a?.Equals(b) ?? b is null;
        public static bool operator !=(DefBlock a, DefBlock b)
            => !a?.Equals(b) ?? b is not null;

        public override int GetHashCode()
            => Id;

        public override bool Equals(object? obj)
        {
            if (obj is DefBlock other)
                return Equals(other);
            else
                return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(DefBlock other)
            => other is null ? false : other.Id == Id;
    }
}
