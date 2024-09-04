using System.Collections;
using System.Collections.Immutable;

namespace FanScript.Compiler.Syntax
{
    public abstract class SeparatedSyntaxList
    {
        private protected SeparatedSyntaxList()
        {
        }

        public abstract ImmutableArray<SyntaxNode> GetWithSeparators();
    }

    public sealed class SeparatedSyntaxList<T> : SeparatedSyntaxList, IEnumerable<T>
        where T : SyntaxNode
    {
        private readonly ImmutableArray<SyntaxNode> nodesAndSeparators;

        internal SeparatedSyntaxList(ImmutableArray<SyntaxNode> nodesAndSeparators)
        {
            this.nodesAndSeparators = nodesAndSeparators;
        }

        public int Count => (nodesAndSeparators.Length + 1) / 2;

        public T this[int index] => (T)nodesAndSeparators[index * 2];

        public SyntaxToken GetSeparator(int index)
        {
            if (index < 0 || index >= Count - 1)
                throw new ArgumentOutOfRangeException(nameof(index));

            return (SyntaxToken)nodesAndSeparators[index * 2 + 1];
        }

        public override ImmutableArray<SyntaxNode> GetWithSeparators() => nodesAndSeparators;

        public IEnumerator<T> GetEnumerator()
        {
            for (var i = 0; i < Count; i++)
                yield return this[i];
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
