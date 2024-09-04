using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FanScript.Compiler.Emit
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
                    return new NopEmitStore();
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
