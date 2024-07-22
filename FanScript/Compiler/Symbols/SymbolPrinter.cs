using FanScript.Compiler.Syntax;
using FanScript.Utils;
using System.Xml.Linq;

namespace FanScript.Compiler.Symbols
{
    internal static class SymbolPrinter
    {
        public static void WriteTo(Symbol symbol, TextWriter writer)
        {
            switch (symbol.Kind)
            {
                case SymbolKind.Function:
                    WriteFunctionTo((FunctionSymbol)symbol, writer);
                    break;
                case SymbolKind.GlobalVariable:
                    WriteGlobalVariableTo((GlobalVariableSymbol)symbol, writer);
                    break;
                case SymbolKind.LocalVariable:
                    WriteLocalVariableTo((LocalVariableSymbol)symbol, writer);
                    break;
                case SymbolKind.Parameter:
                    WriteParameterTo((ParameterSymbol)symbol, writer);
                    break;
                case SymbolKind.Type:
                    WriteTypeTo((TypeSymbol)symbol, writer);
                    break;
                default:
                    throw new Exception($"Unexpected symbol: {symbol.Kind}");
            }
        }

        internal static void WriteFunctionTo(FunctionSymbol symbol, TextWriter writer, bool onlyParams = false)
        {
            if (!onlyParams)
            {
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

            for (int i = 0; i < symbol.Parameters.Length; i++)
            {
                if (i > 0)
                {
                    writer.WritePunctuation(SyntaxKind.CommaToken);
                    writer.WriteSpace();
                }

                symbol.Parameters[i].WriteTo(writer);
            }

            writer.WritePunctuation(SyntaxKind.CloseParenthesisToken);
        }

        private static void WriteGlobalVariableTo(GlobalVariableSymbol symbol, TextWriter writer)
        {
            symbol.Type.WriteTo(writer);
            writer.WriteSpace();
            writer.WriteIdentifier(symbol.Name);
        }

        private static void WriteLocalVariableTo(LocalVariableSymbol symbol, TextWriter writer)
        {
            symbol.Type.WriteTo(writer);
            writer.WriteSpace();
            writer.WriteIdentifier(symbol.Name);
        }

        private static void WriteParameterTo(ParameterSymbol symbol, TextWriter writer)
        {
            // always readonly, so no reason to print it
            Modifiers modifiers = symbol.Modifiers ^ Modifiers.Readonly;
            if (modifiers != 0)
            {
                writer.WriteModifiers(modifiers);
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
