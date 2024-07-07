﻿
using FanScript.FCInfo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FanScript.Compiler.Emit
{
    internal class EmitStore
    {
        public static EmitStore Default => new EmitStore();

        public Block In;
        public Terminal InTerminal;
        public Block[] Out;
        public Terminal[] OutTerminal;

        private EmitStore()
        {
            In = null!;
            InTerminal = null!;
            Out = null!;
            OutTerminal = null!;
        }

        public EmitStore(Block block)
            : this(block, block.Type.Before, block, block.Type.After)
        { }
        public EmitStore(Block _in, Terminal _inTerminal, Block _out, Terminal _outTerminal)
        {
            In = _in;
            InTerminal = _inTerminal;
            Out = [_out];
            OutTerminal = [_outTerminal];
        }
        public EmitStore(Block _in, Terminal _inTerminal, Block[] _out, Terminal[] _outTerminals, bool b)
        {
            if (_out is not null && _outTerminals is not null && _out.Length != _outTerminals.Length)
                throw new ArgumentException($"_outTerminals.Length ({_outTerminals.Length}) must be equal to _out.Length ({_out.Length})", "_outTerminals");

            In = _in;
            InTerminal = _inTerminal;
            Out = _out!;
            OutTerminal = _outTerminals!;
        }

        /// <summary>
        /// Creates an <see cref="EmitStore"/> with <see cref="In"/> and <see cref="InTerminal"/> assigned
        /// </summary>
        /// <param name="block"></param>
        /// <param name="terminal"></param>
        /// <returns></returns>
        public static EmitStore CIn(Block block, Terminal terminal)
            => new EmitStore(block, terminal, null!, null!);

        /// <summary>
        /// Creates an <see cref="EmitStore"/> with <see cref="Out"/> and <see cref="OutTerminal"/> assigned
        /// </summary>
        /// <param name="block"></param>
        /// <param name="terminal"></param>
        /// <returns></returns>
        public static EmitStore COut(Block block, Terminal terminal)
            => new EmitStore(null!, null!, block, terminal);
    }
}
