using FanScript.Compiler.Symbols;
using FanScript.Utils;
using System.Diagnostics.CodeAnalysis;

namespace FanScript.Compiler.Binding
{
    internal sealed class BoundTreeAnalyzer : BoundTreeVisitor
    {
        private readonly BoundAnalysisResult.Builder resultBuilder = new();
        private readonly FunctionSymbol location;

        public BoundTreeAnalyzer(FunctionSymbol location)
        {
            this.location = location;
        }

        public static BoundAnalysisResult AnalyzeAll(IEnumerable<(BoundStatement statement, FunctionSymbol location)> nodes)
        {
            BoundAnalysisResult result = new BoundAnalysisResult();

            foreach (var (node, location) in nodes)
                result.Add(Analyze(node, location));

            return result;
        }
        public static BoundAnalysisResult Analyze(BoundStatement node, FunctionSymbol location)
        {
            BoundTreeAnalyzer analyzer = new BoundTreeAnalyzer(location);
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

            resultBuilder.FunctionCalled(node.Function, location);
        }
    }

    internal sealed class BoundAnalysisResult
    {
        private readonly Dictionary<FunctionSymbol, int> functionCallCount;
        private readonly SetMultiValueDictionary<FunctionSymbol, FunctionSymbol> functionCalls;

        public BoundAnalysisResult()
        {
            functionCallCount = new();
            functionCalls = new();
        }
        public BoundAnalysisResult(Dictionary<FunctionSymbol, int> functionCallCount, SetMultiValueDictionary<FunctionSymbol, FunctionSymbol> functionCalls)
        {
            this.functionCallCount = functionCallCount;
            this.functionCalls = functionCalls;
        }

        public void Add(BoundAnalysisResult other)
        {
            foreach (var (func, count) in other.functionCallCount)
            {
                int thisCount = functionCallCount.GetValueOrDefault(func, 0);
                functionCallCount[func] = thisCount + count;
            }

            foreach (var (func, callLocations) in other.functionCalls)
                functionCalls.AddRange(func, callLocations);
        }

        public int GetCallCount(FunctionSymbol function)
            => functionCallCount.GetValueOrDefault(function, 0);

        public bool ShouldFunctionGetInlined(FunctionSymbol function)
            => function.Declaration is not null && (function.Modifiers.HasFlag(Modifiers.Inline) || GetCallCount(function) <= 1);

        public bool HasCircularCalls([NotNullWhen(true)] out IEnumerable<FunctionSymbol>? circularCall)
        {
            CircularCallDetector detector = new CircularCallDetector(functionCalls);

            circularCall = detector.Detect();

            return circularCall is not null;
        }

        public IEnumerable<FunctionSymbol> EnumerateFunctionsInReverse()
        {
            ReverseCallGraph callGraph = new ReverseCallGraph(functionCalls);
            return callGraph.GetCallOrder();
        }

        public class Builder
        {
            private readonly Dictionary<FunctionSymbol, int> functionCallCount = new();
            // to, from
            private readonly SetMultiValueDictionary<FunctionSymbol, FunctionSymbol> functionCalls = new();

            public BoundAnalysisResult Build()
            {
                return new BoundAnalysisResult(functionCallCount, functionCalls);
            }

            public void Clear()
            {
                functionCallCount.Clear();
                functionCalls.Clear();
            }

            public void FunctionCalled(FunctionSymbol function, FunctionSymbol callLocation)
            {
                if (function.Declaration is null)
                    return;

                functionCallCount[function] = functionCallCount.GetValueOrDefault(function, 0) + 1;
                functionCalls.Add(function, callLocation);
            }
        }
    }
}
