using FanScript.Compiler.Symbols.Variables;
using FanScript.FCInfo;
using FanScript.Utils;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace FanScript.Compiler.Emit.Utils
{
    internal sealed class InlineVarManager
    {
        private readonly Dictionary<VariableSymbol, Entry> dict = new();

        public void Set(VariableSymbol variable, EmitStore store)
        {
            Debug.Assert(variable.Modifiers.HasFlag(Modifiers.Inline));

            dict[variable] = new Entry(store);
        }

        public bool TryGet(VariableSymbol variable, IEmitContext context, [NotNullWhen(true)] out EmitStore? store)
        {
            if (!dict.TryGetValue(variable, out var entry))
            {
                store = NopEmitStore.Instance;
                return true;
            }

            if (entry.UseCount + 1 < FancadeConstants.WireSplitLimit)
            {
                store = entry.GetStore();
                return true;
            }

            WireType type = entry.GetStoreType();
            if (type == WireType.Error)
                type = variable.Type.ToWireType();

            var passthrough = Blocks.GetPassthrough(type);

            if (passthrough is null)
            {
                if (entry.UseCount + 1 <= FancadeConstants.WireSplitLimit)
                {
                    store = entry.GetStore();
                    return true;
                }

                store = null;
                return false;
            }

            Block passthroughBlock = context.AddBlock(passthrough.Value.Def);

            context.Connect(entry.GetStore(), BasicEmitStore.CIn(passthroughBlock, passthrough.Value.In));

            entry = new Entry(BasicEmitStore.COut(passthroughBlock, passthrough.Value.Out));
            dict[variable] = entry;

            store = entry.GetStore();
            return true;
        }

        private class Entry
        {
            private readonly EmitStore store;
            public int UseCount;

            public Entry(EmitStore store)
            {
                this.store = store;
            }

            public EmitStore GetStore()
            {
                UseCount++;
                Debug.Assert(UseCount <= FancadeConstants.WireSplitLimit);
                return store;
            }

            public WireType GetStoreType()
                => store.Out.FirstOrDefault()?.GetWireType() ?? WireType.Error;
        }
    }
}
