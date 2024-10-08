namespace FanScript.Compiler.Emit.Utils
{
    internal class EmitConnector
    {
        public EmitStore Store
        {
            get
            {
                if (firstStore is not null && lastStore is not null)
                    return new MultiEmitStore(firstStore, lastStore);
                else
                    return NopEmitStore.Instance;
            }
        }

        private EmitStore? firstStore;
        private EmitStore? lastStore;

        private Action<EmitStore, EmitStore> connect;

        public EmitConnector(Action<EmitStore, EmitStore> connectFunc)
        {
            connect = connectFunc;
        }

        public void Add(EmitStore store)
        {
            if (store is NopEmitStore)
                return;

            if (lastStore is not null)
                connect(lastStore, store);

            firstStore ??= store;
            lastStore = store;
        }
    }
}
