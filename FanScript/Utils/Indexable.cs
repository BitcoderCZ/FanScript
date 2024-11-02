namespace FanScript.Utils
{
    public readonly struct Indexable<TKey, TValue>
    {
        private readonly Func<TKey, TValue> _getFunc;

        public Indexable(Func<TKey, TValue> getFunc)
        {
            _getFunc = getFunc;
        }

        public TValue this[TKey index] => _getFunc(index);
    }
}
