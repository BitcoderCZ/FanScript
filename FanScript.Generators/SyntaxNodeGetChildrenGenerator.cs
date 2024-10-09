﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FanScript.Generators
{
    [Generator]
    public class SyntaxNodeGetChildrenGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
        }

        public void Execute(GeneratorExecutionContext context)
        {
            HashSet<string> typesToSkip = new HashSet<string>()
            {
                "CallExpressionSyntax",
                "CallStatementSyntax",
                "TypeClauseSyntax",
                "VariableDeclarationStatementSyntax",
            };

            SourceText sourceText;

            var compilation = (CSharpCompilation)context.Compilation;

            var immutableArrayType = compilation.GetTypeByMetadataName("System.Collections.Immutable.ImmutableArray`1");
            var separatedSyntaxListType = compilation.GetTypeByMetadataName("FanScript.Compiler.Syntax.SeparatedSyntaxList`1");
            var syntaxNodeType = compilation.GetTypeByMetadataName("FanScript.Compiler.Syntax.SyntaxNode");

            if (immutableArrayType == null || separatedSyntaxListType == null || syntaxNodeType == null)
                return;

            var types = GetAllTypes(compilation.Assembly);
            var syntaxNodeTypes = types.Where(t => !t.IsAbstract && IsPartial(t) && IsDerivedFrom(t, syntaxNodeType));

            string indentString = "    ";
            using (var stringWriter = new StringWriter())
            using (var indentedTextWriter = new IndentedTextWriter(stringWriter, indentString))
            {
                indentedTextWriter.WriteLine("// <auto-generated/>");
                indentedTextWriter.WriteLine("using System;");
                indentedTextWriter.WriteLine("using System.Collections.Generic;");
                indentedTextWriter.WriteLine("using System.Collections.Immutable;");
                indentedTextWriter.WriteLine();
                using (var nameSpaceCurly = new CurlyIndenter(indentedTextWriter, "namespace FanScript.Compiler.Syntax"))
                {
                    foreach (var type in syntaxNodeTypes)
                    {
                        if (typesToSkip.Contains(type.Name))
                            continue;

                        using (var classCurly = new CurlyIndenter(indentedTextWriter, $"partial class {type.Name}"))
                        using (var getChildCurly = new CurlyIndenter(indentedTextWriter, "public override IEnumerable<SyntaxNode> GetChildren()"))
                        {
                            var properties = type.GetMembers().OfType<IPropertySymbol>();

                            HashSet<string> processedProperties = new HashSet<string>();

                            foreach (var property in properties)
                            {
                                if (property.Type is INamedTypeSymbol propertyType)
                                {
                                    if (IsDerivedFrom(propertyType, syntaxNodeType))
                                    {
                                        var canBeNull = property.NullableAnnotation == NullableAnnotation.Annotated;
                                        if (canBeNull)
                                        {
                                            indentedTextWriter.WriteLine($"if ({property.Name} is not null)");
                                            indentedTextWriter.Indent++;
                                        }

                                        indentedTextWriter.WriteLine($"yield return {property.Name};");

                                        if (canBeNull)
                                            indentedTextWriter.Indent--;

                                        processedProperties.Add(property.Name);
                                    }
                                    else if (propertyType.TypeArguments.Length == 1 &&
                                             IsDerivedFrom(propertyType.TypeArguments[0], syntaxNodeType) &&
                                             SymbolEqualityComparer.Default.Equals(propertyType.OriginalDefinition, immutableArrayType))
                                    {
                                        indentedTextWriter.WriteLine($"foreach (var child in {property.Name})");
                                        indentedTextWriter.WriteLine($"{indentString}yield return child;");

                                        processedProperties.Add(property.Name);
                                    }
                                    else if (SymbolEqualityComparer.Default.Equals(propertyType.OriginalDefinition, separatedSyntaxListType) &&
                                             IsDerivedFrom(propertyType.TypeArguments[0], syntaxNodeType))
                                    {
                                        indentedTextWriter.WriteLine($"foreach (var child in {property.Name}.GetWithSeparators())");
                                        indentedTextWriter.WriteLine($"{indentString}yield return child;");

                                        processedProperties.Add(property.Name);
                                    }
                                }
                            }
                        }
                    }
                }

                indentedTextWriter.Flush();
                stringWriter.Flush();

                sourceText = SourceText.From(stringWriter.ToString(), Encoding.UTF8);
            }

            context.AddSource("SyntaxNode_GetChildren.g.cs", sourceText);
        }

        private IReadOnlyList<INamedTypeSymbol> GetAllTypes(IAssemblySymbol symbol)
        {
            var result = new List<INamedTypeSymbol>();
            GetAllTypes(result, symbol.GlobalNamespace);
            result.Sort((x, y) => x.MetadataName.CompareTo(y.MetadataName));
            return result;
        }

        private void GetAllTypes(List<INamedTypeSymbol> result, INamespaceOrTypeSymbol symbol)
        {
            if (symbol is INamedTypeSymbol type)
                result.Add(type);

            foreach (var child in symbol.GetMembers())
                if (child is INamespaceOrTypeSymbol nsChild)
                    GetAllTypes(result, nsChild);
        }

        private bool IsDerivedFrom(ITypeSymbol type, INamedTypeSymbol baseType)
        {
            var current = type;

            while (current != null)
            {
                if (SymbolEqualityComparer.Default.Equals(current, baseType))
                    return true;

                current = current.BaseType;
            }

            return false;
        }

        private bool IsPartial(INamedTypeSymbol type)
        {
            foreach (var declaration in type.DeclaringSyntaxReferences)
            {
                var syntax = declaration.GetSyntax();
                if (syntax is TypeDeclarationSyntax typeDeclaration)
                {
                    foreach (var modifer in typeDeclaration.Modifiers)
                    {
                        if (modifer.ValueText == "partial")
                            return true;
                    }
                }
            }

            return false;
        }
    }
}
