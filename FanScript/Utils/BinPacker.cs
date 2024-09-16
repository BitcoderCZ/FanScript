using MathUtils.Vectors;
using System.Diagnostics.CodeAnalysis;

namespace FanScript.Utils
{
    public static class BinPacker
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sizes">Sizes of the boxes</param>
        /// <returns>Positions of the boxes</returns>
        public static Vector3I[] Compute(Vector3I[] sizes)
        {
            // sort both split containes and sizes by volume
            SortedSet<Container> containers =
            [
                new Container(Vector3I.Zero, new Vector3I(int.MaxValue, int.MaxValue, int.MaxValue))
            ];

            Vector3I[] positions = new Vector3I[sizes.Length];

            foreach (var (index, size) in sizes
                .Select((Size, Index) => (Index, Size))
                .OrderBy(item => item.Size.X * item.Size.Y * item.Size.Z))
            {
                Container? selectedContainer = null;

                // find a container that this box sits into
                foreach (var container in containers)
                {
                    if (container.CanFit(size))
                    {
                        selectedContainer = container;
                        break;
                    }
                }

                if (selectedContainer is null)
                    throw new Exception($"Couldn't find a container large enought to fit a box of size {size}.");

                // remove the space occupied by the box
                containers.Remove(selectedContainer);
                var split = selectedContainer.Split(size);
                containers.UnionWith(split); // AddRange

                Console.WriteLine($"Container: {selectedContainer} split into (box size: {size}):");
                foreach (var c in split)
                    Console.WriteLine(" - " + c);

                Container.MergeAll(containers);

                Console.WriteLine("All containers after merge:");
                foreach (var c in containers)
                    Console.WriteLine(" - " + c);

                positions[index] = selectedContainer.Pos;
            }

            return positions;
        }

        private class Container : IComparable<Container>
        {
            public Vector3I Pos;
            public Vector3I Size;

            private Vector3I neighborPosX => Pos + new Vector3I(Size.X, 0, 0);
            private Vector3I neighborPosY => Pos + new Vector3I(0, Size.Y, 0);
            private Vector3I neighborPosZ => Pos + new Vector3I(0, 0, Size.Z);

            public Container(Vector3I pos, Vector3I size)
            {
                Pos = pos;
                Size = size;

                if (size.X == 0 || size.Y == 0 || size.Z == 0)
                {

                }
            }

            public bool CanFit(Vector3I size)
                => size.X <= Size.X && size.Y <= Size.Y && size.Z <= Size.Z;

            public IEnumerable<Container> Split(Vector3I blockSize)
            {
                if (blockSize.X > Size.X || blockSize.Y > Size.Y || blockSize.Z > Size.Z)
                    throw new ArgumentOutOfRangeException(nameof(blockSize), $"{nameof(blockSize)} ({blockSize}) cannot be larger than {nameof(Size)} ({Size}).");
                else if (blockSize == Size)
                    yield break;

                // sides
                if (blockSize.X < Size.X)
                    yield return new Container(Pos + new Vector3I(blockSize.X, 0, 0), new Vector3I(Size.X - blockSize.X, blockSize.Y, blockSize.Z));
                if (blockSize.Y < Size.Y)
                    yield return new Container(Pos + new Vector3I(0, blockSize.Y, 0), new Vector3I(blockSize.X, Size.Y - blockSize.Y, blockSize.Z));
                if (blockSize.Z < Size.Z)
                    yield return new Container(Pos + new Vector3I(0, 0, blockSize.Z), new Vector3I(blockSize.X, blockSize.Y, Size.Z - blockSize.Z));

                // corners
                if (blockSize.X < Size.X && blockSize.Y < Size.Y)
                    yield return new Container(Pos + new Vector3I(blockSize.X, blockSize.Y, 0), new Vector3I(Size.X - blockSize.X, Size.Y - blockSize.Y, blockSize.Z));
                if (blockSize.Z < Size.Z && blockSize.Y < Size.Y)
                    yield return new Container(Pos + new Vector3I(0, blockSize.Y, blockSize.Z), new Vector3I(blockSize.X, Size.Y - blockSize.Y, Size.Z - blockSize.Z));
                if (blockSize.X < Size.X && blockSize.Z < Size.Z)
                    yield return new Container(Pos + new Vector3I(blockSize.X, 0, blockSize.Z), new Vector3I(Size.X - blockSize.X, Size.Y - blockSize.Y, blockSize.Z));

                if (blockSize.X < Size.X && blockSize.Y < Size.Y && blockSize.Z < Size.Z)
                    yield return new Container(Pos + blockSize, Size - blockSize);
            }

            public static void MergeAll(ICollection<Container> containers)
            {
                Container? conA = null;
                Container? conB = null;
                Container? merged = null;

                while (true)
                {
                    foreach (var container in containers)
                    {
                        foreach (var b in containers)
                        {
                            if (b != container && TryMerge(container, b, out merged))
                            {
                                conA = container;
                                conB = b;
                                break;
                            }
                        }

                        if (merged is not null)
                            break;
                    }

                    if (merged is not null)
                    {
                        Console.WriteLine($"Merge {conA!} and {conB!} into: {merged}");

                        containers.Remove(conA!);
                        containers.Remove(conB!);
                        containers.Add(merged);

                        merged = null;
                    }
                    else
                        break;
                }
            }

            public static bool TryMerge(Container a, Container b, [NotNullWhen(true)] out Container? merged)
            {
                merged = null;

                if (a.neighborPosX == b.Pos)
                {
                    if (a.Size.YZ == b.Size.YZ)
                    {
                        merged = new Container(a.Pos, a.Size + new Vector3I(b.Size.X, 0, 0));
                        return true;
                    }
                    else
                        return false;
                }

                if (a.neighborPosY == b.Pos)
                {
                    if (a.Size.XZ == b.Size.XZ)
                    {
                        merged = new Container(a.Pos, a.Size + new Vector3I(0, b.Size.Y, 0));
                        return true;
                    }
                    else
                        return false;
                }

                if (a.neighborPosZ == b.Pos)
                {
                    if (a.Size.XY == b.Size.XY)
                    {
                        merged = new Container(a.Pos, a.Size + new Vector3I(0, 0, b.Size.Z));
                        return true;
                    }
                    else
                        return false;
                }

                return false;
            }

            public override string ToString()
                => $"{{Pos: {Pos}, Size: {Size}}}";

            public int CompareTo(Container? other)
            {
                if (other is null)
                    return 1;

                int lenComp = Pos.LengthSquared.CompareTo(other.Pos.LengthSquared);

                return lenComp != 0 ? lenComp : posComp();

                int posComp()
                {
                    int xComp = Pos.Y.CompareTo(other.Pos.Y);

                    if (xComp != 0)
                        return xComp;

                    int zComp = Pos.Z.CompareTo(other.Pos.Z);

                    if (zComp != 0)
                        return zComp;
                    else
                        return Pos.X.CompareTo(other.Pos.X);
                }
            }
        }
    }
}
