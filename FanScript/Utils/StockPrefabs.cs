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
            using (Stream stream = ResourceUtils.OpenResource("stockPrefabs.fcppl")) // fcppl - Fancade partial prefab list
            using (FcBinaryReader reader = new FcBinaryReader(stream))
                List = PartialPrefabList.Load(reader);
        }
    }
}
