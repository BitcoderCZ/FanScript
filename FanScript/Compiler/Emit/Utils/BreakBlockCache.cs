using System.Diagnostics.CodeAnalysis;
using FanScript.FCInfo;

namespace FanScript.Compiler.Emit.Utils
{
    internal sealed class BreakBlockCache
    {
        private const int MaxUsesPerAxis = 3;

        private Block? _lastBlock;
        private bool _invalid = false;

        private int _xUseCount;
        private int _yUseCount;
        private int _zUseCount;

        public BreakBlockCache()
            : this(null)
        {
        }

        public BreakBlockCache(Block? breakBlock)
        {
            _lastBlock = ValidateBlock(breakBlock);
        }

        public void SetNewBlock(Block breakBlock)
        {
            _lastBlock = ValidateBlock(breakBlock);
            _invalid = false;
            _xUseCount = 0;
            _yUseCount = 0;
            _zUseCount = 0;
        }

        public bool CanGet()
            => _lastBlock is not null && !_invalid;

        public bool TryGet([NotNullWhen(true)] out Block? breakBlock)
        {
            if (CheckAndInc(0) &&
                CheckAndInc(1) &&
                CheckAndInc(2))
            {
                breakBlock = _lastBlock;
                return true;
            }
            else
            {
                breakBlock = null;
                return false;
            }
        }

        public bool TryGetAxis(int axis, [NotNullWhen(true)] out IEmitStore? emitStore)
        {
            if (axis < 0 || axis > 2)
            {
                throw new ArgumentOutOfRangeException(nameof(axis));
            }

            if (CheckAndInc(axis))
            {
                // x - 2, y - 1, z - 0
                emitStore = BasicEmitStore.COut(_lastBlock, _lastBlock.Type.TerminalArray[2 - axis]);
                return true;
            }
            else
            {
                emitStore = null;
                return false;
            }
        }

        private static Block? ValidateBlock(Block? breakBlock)
        {
            if (breakBlock is null)
            {
                return breakBlock;
            }

            BlockDef type = breakBlock.Type;
            return type == Blocks.Math.Break_Vector || type == Blocks.Math.Break_Rotation
                ? breakBlock
                : throw new ArgumentException(nameof(breakBlock), $"{nameof(breakBlock)} must be {nameof(Blocks.Math.Break_Vector)} or {nameof(Blocks.Math.Break_Rotation)}");
        }

        [MemberNotNullWhen(true, nameof(_lastBlock))]
        private bool CheckAndInc(int axis)
        {
            if (_lastBlock is null || _invalid)
            {
                return false;
            }

#pragma warning disable SA1119 // Statement should not use unnecessary parenthesis - but they are necesarry here... wtf
            _invalid = !(axis switch
            {
                0 => _xUseCount++ < MaxUsesPerAxis,
                1 => _yUseCount++ < MaxUsesPerAxis,
                2 => _zUseCount++ < MaxUsesPerAxis,
                _ => false,
            });
#pragma warning restore SA1119

            return !_invalid;
        }
    }
}
