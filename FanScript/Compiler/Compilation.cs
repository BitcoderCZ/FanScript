using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using FanScript.Compiler.Binding;
using FanScript.Compiler.Diagnostics;
using FanScript.Compiler.Emit;
using FanScript.Compiler.Emit.BlockBuilders;
using FanScript.Compiler.Emit.CodePlacers;
using FanScript.Compiler.Symbols;
using FanScript.Compiler.Symbols.Functions;
using FanScript.Compiler.Symbols.Variables;
using FanScript.Compiler.Syntax;

[assembly: InternalsVisibleTo("FanScript.LangServer")]

namespace FanScript.Compiler
{
    public sealed class Compilation
    {
        private BoundGlobalScope? _globalScope;

        private Compilation(Compilation? previous, params SyntaxTree[] syntaxTrees)
        {
            Previous = previous;
            SyntaxTrees = [.. syntaxTrees];
        }

        public Compilation? Previous { get; }

        public ImmutableArray<SyntaxTree> SyntaxTrees { get; }

        public ImmutableArray<FunctionSymbol> Functions => GlobalScope.Functions;

        public ImmutableArray<VariableSymbol> Variables => GlobalScope.Variables;

        internal BoundGlobalScope GlobalScope
        {
            get
            {
                if (_globalScope is null)
                {
                    BoundGlobalScope newGlobalScope = Binder.BindGlobalScope(Previous?.GlobalScope, SyntaxTrees);
                    Interlocked.CompareExchange(ref _globalScope, newGlobalScope, null);
                }

                return _globalScope;
            }
        }

        public static Compilation Create(Compilation? previous, params SyntaxTree[] syntaxTrees) => new Compilation(previous, syntaxTrees);

        public IEnumerable<Symbol> GetSymbols()
            => Enumerable.Concat<Symbol>(
                GetFunctions(),
                GetVariables());

        public IEnumerable<FunctionSymbol> GetFunctions()
        {
            Compilation? submission = this;

            foreach (FunctionSymbol builtin in BuiltinFunctions.GetAll())
            {
                yield return builtin;
            }

            while (submission is not null)
            {
                foreach (FunctionSymbol function in submission.Functions)
                {
                    yield return function;
                }

                submission = submission.Previous;
            }
        }

        public IEnumerable<VariableSymbol> GetVariables()
        {
            Compilation? submission = this;
            HashSet<string> seenVariables = [];

            while (submission is not null)
            {
                foreach (VariableSymbol variable in submission.Variables)
                {
                    if (seenVariables.Add(variable.Name))
                    {
                        yield return variable;
                    }
                }

                submission = submission.Previous;
            }
        }

        /*public EvaluationResult Evaluate(Dictionary<VariableSymbol, object> variables)
        {
            if (GlobalScope.Diagnostics.Any())
                return new EvaluationResult(GlobalScope.Diagnostics, null);

            var program = GetProgram();

            // var appPath = Environment.GetCommandLineArgs()[0];
            // var appDirectory = Path.GetDirectoryName(appPath);
            // var cfgPath = Path.Combine(appDirectory, "cfg.dot");
            // var cfgStatement = !program.Statement.Statements.Any() && program.Functions.Any()
            //                       ? program.Functions.Last().Value
            //                       : program.Statement;
            // var cfg = ControlFlowGraph.Create(cfgStatement);
            // using (var streamWriter = new StreamWriter(cfgPath))
            //     cfg.WriteTo(streamWriter);

            if (program.Diagnostics.HasErrors())
                return new EvaluationResult(program.Diagnostics, null);

            var evaluator = new Evaluator(program, variables);
            var value = evaluator.Evaluate();

            return new EvaluationResult(program.Diagnostics, value);
        }*/

        public IDictionary<FunctionSymbol, ScopeWSpan> GetScopes()
        {
            BoundProgram program = GetProgram();

            return program.FunctionScopes;
        }

        public void EmitTree(TextWriter writer)
        {
            if (GlobalScope.ScriptFunction is not null)
            {
                EmitTree(GlobalScope.ScriptFunction, writer);
            }
        }

        public void EmitTree(FunctionSymbol symbol, TextWriter writer)
        {
            BoundProgram program = GetProgram();
            symbol.WriteTo(writer);
            writer.WriteLine();
            if (program.Functions.TryGetValue(symbol, out BoundBlockStatement? body))
            {
                body.WriteTo(writer);
            }
        }

        public ImmutableArray<Diagnostic> Emit(CodePlacer placer, BlockBuilder builder)
        {
            if (GlobalScope.Diagnostics.HasErrors())
            {
                return GlobalScope.Diagnostics;
            }

            BoundProgram program = GetProgram();
            return Emitter.Emit(program, placer, builder);
        }

        internal BoundProgram GetProgram()
        {
            BoundProgram? previous = Previous?.GetProgram();
            return Binder.BindProgram(previous, GlobalScope);
        }
    }
}
