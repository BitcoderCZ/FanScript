using FanScript.Compiler.Symbols;

namespace FanScript.Compiler.Binding
{
    internal sealed class BoundTreeAnalyzer : BoundTreeVisitor
    {
        private readonly BoundAnalysisResult.Builder resultBuilder = new();

        public static BoundAnalysisResult AnalyzeAll(IEnumerable<BoundStatement> nodes)
        {
            BoundAnalysisResult result = new BoundAnalysisResult();

            foreach (var node in nodes)
                result.Add(Analyze(node));

            return result;
        }
        public static BoundAnalysisResult Analyze(BoundStatement node)
        {
            BoundTreeAnalyzer analyzer = new BoundTreeAnalyzer();
            return analyzer.analyze(node);
        }

        private BoundAnalysisResult analyze(BoundStatement node)
        {
            resultBuilder.Clear();

            Visit(node);

            return resultBuilder.Build();
        }

        protected override void VisitCallExpression(BoundCallExpression node)
        {
            base.VisitCallExpression(node);

            resultBuilder.FunctionCalled(node.Function);
        }
    }

    internal sealed class BoundAnalysisResult
    {
        private readonly Dictionary<FunctionSymbol, int> functionCallCount;

        public BoundAnalysisResult()
        {
            functionCallCount = new Dictionary<FunctionSymbol, int>();
        }
        public BoundAnalysisResult(Dictionary<FunctionSymbol, int> functionCallCount)
        {
            this.functionCallCount = functionCallCount;
        }

        public void Add(BoundAnalysisResult other)
        {
            foreach (var (func, count) in other.functionCallCount)
            {
                int thisCount = functionCallCount.GetValueOrDefault(func, 0);
                functionCallCount[func] = thisCount + count;
            }
        }

        public int GetCallCount(FunctionSymbol function)
            => functionCallCount.GetValueOrDefault(function, 0);

        public class Builder
        {
            private readonly Dictionary<FunctionSymbol, int> functionCallCount = new();

            public BoundAnalysisResult Build()
            {
                return new BoundAnalysisResult(functionCallCount);
            }

            public void Clear()
            {
                functionCallCount.Clear();
            }

            public void FunctionCalled(FunctionSymbol function)
            {
                functionCallCount[function] = functionCallCount.GetValueOrDefault(function, 0) + 1;
            }
        }
    }
}
