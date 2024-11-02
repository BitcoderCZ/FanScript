using System.Diagnostics.CodeAnalysis;
using FanScript.Compiler.Symbols.Functions;
using FanScript.Utils;

namespace FanScript.Compiler.Binding
{
    internal sealed class BoundTreeAnalyzer : BoundTreeVisitor
    {
        private readonly BoundAnalysisResult.Builder _resultBuilder = new();
        private readonly FunctionSymbol _location;

        public BoundTreeAnalyzer(FunctionSymbol location)
        {
            _location = location;
        }

        public static BoundAnalysisResult AnalyzeAll(IEnumerable<(BoundStatement Statement, FunctionSymbol Location)> nodes)
        {
            BoundAnalysisResult result = new BoundAnalysisResult();

            foreach (var (node, location) in nodes)
                result.Add(Analyze(node, location));

            return result;
        }

        public static BoundAnalysisResult Analyze(BoundStatement node, FunctionSymbol location)
        {
            BoundTreeAnalyzer analyzer = new BoundTreeAnalyzer(location);
            return analyzer.Analyze(node);
        }

        protected override void VisitCallStatement(BoundCallStatement node)
        {
            base.VisitCallStatement(node);

            _resultBuilder.FunctionCalled(node.Function, _location);
        }

        protected override void VisitCallExpression(BoundCallExpression node)
        {
            base.VisitCallExpression(node);

            _resultBuilder.FunctionCalled(node.Function, _location);
        }

        private BoundAnalysisResult Analyze(BoundStatement node)
        {
            _resultBuilder.Clear();

            Visit(node);

            return _resultBuilder.Build();
        }
    }

    internal sealed class BoundAnalysisResult
    {
        private readonly Dictionary<FunctionSymbol, int> _functionCallCount;
        private readonly SetMultiValueDictionary<FunctionSymbol, FunctionSymbol> _functionCalls;

        public BoundAnalysisResult()
        {
            _functionCallCount = [];
            _functionCalls = [];
        }

        public BoundAnalysisResult(Dictionary<FunctionSymbol, int> functionCallCount, SetMultiValueDictionary<FunctionSymbol, FunctionSymbol> functionCalls)
        {
            _functionCallCount = functionCallCount;
            _functionCalls = functionCalls;
        }

        public void Add(BoundAnalysisResult other)
        {
            foreach (var (func, count) in other._functionCallCount)
            {
                int thisCount = _functionCallCount.GetValueOrDefault(func, 0);
                _functionCallCount[func] = thisCount + count;
            }

            foreach (var (func, callLocations) in other._functionCalls)
                _functionCalls.AddRange(func, callLocations);
        }

        public int GetCallCount(FunctionSymbol function)
            => _functionCallCount.GetValueOrDefault(function, 0);

        public bool ShouldFunctionGetInlined(FunctionSymbol function)
            => function.Declaration is not null && (function.Modifiers.HasFlag(Modifiers.Inline) || GetCallCount(function) <= 1);

        public bool HasCircularCalls([NotNullWhen(true)] out IEnumerable<FunctionSymbol>? circularCall)
        {
            CircularCallDetector detector = new CircularCallDetector(_functionCalls);

            circularCall = detector.Detect();

            return circularCall is not null;
        }

        public IEnumerable<FunctionSymbol> EnumerateFunctionsInReverse()
        {
            ReverseCallGraph callGraph = new ReverseCallGraph(_functionCalls);
            return callGraph.GetCallOrder();
        }

        public class Builder
        {
            private readonly Dictionary<FunctionSymbol, int> _functionCallCount = [];

            // to, from
            private readonly SetMultiValueDictionary<FunctionSymbol, FunctionSymbol> _functionCalls = [];

            public BoundAnalysisResult Build()
                => new BoundAnalysisResult(_functionCallCount, _functionCalls);

            public void Clear()
            {
                _functionCallCount.Clear();
                _functionCalls.Clear();
            }

            public void FunctionCalled(FunctionSymbol function, FunctionSymbol callLocation)
            {
                if (function.Declaration is null)
                {
                    return;
                }

                _functionCallCount[function] = _functionCallCount.GetValueOrDefault(function, 0) + 1;
                _functionCalls.Add(function, callLocation);
            }
        }
    }
}
