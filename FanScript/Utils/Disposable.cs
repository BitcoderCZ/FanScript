namespace FanScript.Utils
{
    public class Disposable : IDisposable
    {
        private Action? onDispose;

        public Disposable(Action? _onDispose)
        {
            onDispose = _onDispose;
        }

        public void Dispose()
        {
            onDispose?.Invoke();
            onDispose = null;
        }
    }
}
