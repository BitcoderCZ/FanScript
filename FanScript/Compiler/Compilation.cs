using FanScript.Compiler.Binding;
using FanScript.Compiler.Diagnostics;
using FanScript.Compiler.Emit;
using FanScript.Compiler.Symbols;
using FanScript.Compiler.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FanScript.Compiler
{
    public sealed class Compilation
    {
        private BoundGlobalScope? _globalScope;

        private Compilation(bool isScript, Compilation? previous, params SyntaxTree[] syntaxTrees)
        {
            IsScript = isScript;
            Previous = previous;
            SyntaxTrees = syntaxTrees.ToImmutableArray();
        }

        public static Compilation Create(params SyntaxTree[] syntaxTrees)
        {
            return new Compilation(isScript: false, previous: null, syntaxTrees);
        }

        public static Compilation CreateScript(Compilation? previous, params SyntaxTree[] syntaxTrees)
        {
            return new Compilation(isScript: true, previous, syntaxTrees);
        }

        public bool IsScript { get; }
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
                    BoundGlobalScope globalScope = Binder.BindGlobalScope(IsScript, Previous?.GlobalScope, SyntaxTrees);
                    Interlocked.CompareExchange(ref _globalScope, globalScope, null);
                }

                return _globalScope;
            }
        }

        public IEnumerable<Symbol> GetSymbols()
        {
            Compilation? submission = this;
            HashSet<string> seenSymbolNames = new HashSet<string>();

            List<FunctionSymbol> builtinFunctions = BuiltinFunctions.GetAll().ToList();

            while (submission is not null)
            {
                foreach (FunctionSymbol function in submission.Functions)
                    if (seenSymbolNames.Add(function.Name))
                        yield return function;

                foreach (VariableSymbol variable in submission.Variables)
                    if (seenSymbolNames.Add(variable.Name))
                        yield return variable;

                foreach (FunctionSymbol builtin in builtinFunctions)
                    if (seenSymbolNames.Add(builtin.Name))
                        yield return builtin;

                submission = submission.Previous;
            }
        }

        private BoundProgram GetProgram()
        {
            BoundProgram? previous = Previous is null ? null : Previous.GetProgram();
            return Binder.BindProgram(IsScript, previous, GlobalScope);
        }

        //public EvaluationResult Evaluate(Dictionary<VariableSymbol, object> variables)
        //{
        //    if (GlobalScope.Diagnostics.Any())
        //        return new EvaluationResult(GlobalScope.Diagnostics, null);

        //    var program = GetProgram();

        //    // var appPath = Environment.GetCommandLineArgs()[0];
        //    // var appDirectory = Path.GetDirectoryName(appPath);
        //    // var cfgPath = Path.Combine(appDirectory, "cfg.dot");
        //    // var cfgStatement = !program.Statement.Statements.Any() && program.Functions.Any()
        //    //                       ? program.Functions.Last().Value
        //    //                       : program.Statement;
        //    // var cfg = ControlFlowGraph.Create(cfgStatement);
        //    // using (var streamWriter = new StreamWriter(cfgPath))
        //    //     cfg.WriteTo(streamWriter);

        //    if (program.Diagnostics.HasErrors())
        //        return new EvaluationResult(program.Diagnostics, null);

        //    var evaluator = new Evaluator(program, variables);
        //    var value = evaluator.Evaluate();

        //    return new EvaluationResult(program.Diagnostics, value);
        //}

        public void EmitTree(TextWriter writer)
        {
            /*if (GlobalScope.MainFunction is not null)
                EmitTree(GlobalScope.MainFunction, writer);
            else */
            if (GlobalScope.ScriptFunction is not null)
                EmitTree(GlobalScope.ScriptFunction, writer);
        }

        public void EmitTree(FunctionSymbol symbol, TextWriter writer)
        {
            BoundProgram program = GetProgram();
            symbol.WriteTo(writer);
            writer.WriteLine();
            if (!program.Functions.TryGetValue(symbol, out var body))
                return;
            body.WriteTo(writer);
        }

        // TODO: References should be part of the compilation, not arguments for Emit
        /*public ImmutableArray<Diagnostic> Emit(string moduleName, string[] references, string outputPath)
        {
            var parseDiagnostics = SyntaxTrees.SelectMany(st => st.Diagnostics);

            var diagnostics = parseDiagnostics.Concat(GlobalScope.Diagnostics).ToImmutableArray();
            if (diagnostics.HasErrors())
                return diagnostics;

            var program = GetProgram();
            return Emitter.Emit(program, moduleName, references, outputPath);
        }*/

        public ImmutableArray<Diagnostic> Emit(CodeBuilder builder)
        {
            IEnumerable<Diagnostic> parseDiagnostics = SyntaxTrees.SelectMany(st => st.Diagnostics);

            ImmutableArray<Diagnostic> diagnostics = parseDiagnostics.Concat(GlobalScope.Diagnostics).ToImmutableArray();

            if (diagnostics.HasErrors())
                return diagnostics;

            BoundProgram program = GetProgram();
            return Emitter.Emit(program, builder);
        }
    }
}
