using MathUtils.Vectors;

namespace FanScript.Utils
{
    internal static class BinPacker
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sizes">Sizes of the boxes</param>
        /// <returns>Positions of the boxes</returns>
        public static Vector3I[] Compute(Vector3I[] sizes)
        {
            List<Container> placedContainers = [];

            // the result positions
            Vector3I[] positions = new Vector3I[sizes.Length];

            Vector3I occupiedArea = Vector3I.Zero;
            List<Vector3I> freePositions =
            [
                Vector3I.Zero
            ];

            foreach (var (index, size) in sizes
                .Select((size, index) => (index, size))
                .OrderByDescending(item => item.size.X * item.size.Y * item.size.Z))
            {
                if (freePositions.Count == 0)
                {
                    throw new Exception("No free positions left (this shouldn't happen)."); // shouldn't really happen, but check here just in case
                }
                else if (freePositions.Count == 1)
                {
                    if (new Container(freePositions[0], size).IntersectsAny(placedContainers))
                    {
                        throw new Exception("No free (un-occupied) positions left (this shouldn't happen)."); // shouldn't really happen, but check here just in case
                    }

                    positions[index] = freePositions[0];
                    AddContainer(freePositions[0], size);
                    continue;
                }

                // pick the position adding the least to occupiedArea
                var (pos, _) = freePositions
                    .Where(pos => !new Container(pos, size).IntersectsAny(placedContainers))
                    .Select(pos => (pos, CalculateArea(Vector3I.Max(pos + size, occupiedArea))))
                    .MinBy(item => item.Item2);

                positions[index] = pos;
                AddContainer(pos, size);

                occupiedArea = Vector3I.Max(pos + size, occupiedArea);
            }

            return positions;

            void AddContainer(Vector3I pos, Vector3I size)
            {
                freePositions.Remove(pos);
                placedContainers.Add(new Container(pos, size));

                // add some un-occupied positions, the most optimal positions might not get selected, but we don't need to loop over all positions
                freePositions.AddRange(
                [
                    pos + new Vector3I(size.X, 0, 0),
                    pos + new Vector3I(0, size.Y, 0),
                    pos + new Vector3I(0, 0, size.Z),
                    pos + new Vector3I(size.X, size.Y, 0),
                    pos + new Vector3I(size.X, 0, size.Z),
                    pos + new Vector3I(0, size.Y, size.Z),
                    pos + new Vector3I(size.X, size.Y, size.Z),
                ]);
            }
        }

        private static long CalculateArea(Vector3I size)
            => size.X * (int)Math.Pow(size.Y, 1.25) * size.Z; // favor x and z over y

        private struct Container
        {
            public Vector3I Pos;
            public Vector3I Size;

            public Container(Vector3I pos, Vector3I size)
            {
                Pos = pos;
                Size = size;
            }

            public readonly Vector3I Max => Pos + Size;

            public static bool Intersects(Container a, Container b)
                => (a.Pos.X < b.Max.X && a.Max.X > b.Pos.X) &&
                    (a.Pos.Y < b.Max.Y && a.Max.Y > b.Pos.Y) &&
                    (a.Pos.Z < b.Max.Z && a.Max.Z > b.Pos.Z);

            public readonly bool IntersectsAny(List<Container> containers)
            {
                for (int i = 0; i < containers.Count; i++)
                {
                    if (Intersects(this, containers[i]))
                    {
                        return true;
                    }
                }

                return false;
            }

            public override readonly string ToString()
                => $"{{Pos: {Pos}, Size: {Size}}}";
        }
    }
}
