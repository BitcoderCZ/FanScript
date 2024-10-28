using FancadeLoaderLib;
using MathUtils.Vectors;
using System.Runtime.CompilerServices;

namespace FanScript.Utils
{
    public class BlockVoxelsGenerator
    {
        private delegate void LoopDelegate(ref Voxel voxel);

        private readonly Dictionary<Vector3B, Voxel[]> blocks = new();

        private BlockVoxelsGenerator()
        {
        }

        private Voxel[] getBlock(Vector3B pos)
        {
            if (!blocks.TryGetValue(pos, out Voxel[]? voxels))
            {
                voxels = new Voxel[Prefab.NumbVoxels];
                blocks.Add(pos, voxels);
            }

            return voxels;
        }

        private ref Voxel getVoxel(Vector3I pos)
        {
            Vector3I blockPos = pos / 8;
            Vector3I voxelBlockPos = blockPos * 8;
            Vector3I inBlockPos = pos - voxelBlockPos;

            return ref getBlock((Vector3B)blockPos)[index(inBlockPos)];
        }

        private unsafe void fill(Vector3I from, Vector3I to, FcColor color)
        {
            Voxel voxel = new Voxel();
            byte colByte = (byte)color;
            voxel.Colors[0] = colByte;
            voxel.Colors[1] = colByte;
            voxel.Colors[2] = colByte;
            voxel.Colors[3] = colByte;
            voxel.Colors[4] = colByte;
            voxel.Colors[5] = colByte;

            fill(from, to, voxel);
        }
        private void fill(Vector3I from, Vector3I to, Voxel voxel)
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
                        Voxel[] block = getBlock(blockPos);

                        Vector3I min = Vector3I.Max(Vector3I.Zero, from - voxelPos);
                        Vector3I max = Vector3I.Min(new Vector3I(8, 8, 8), to - voxelPos);

                        for (int z = min.Z; z < max.Z; z++)
                        {
                            for (int y = min.Y; y < max.Y; y++)
                            {
                                for (int x = min.X; x < max.X; x++)
                                    block[index(x, y, z)] = voxel;
                            }
                        }
                    }
                }
            }
        }

        private void loop(Vector3I from, Vector3I to, LoopDelegate action)
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
                        Voxel[] block = getBlock(blockPos);

                        Vector3I min = Vector3I.Max(Vector3I.Zero, from - voxelPos);
                        Vector3I max = Vector3I.Min(new Vector3I(8, 8, 8), to - voxelPos);

                        for (int z = min.Z; z < max.Z; z++)
                        {
                            for (int y = min.Y; y < max.Y; y++)
                            {
                                for (int x = min.X; x < max.X; x++)
                                    action(ref block[index(x, y, z)]);
                            }
                        }
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int index(Vector3I pos)
            => index(pos.X, pos.Y, pos.Z);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int index(int x, int y, int z)
            => x + y * 8 + z * 8 * 8;

        public unsafe static IEnumerable<KeyValuePair<Vector3B, Voxel[]>> CreateScript(Vector2I sizeInBlocks)
        {
            if (sizeInBlocks.X < 1 || sizeInBlocks.Y < 1)
                throw new ArgumentOutOfRangeException(nameof(sizeInBlocks));

            Vector3I sizeInVoxels = new Vector3I(sizeInBlocks.X * 8 - 1, 3, sizeInBlocks.Y * 8 - 1);

            byte gray4 = (byte)FcColor.Gray4;
            byte gray3 = (byte)FcColor.Gray3;

            BlockVoxelsGenerator generator = new BlockVoxelsGenerator();

            generator.fill(Vector3B.Zero, sizeInVoxels, FcColor.Black);

            generator.loop(new Vector3I(1, 2, 1), new Vector3I(sizeInVoxels.X - 1, 3, sizeInVoxels.Z - 1), (ref Voxel voxel) =>
            {
                voxel.Colors[2] = gray4;
            });

            generator.getVoxel(new Vector3I(sizeInVoxels.X - 1, 2, 0)).Colors[2] = gray4;
            generator.getVoxel(new Vector3I(0, 2, sizeInVoxels.Z - 1)).Colors[2] = gray4;

            generator.loop(new Vector3I(1, 2, sizeInVoxels.Z - 1), new Vector3I(sizeInVoxels.X, 3, sizeInVoxels.Z), (ref Voxel voxel) =>
            {
                voxel.Colors[2] = gray3;
            });

            generator.loop(new Vector3I(sizeInVoxels.X - 1, 2, 1), new Vector3I(sizeInVoxels.X, 3, sizeInVoxels.Z), (ref Voxel voxel) =>
            {
                voxel.Colors[2] = gray3;
            });

            return generator.blocks;
        }
    }
}
