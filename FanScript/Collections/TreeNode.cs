namespace FanScript.Collections
{
    // from: https://stackoverflow.com/a/10442244/15878562
    public class TreeNode<T>
    {
        public T Value { get; set; }
        private readonly Dictionary<int, TreeNode<T>> children = new();

        public TreeNode(T _value)
        {
            Value = _value;
        }

        public TreeNode<T> this[int i]
        {
            get { return children[i]; }
        }

        public TreeNode<T>? Parent { get; private set; }

        //public TreeNode<T> AddChild(T value)
        //    => AddChild(children.Count, value);
        public TreeNode<T> AddChild(int index, T value)
        {
            TreeNode<T> node = new TreeNode<T>(value) { Parent = this };
            children.Add(index, node);
            return node;
        }

        //public TreeNode<T>[] AddChildren(params T[] values)
        //{
        //    return values.Select(AddChild).ToArray();
        //}

        public TreeNode<T> GetOrCreateChild(int index, T defaultValue)
        {
            if (index < children.Count)
                return children[index];
            else
                return AddChild(index, defaultValue);
        }
    }

    public class TreeIndex
    {
        private readonly int[] indexes;

        public TreeIndex(int[] _indexes)
        {
            indexes = _indexes;
        }

        public T GetValue<T>(TreeNode<T> tree)
            => GetValueFromLevel(tree, 0);
        public T GetValueFromLevel<T>(TreeNode<T> tree, int startLevel)
        {
            for (int i = startLevel; i < indexes.Length; i++)
                tree = tree[indexes[i]];

            return tree.Value;
        }

        public TreeIndex Lower(int index)
        {
            int[] newIndexes = new int[indexes.Length + 1];
            Array.Copy(indexes, newIndexes, indexes.Length);

            newIndexes[indexes.Length] = index;

            return new TreeIndex(newIndexes);
        }

        public TreeIndex Upper()
        {
            if (indexes.Length == 0) throw new InvalidOperationException();

            int[] newIndexes = new int[indexes.Length - 1];
            Array.Copy(indexes, newIndexes, newIndexes.Length);

            return new TreeIndex(newIndexes);
        }
    }
}
