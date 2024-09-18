namespace FanScript.Utils
{
    public readonly struct Indexable<TKey, TValue>
    {
        private readonly Func<TKey, TValue> get;

        public TValue this[TKey index]
        {
            get => get(index);
        }

        public Indexable(Func<TKey, TValue> get)
        {
            this.get = get;
        }
    }
}
