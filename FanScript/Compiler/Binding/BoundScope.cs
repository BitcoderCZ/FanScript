// <copyright file="BoundScope.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FanScript.Compiler.Symbols;
using FanScript.Compiler.Symbols.Functions;
using FanScript.Compiler.Symbols.Variables;
using FanScript.Compiler.Text;
using System.Collections.Immutable;

namespace FanScript.Compiler.Binding;

internal sealed class BoundScope
{
	private readonly Dictionary<string, VariableSymbol> _variables = [];
	private readonly Dictionary<string, List<FunctionSymbol>> _functions = [];

	private readonly List<BoundScope> _childScopes = [];

	private TextSpan _span;

	public BoundScope()
	{
	}

	private BoundScope(BoundScope? parent)
	{
		Parent = parent;
	}

	public TextSpan Span
	{
		get
		{
			TextSpan sp = _span;

			for (int i = 0; i < _childScopes.Count; i++)
			{
				sp = TextSpan.Combine(sp, _childScopes[i].Span);
			}

			return sp;
		}
	}

	public BoundScope? Parent { get; }

	public BoundScope AddChild()
	{
		BoundScope child = new BoundScope(this);
		_childScopes.Add(child);
		return child;
	}

	public void AddSpan(TextSpan span)
		=> _span = _span == default
			? span
			: TextSpan.FromBounds(
				Math.Min(_span.Start, span.Start),
				Math.Max(_span.End, span.End));

	public ScopeWSpan GetWithSpan()
		=> new ScopeWSpan(_variables.Values, Span, null, _childScopes.Select(child => child.GetWithSpan()));

	public bool TryDeclareVariable(VariableSymbol variable)
	{
		if (VariablelExists(variable))
		{
			return false;
		}

		if (variable.IsGlobal)
		{
			GetTopScope()._variables.Add(variable.Name, variable);
		}
		else
		{
			_variables.Add(variable.Name, variable);
		}

		return true;
	}

	public bool TryDeclareFunction(FunctionSymbol function)
	{
		if (FunctionExists(function))
		{
			return false;
		}

		if (!_functions.TryGetValue(function.Name, out var symbolList))
		{
			symbolList = [];
			_functions.Add(function.Name, symbolList);
		}

		symbolList.Add(function);
		return true;
	}

	public Symbol? TryLookupVariable(string name)
		=> _variables.TryGetValue(name, out var variable) ? variable : Parent?.TryLookupVariable(name);

	public FunctionSymbol? TryLookupFunction(string name, IEnumerable<TypeSymbol> arguments, bool method)
	{
		int argumentCount = arguments.Count();

		if (_functions.TryGetValue(name, out var list))
		{
			foreach (var function in list)
			{
				if ((!method || function.IsMethod) &&
					function.Parameters
						.Select(param => param.Type)
						.SequenceEqual(arguments, new TypeSymbol.FuntionParamsComparer()))
				{
					return function;
				}
			}
		}

		FunctionSymbol? result = Parent?.TryLookupFunction(name, arguments, method);
		return result is not null
			? result
			: _functions.TryGetValue(name, out var funcs)
			? funcs
				.Where(func => func.Name == name && (!method || func.IsMethod))
				.OrderBy(func =>
				{
					int diff = func.Parameters.Length - argumentCount;
					if (diff < 0)
					{
						return Math.Abs(diff) + 2; // make funcs with lees params than args choosen less
					}

					return diff;
				})
				.FirstOrDefault()
			: null;
	}

	public ImmutableArray<VariableSymbol> GetDeclaredVariables()
		=> [.. _variables.Values];

	public ImmutableArray<VariableSymbol> GetAllDeclaredVariables()
	{
		ImmutableArray<VariableSymbol>.Builder builder = ImmutableArray.CreateBuilder<VariableSymbol>(_variables.Count);

		builder.AddRange(_variables.Values);

		BoundScope? current = Parent;

		while (current is not null)
		{
			builder.AddRange(current._variables.Values);
			current = current.Parent;
		}

		return builder.ToImmutable();
	}

	public ImmutableArray<FunctionSymbol> GetDeclaredFunctions()
		=> _functions.Values.SelectMany(list => list).ToImmutableArray();

	private bool VariablelExists(VariableSymbol variable)
		=> _variables.ContainsKey(variable.Name) || (Parent?.VariablelExists(variable) ?? false);

	private bool FunctionExists(FunctionSymbol function)
	{
		if (_functions.TryGetValue(function.Name, out var symbolList))
		{
			if (symbolList.Where(symbol => symbol is FunctionSymbol)
				   .Where(func => Enumerable.SequenceEqual(
					   func.Parameters.Select(param => param.Type),
					   function.Parameters.Select(param => param.Type)))
				   .Count() != 0)
			{
				return true;
			}
		}

		return Parent?.FunctionExists(function) ?? false;
	}

	private BoundScope GetTopScope()
	{
		BoundScope current = this;

		while (current.Parent is not null)
		{
			current = current.Parent;
		}

		return current;
	}
}
