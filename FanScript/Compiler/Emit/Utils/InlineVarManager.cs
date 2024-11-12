using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using FanScript.Compiler.Symbols.Variables;
using FanScript.FCInfo;
using FanScript.Utils;

namespace FanScript.Compiler.Emit.Utils;

internal sealed class InlineVarManager
{
    private readonly Dictionary<VariableSymbol, Entry> _dict = [];

    public void Set(VariableSymbol variable, IEmitStore store)
    {
        Debug.Assert(variable.Modifiers.HasFlag(Modifiers.Inline), "Only inline variables can be set.");

        _dict[variable] = new Entry(store);
    }

    public bool TryGet(VariableSymbol variable, IEmitContext context, [NotNullWhen(true)] out IEmitStore? store)
    {
        if (!_dict.TryGetValue(variable, out var entry))
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
        {
            type = variable.Type.ToWireType();
        }

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
        _dict[variable] = entry;

        store = entry.GetStore();
        return true;
    }

    private class Entry
    {
        public int UseCount;
        private readonly IEmitStore _store;

        public Entry(IEmitStore store)
        {
            _store = store;
        }

        public IEmitStore GetStore()
        {
            UseCount++;
            Debug.Assert(UseCount <= FancadeConstants.WireSplitLimit, $"A terminals shouldn't be used more times than wire split limit ({FancadeConstants.WireSplitLimit}).");
            return _store;
        }

        public WireType GetStoreType()
            => _store.Out.FirstOrDefault()?.GetWireType() ?? WireType.Error;
    }
}
