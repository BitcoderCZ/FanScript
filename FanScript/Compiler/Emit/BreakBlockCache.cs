using FanScript.FCInfo;
using System.Diagnostics.CodeAnalysis;

namespace FanScript.Compiler.Emit
{
    internal sealed class BreakBlockCache
    {
        private const int maxUsesPerAxis = 3;

        private Block? lastBlock;
        private bool invalid = false;

        private int xUseCount;
        private int yUseCount;
        private int zUseCount;

        public BreakBlockCache()
            : this(null)
        {
        }
        public BreakBlockCache(Block? breakBlock)
        {
            lastBlock = validateBlock(breakBlock);
        }

        public void SetNewBlock(Block breakBlock)
        {
            lastBlock = validateBlock(breakBlock);
            invalid = false;
            xUseCount = 0;
            yUseCount = 0;
            zUseCount = 0;
        }

        public bool CanGet()
            => lastBlock is not null && !invalid;

        public bool TryGet([NotNullWhen(true)] out Block? breakBlock)
        {
            if (checkAndInc(0) &&
                checkAndInc(1) &&
                checkAndInc(2))
            {
                breakBlock = lastBlock;
                return true;
            }
            else
            {
                breakBlock = null;
                return false;
            }
        }

        public bool TryGetAxis(int axis, [NotNullWhen(true)] out EmitStore? emitStore)
        {
            if (axis < 0 || axis > 2)
                throw new ArgumentOutOfRangeException(nameof(axis));

            if (checkAndInc(axis))
            {
                // x - 2, y - 1, z - 0
                emitStore = BasicEmitStore.COut(lastBlock, lastBlock.Type.Terminals[2 - axis]);
                return true;
            }
            else
            {
                emitStore = null;
                return false;
            }
        }

        private Block? validateBlock(Block? breakBlock)
        {
            if (breakBlock is null)
                return breakBlock;

            BlockDef type = breakBlock.Type;
            if (type == Blocks.Math.Break_Vector || type == Blocks.Math.Break_Rotation)
                return breakBlock;
            else
                throw new ArgumentException(nameof(breakBlock), $"{nameof(breakBlock)} must be {nameof(Blocks.Math.Break_Vector)} or {nameof(Blocks.Math.Break_Rotation)}");
        }

        [MemberNotNullWhen(true, nameof(lastBlock))]
        private bool checkAndInc(int axis)
        {
            if (lastBlock is null || invalid)
                return false;

            invalid = !(axis switch
            {
                0 => xUseCount++ < maxUsesPerAxis,
                1 => yUseCount++ < maxUsesPerAxis,
                2 => zUseCount++ < maxUsesPerAxis,
                _ => false,
            });

            return !invalid;
        }
    }
}
