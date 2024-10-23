using FanScript.Compiler.Exceptions;
using FanScript.Compiler.Symbols.Variables;
using FanScript.Compiler.Syntax;
using FanScript.Utils;

namespace FanScript.Compiler.Symbols
{
    internal static class SymbolPrinter
    {
        public static void WriteTo(Symbol symbol, TextWriter writer)
        {
            switch (symbol)
            {
                case FunctionSymbol functionSymbol:
                    WriteFunctionTo(functionSymbol, writer);
                    break;
                case ParameterSymbol parameterSymbol:
                    WriteParameterTo(parameterSymbol, writer);
                    break;
                case VariableSymbol variableSymbol:
                    WriteVariableTo(variableSymbol, writer);
                    break;
                case TypeSymbol typeSymbol:
                    WriteTypeTo(typeSymbol, writer);
                    break;
                default:
                    throw new UnexpectedSymbolException(symbol);
            }
        }

        internal static void WriteFunctionTo(FunctionSymbol symbol, TextWriter writer, bool onlyParams = false, bool writeAsMethod = false)
        {
            if (!onlyParams)
            {
                if (symbol.Modifiers != 0)
                {
                    writer.WriteModifiers(symbol.Modifiers);
                    writer.WriteSpace();
                }
                symbol.Type.WriteTo(writer);
                writer.WriteSpace();
                writer.WriteIdentifier(symbol.Name);
            }

            if (symbol.IsGeneric)
            {
                writer.WritePunctuation(SyntaxKind.LessToken);
                writer.WritePunctuation(SyntaxKind.GreaterToken);
            }
            writer.WritePunctuation(SyntaxKind.OpenParenthesisToken);

            int startParam = (writeAsMethod && symbol.IsMethod) ? 1 : 0;
            for (int i = startParam; i < symbol.Parameters.Length; i++)
            {
                if (i > startParam)
                {
                    writer.WritePunctuation(SyntaxKind.CommaToken);
                    writer.WriteSpace();
                }

                symbol.Parameters[i].WriteTo(writer);
            }

            writer.WritePunctuation(SyntaxKind.CloseParenthesisToken);
        }

        private static void WriteVariableTo(VariableSymbol symbol, TextWriter writer)
        {
            symbol.Type.WriteTo(writer);
            writer.WriteSpace();
            writer.WriteIdentifier(symbol.Name);
        }

        private static void WriteParameterTo(ParameterSymbol symbol, TextWriter writer)
        {
            if (symbol.Modifiers != 0)
            {
                writer.WriteModifiers(symbol.Modifiers);
                writer.WriteSpace();
            }
            symbol.Type.WriteTo(writer);
            writer.WriteSpace();
            writer.WriteIdentifier(symbol.Name);
        }

        private static void WriteTypeTo(TypeSymbol type, TextWriter writer)
        {
            type ??= TypeSymbol.Error;
            writer.SetForeground(type.IsGeneric ? ConsoleColor.DarkGreen : ConsoleColor.Blue);
            writer.Write(type.Name);
            writer.ResetColor();
            if (type.IsGeneric)
            {
                writer.WritePunctuation(SyntaxKind.LessToken);
                if (type.IsGenericInstance)
                    type.InnerType.WriteTo(writer);
                writer.WritePunctuation(SyntaxKind.GreaterToken);
            }
        }
    }
}
