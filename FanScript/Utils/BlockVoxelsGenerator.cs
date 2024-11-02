using System.Runtime.CompilerServices;
using FancadeLoaderLib;
using MathUtils.Vectors;

namespace FanScript.Utils
{
    public class BlockVoxelsGenerator
    {
        private readonly Dictionary<Vector3B, Voxel[]> _blocks = [];

        private BlockVoxelsGenerator()
        {
        }

        private delegate void LoopDelegate(ref Voxel voxel);

        public static unsafe IEnumerable<KeyValuePair<Vector3B, Voxel[]>> CreateScript(Vector2I sizeInBlocks)
        {
            if (sizeInBlocks.X < 1 || sizeInBlocks.Y < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(sizeInBlocks));
            }

            Vector3I sizeInVoxels = new Vector3I((sizeInBlocks.X * 8) - 1, 3, (sizeInBlocks.Y * 8) - 1);

            byte gray4 = (byte)FcColor.Gray4;
            byte gray3 = (byte)FcColor.Gray3;

            BlockVoxelsGenerator generator = new BlockVoxelsGenerator();

            generator.Fill(Vector3B.Zero, sizeInVoxels, FcColor.Black);

            generator.Loop(new Vector3I(1, 2, 1), new Vector3I(sizeInVoxels.X - 1, 3, sizeInVoxels.Z - 1), (ref Voxel voxel) =>
            {
                voxel.Colors[2] = gray4;
            });

            generator.GetVoxel(new Vector3I(sizeInVoxels.X - 1, 2, 0)).Colors[2] = gray4;
            generator.GetVoxel(new Vector3I(0, 2, sizeInVoxels.Z - 1)).Colors[2] = gray4;

            generator.Loop(new Vector3I(1, 2, sizeInVoxels.Z - 1), new Vector3I(sizeInVoxels.X, 3, sizeInVoxels.Z), (ref Voxel voxel) =>
            {
                voxel.Colors[2] = gray3;
            });

            generator.Loop(new Vector3I(sizeInVoxels.X - 1, 2, 1), new Vector3I(sizeInVoxels.X, 3, sizeInVoxels.Z), (ref Voxel voxel) =>
            {
                voxel.Colors[2] = gray3;
            });

            return generator._blocks;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Index(Vector3I pos)
            => Index(pos.X, pos.Y, pos.Z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Index(int x, int y, int z)
            => x + (y * 8) + (z * 8 * 8);

        private Voxel[] GetBlock(Vector3B pos)
        {
            if (!_blocks.TryGetValue(pos, out Voxel[]? voxels))
            {
                voxels = new Voxel[Prefab.NumbVoxels];
                _blocks.Add(pos, voxels);
            }

            return voxels;
        }

        private ref Voxel GetVoxel(Vector3I pos)
        {
            Vector3I blockPos = pos / 8;
            Vector3I voxelBlockPos = blockPos * 8;
            Vector3I inBlockPos = pos - voxelBlockPos;

            return ref GetBlock((Vector3B)blockPos)[Index(inBlockPos)];
        }

        private unsafe void Fill(Vector3I from, Vector3I to, FcColor color)
        {
            Voxel voxel = default;
            byte colByte = (byte)color;
            voxel.Colors[0] = colByte;
            voxel.Colors[1] = colByte;
            voxel.Colors[2] = colByte;
            voxel.Colors[3] = colByte;
            voxel.Colors[4] = colByte;
            voxel.Colors[5] = colByte;

            Fill(from, to, voxel);
        }

        private void Fill(Vector3I from, Vector3I to, Voxel voxel)
        {
            if (from.X > to.X)
            {
                int temp = from.X;
                from.X = to.X;
                to.X = temp;
            }

            if (from.Y > to.Y)
            {
                int temp = from.Y;
                from.Y = to.Y;
                to.Y = temp;
            }

            if (from.X > to.X)
            {
                int temp = from.Z;
                from.Z = to.Z;
                to.Z = temp;
            }

            Vector3I fromBlock = from / 8;
            Vector3I toBlock = to / 8;

            for (int bz = fromBlock.Z; bz <= toBlock.Z; bz++)
            {
                for (int by = fromBlock.Y; by <= toBlock.Y; by++)
                {
                    for (int bx = 0; bx <= toBlock.X; bx++)
                    {
                        Vector3B blockPos = new Vector3B(bx, by, bz);
                        Vector3I voxelPos = blockPos * 8;
                        Voxel[] block = GetBlock(blockPos);

                        Vector3I min = Vector3I.Max(Vector3I.Zero, from - voxelPos);
                        Vector3I max = Vector3I.Min(new Vector3I(8, 8, 8), to - voxelPos);

                        for (int z = min.Z; z < max.Z; z++)
                        {
                            for (int y = min.Y; y < max.Y; y++)
                            {
                                for (int x = min.X; x < max.X; x++)
                                {
                                    block[Index(x, y, z)] = voxel;
                                }
                            }
                        }
                    }
                }
            }
        }

        private void Loop(Vector3I from, Vector3I to, LoopDelegate action)
        {
            if (from.X > to.X)
            {
                int temp = from.X;
                from.X = to.X;
                to.X = temp;
            }

            if (from.Y > to.Y)
            {
                int temp = from.Y;
                from.Y = to.Y;
                to.Y = temp;
            }

            if (from.X > to.X)
            {
                int temp = from.Z;
                from.Z = to.Z;
                to.Z = temp;
            }

            Vector3I fromBlock = from / 8;
            Vector3I toBlock = to / 8;

            for (int bz = fromBlock.Z; bz <= toBlock.Z; bz++)
            {
                for (int by = fromBlock.Y; by <= toBlock.Y; by++)
                {
                    for (int bx = 0; bx <= toBlock.X; bx++)
                    {
                        Vector3B blockPos = new Vector3B(bx, by, bz);
                        Vector3I voxelPos = blockPos * 8;
                        Voxel[] block = GetBlock(blockPos);

                        Vector3I min = Vector3I.Max(Vector3I.Zero, from - voxelPos);
                        Vector3I max = Vector3I.Min(new Vector3I(8, 8, 8), to - voxelPos);

                        for (int z = min.Z; z < max.Z; z++)
                        {
                            for (int y = min.Y; y < max.Y; y++)
                            {
                                for (int x = min.X; x < max.X; x++)
                                {
                                    action(ref block[Index(x, y, z)]);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
