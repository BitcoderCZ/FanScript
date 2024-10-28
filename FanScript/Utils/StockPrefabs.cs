using FancadeLoaderLib;
using FancadeLoaderLib.Partial;

namespace FanScript.Utils
{
    public sealed class StockPrefabs
    {
        private static StockPrefabs? instance;
        public static StockPrefabs Instance => instance ??= new StockPrefabs();

        public readonly PartialPrefabList List;

        private StockPrefabs()
        {
            using (FcBinaryReader reader = new FcBinaryReader("stockPrefabs.fcppl")) // fcppl - Fancade partial prefab list
                List = PartialPrefabList.Load(reader);
        }
    }
}
