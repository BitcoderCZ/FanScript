using FanScript.Compiler;
using FanScript.Compiler.Binding;
using FanScript.Compiler.Symbols;
using FanScript.Compiler.Symbols.Functions;
using FanScript.Compiler.Symbols.Variables;
using FanScript.Compiler.Syntax;
using FanScript.Compiler.Text;
using FanScript.Documentation.Attributes;
using FanScript.LangServer.Utils;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FanScript.LangServer.Handlers;

internal class HoverHandler : HoverHandlerBase
{
	private readonly ILanguageServerFacade facade;

	private TextDocumentHandler? documentHandler;

	public HoverHandler(ILanguageServerFacade facade)
	{
		this.facade = facade;
	}

	public override async Task<Hover?> Handle(HoverParams request, CancellationToken cancellationToken)
	{
		documentHandler ??= facade.Workspace.GetService(typeof(TextDocumentHandler)) as TextDocumentHandler;

		if (documentHandler is null)
			return null;

		await Task.Yield();

		Document document = documentHandler.GetDocument(request.TextDocument.Uri);
		SyntaxTree? tree = document.Tree;
		Compilation? compilation = document.Compilation;

		if (tree is null || compilation is null)
			return null;

		var requestSpan = request.Position.ToSpan(tree.Text);
		object? syntax = tree.FindSyntax(requestSpan);

		if (syntax is not SyntaxNode node)
			return null;

		ScopeWSpan? scope = null;
		foreach (var (func, funcScope) in compilation
			.GetScopes()
			.OrderBy(item => item.Value.Span.Length))
		{
			if (funcScope.Span.OverlapsWith(requestSpan))
			{
				scope = funcScope.GetScopeAt(requestSpan.Start);
				break;
			}
		}

		if (node is SyntaxToken sToken)
		{
			if (sToken.Kind.IsModifier())
				return GetHoverForModifier(ModifiersE.FromKind(sToken.Kind), sToken.Location);

			TypeSymbol? type = sToken.Text == TypeSymbol.Null.Name ? TypeSymbol.Null : TypeSymbol.GetType(sToken.Text);

			if (type != TypeSymbol.Error)
				return GetHoverForType(type, sToken.Location);
		}

		switch (node.Parent)
		{
			case NameExpressionSyntax name:
				{
					if (scope is null)
						break;

					if (name.Parent is PropertyExpressionSyntax propEx && name == propEx.Expression)
					{
						PropertySymbol? prop = ResolveProperty(compilation, propEx,
						[
							.. scope.GetAllVariables(),
							.. compilation.GetVariables().Where(var => var.IsGlobal),
						], out _) as PropertySymbol;

						if (prop is not null)
							return GetHoverForProperty(prop, node.Location);
					}
					else
					{
						if (node is not SyntaxToken token)
							break;

						VariableSymbol? varSymbol = scope
							.GetAllVariables()
							.Concat(compilation.GetVariables().Where(var => var.IsGlobal))
							.FirstOrDefault(var => var.Name == token.Text);

						if (varSymbol is not null)
							return GetHoverForVariable(varSymbol, node.Location);
					}
				}

				break;
			case VariableDeclarationStatementSyntax variableDeclarationStatement when node == variableDeclarationStatement.IdentifierToken:
			case AssignmentStatementSyntax assignment when node == assignment.Destination:
				{
					if (scope is null || node is not SyntaxToken token)
						break;

					VariableSymbol? varSymbol = scope
						.GetAllVariables()
						.Concat(compilation.GetVariables().Where(var => var.IsGlobal))
						.FirstOrDefault(var => var.Name == token.Text);

					if (varSymbol is not null)
						return GetHoverForVariable(varSymbol, node.Location);
				}

				break;
			case CallExpressionSyntax call when node == call.Identifier:
				{
					if (call.Parent is PropertyExpressionSyntax propEx && call == propEx.Expression)
					{
						IEnumerable<FunctionSymbol>? funcs = null;
						TypeSymbol? baseType = null;
						if (scope is not null)
							funcs = ResolveProperty(compilation, propEx,
							[
								.. scope.GetAllVariables(),
								.. compilation.GetVariables().Where(var => var.IsGlobal),
							], out baseType) as IEnumerable<FunctionSymbol>;

						if (funcs is not null && baseType is not null)
							return GetHoverForMethods(funcs.ToArray(), baseType, node.Location);
					}
					else
					{
						var functions = compilation
							.GetFunctions()
							.Where(func => func.Name == call.Identifier.Text);

						return GetHoverForFunctions(functions.ToArray(), node.Location);
					}
				}

				break;
			case CallStatementSyntax call when node == call.Identifier:
				{
					var functions = compilation
						.GetFunctions()
						.Where(func => func.Name == call.Identifier.Text);

					return GetHoverForFunctions(functions.ToArray(), node.Location);
				}
			case EventStatementSyntax sb when node == sb.Identifier:
				{
					if (!Enum.TryParse(sb.Identifier.Text, out EventType type))
						break;

					var info = type.GetInfo();
					var doc = DocUtils.GetAttribute<EventType, EventDocAttribute>(type);

					string val = info.ToString();

					if (!string.IsNullOrEmpty(doc.Info))
						val += " - " + doc.Info;

					return new Hover()
					{
						Contents = new MarkedStringsOrMarkupContent(new MarkupContent()
						{
							Kind = MarkupKind.Markdown,
							Value = val
						}),
						Range = node.Location.ToRange(),
					};
				}
			case PostfixExpressionSyntax pe when node == pe.IdentifierToken:
			case PostfixStatementSyntax ps when node == ps.IdentifierToken:
				{
					if (scope is null || node is not SyntaxToken token)
						break;

					VariableSymbol? varSymbol = scope
						   .GetAllVariables()
						   .Concat(compilation.GetVariables().Where(var => var.IsGlobal))
						   .FirstOrDefault(var => var.Name == token.Text);

					if (varSymbol is not null)
						return GetHoverForVariable(varSymbol, node.Location);
				}

				break;
			case BuildCommandStatementSyntax bc when node == bc.Identifier:
				{
					BuildCommand? command = BuildCommandE.Parse(bc.Identifier.Text);

					if (command is null)
						break;

					return GetHoverForBuildCommand(command.Value, node.Location);
				}
		}

		return null;
	}

	protected override HoverRegistrationOptions CreateRegistrationOptions(HoverCapability capability, ClientCapabilities clientCapabilities)
		=> new HoverRegistrationOptions()
		{
			DocumentSelector = TextDocumentSelector.ForLanguage("fanscript"),
		};

	private static object? ResolveProperty(Compilation compilation, PropertyExpressionSyntax syntax, VariableSymbol[] variables, out TypeSymbol? baseType)
	{
		VariableSymbol? baseVar = null;
		baseType = null;

		switch (syntax.BaseExpression)
		{
			case PropertyExpressionSyntax propEx:
				baseVar = ResolveProperty(compilation, propEx, variables, out _) as VariableSymbol;
				break;
			case NameExpressionSyntax nameEx:
				baseVar = variables.FirstOrDefault(var => var.Name == nameEx.IdentifierToken.Text);
				break;
			default:
				return null;
		}

		if (baseVar is null)
			return null;

		baseType = baseVar.Type;

		if (syntax.Expression is NameExpressionSyntax name)
		{
			PropertyDefinitionSymbol? propDef = baseVar.Type.GetProperty(name.IdentifierToken.Text);
			return propDef is null ?
				null :
				new PropertySymbol(propDef, new BoundVariableExpression(null!, baseVar));
		}
		else if (syntax.Expression is CallExpressionSyntax call)
		{
			// method (instance function)
			return compilation
				.GetFunctions()
				.Where(func => func.IsMethod && func.Name == call.Identifier.Text)
				.OrderBy(func => Math.Abs(func.Parameters.Length - call.Arguments.Count));
		}

		return null;
	}

	private static Hover? GetHoverForVariable(VariableSymbol variable, TextLocation location)
	{
		if (variable.Modifiers.HasFlag(Modifiers.Constant) && variable.Modifiers.HasFlag(Modifiers.Global))
		{
			foreach (var group in Constants.Groups)
			{
				if (variable.Name.StartsWith(group.Name, StringComparison.Ordinal))
				{
					for (int i = 0; i < group.Values.Length; i++)
					{
						if (variable.Name == group.Name + "_" + group.Values[i].Name)
						{
							var doc = Constants.ConstantToDoc[group];

							string info = doc.Info is null ? string.Empty : DocUtils.ParseAndBuild(doc.Info, null);

							if (doc.ValueInfos is not null && !string.IsNullOrEmpty(doc.ValueInfos[i]))
								info += "\n" + DocUtils.ParseAndBuild(doc.ValueInfos[i], null);

							return info.Length == 0
								? null
								: new Hover()
								{
									Contents = new MarkedStringsOrMarkupContent(new MarkupContent()
									{
										Kind = MarkupKind.PlainText,
										Value = info,
									}),
									Range = location.ToRange(),
								};
						}
					}
				}
			}
		}

		StringBuilder builder = new StringBuilder();

		variable.Modifiers.ToSyntaxString(builder);
		builder.Append(' ');
		builder.Append(variable.Type);
		builder.Append(' ');
		builder.Append(variable.Name);

		if (variable.Constant is not null)
		{
			builder.Append(" = ");
			builder.Append(variable.Constant.Value);
		}

		return new Hover()
		{
			Contents = new MarkedStringsOrMarkupContent(new MarkupContent()
			{
				Kind = MarkupKind.PlainText,
				Value = builder.ToString(),
			}),
			Range = location.ToRange(),
		};
	}

	private static Hover GetHoverForProperty(PropertySymbol property, TextLocation location)
	{
		StringBuilder builder = new StringBuilder();

		TypeSymbol baseType = property.Expression.Type;
		if (baseType.IsGenericInstance)
			baseType = TypeSymbol.GetGenericDefinition(baseType);

		property.Modifiers.ToSyntaxString(builder);
		builder.Append(' ');
		builder.Append(property.Type);
		builder.Append(' ');
		builder.Append(baseType);
		builder.Append('.');
		builder.Append(property.Name);

		if (property.Constant is not null)
		{
			builder.Append(" = ");
			builder.Append(property.Constant.Value);
		}

		return new Hover()
		{
			Contents = new MarkedStringsOrMarkupContent(new MarkupContent()
			{
				Kind = MarkupKind.PlainText,
				Value = builder.ToString(),
			}),
			Range = location.ToRange(),
		};
	}

	private static Hover? GetHoverForFunctions(FunctionSymbol[] functions, TextLocation location)
	{
		if (functions.Length == 0)
			return null;

		StringBuilder builder = new StringBuilder();
		MarkedString[] results = new MarkedString[functions.Length];

		for (int i = 0; i < functions.Length; i++)
		{
			FunctionSymbol function = functions[i];

			FunctionDocAttribute? doc = function is BuiltinFunctionSymbol ? BuiltinFunctions.FunctionToDoc[function] : new FunctionDocAttribute();

			builder.Append(function.ToString());

			if (!string.IsNullOrEmpty(doc.Info))
			{
				builder.Append(" - ");
				builder.Append(DocUtils.ParseAndBuild(doc.Info, function));
			}

			results[i] = new MarkedString(builder.ToString());

			builder.Clear();
		}

		return new Hover()
		{
			Contents = new MarkedStringsOrMarkupContent(results),
			Range = location.ToRange(),
		};
	}

	private static Hover? GetHoverForMethods(FunctionSymbol[] methods, TypeSymbol baseType, TextLocation location)
	{
		if (methods.Length == 0)
			return null;

		StringBuilder builder = new StringBuilder();
		MarkedString[] results = new MarkedString[methods.Length];

		for (int i = 0; i < methods.Length; i++)
		{
			FunctionSymbol method = methods[i];

			FunctionDocAttribute? doc = method is BuiltinFunctionSymbol ? BuiltinFunctions.FunctionToDoc[method] : new FunctionDocAttribute();
			using StringWriter writer = new StringWriter(builder);

			if (baseType.IsGenericInstance)
				baseType = TypeSymbol.GetGenericDefinition(baseType);

			method.Modifiers.ToSyntaxString(builder);
			builder.Append(' ');
			builder.Append(method.Type);
			builder.Append(' ');
			builder.Append(baseType);
			builder.Append('.');
			builder.Append(method.Name);

			method.WriteTo(writer, true, true);

			if (!string.IsNullOrEmpty(doc.Info))
			{
				builder.Append(" - ");
				builder.Append(DocUtils.ParseAndBuild(doc.Info, method));
			}

			results[i] = new MarkedString(builder.ToString());

			builder.Clear();
		}

		return new Hover()
		{
			Contents = new MarkedStringsOrMarkupContent(results),
			Range = location.ToRange(),
		};
	}

	private static Hover GetHoverForModifier(Modifiers mod, TextLocation location)
	{
		ModifierDocAttribute doc = DocUtils.GetAttribute<Modifiers, ModifierDocAttribute>(mod);

		return new Hover()
		{
			Contents = new MarkedStringsOrMarkupContent(new MarkupContent()
			{
				Kind = MarkupKind.PlainText,
				Value = DocUtils.ParseAndBuild(doc.Info, null),
			}),
			Range = location.ToRange(),
		};
	}

	private static Hover GetHoverForType(TypeSymbol type, TextLocation location)
	{
		TypeDocAttribute doc = TypeSymbol.TypeToDoc[type];

		return new Hover()
		{
			Contents = new MarkedStringsOrMarkupContent(new MarkupContent()
			{
				Kind = MarkupKind.PlainText,
				Value = DocUtils.ParseAndBuild(doc.Info, null),
			}),
			Range = location.ToRange(),
		};
	}

	private static Hover GetHoverForBuildCommand(BuildCommand command, TextLocation location)
	{
		BuildCommandDocAttribute doc = DocUtils.GetAttribute<BuildCommand, BuildCommandDocAttribute>(command);

		return new Hover()
		{
			Contents = new MarkedStringsOrMarkupContent(new MarkupContent()
			{
				Kind = MarkupKind.PlainText,
				Value = DocUtils.ParseAndBuild(doc.Info, null),
			}),
			Range = location.ToRange(),
		};
	}
}
