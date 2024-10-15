namespace FanScript.Utils
{
    internal class Disposable : IDisposable
    {
        private Action? onDispose;

        public Disposable(Action? onDispose)
        {
            this.onDispose = onDispose;
        }

        public void Dispose()
        {
            onDispose?.Invoke();
            onDispose = null;
        }
    }
}
