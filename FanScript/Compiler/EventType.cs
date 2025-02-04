﻿// <copyright file="EventType.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FanScript.Compiler.Symbols;
using FanScript.Compiler.Symbols.Variables;
using FanScript.Documentation.Attributes;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Text;

namespace FanScript.Compiler;

public enum EventType
{
	[EventDoc(
		Info = """
        Executed only on the first frame.
        """)]
	Play,
	[EventDoc(
		Info = """
        Executes after physics, before rendering.
        """,
		Remarks = [
			"""
            Each frame Fancade does the following:
                1. Runs scripts.
                2. Simulates physics.
                3. Runs Late Update scripts.
                4. Renders the frame.
            """
		])]
	LateUpdate,
	[EventDoc(
		Info = """
        Executes when taking a screenshot for the game cover.
        """)]
	BoxArt,
	[EventDoc(
		Info = """
        Executes when a touch is detected.
        """,
		ParamInfos = [
			"""
            The x coordinate of the touch (in pixels).
            """,
			"""
            The y coordinate of the touch (in pixels).
            """,
			"""
            One of <link type="con">TOUCH_STATE</>.
            """,
			"""
            One of <link type="con">TOUCH_FINGER</>.
            """
		])]
	Touch,
	[EventDoc(
		Info = """
        Executes when a swipe is detected.
        """,
		ParamInfos = [
			"""
            Direction of the swipe (a cardinal direction in the XZ plane).
            """
		],
		Remarks = [
			"""
            If the player holds the finger after swiping, swipe is executed every 15 frames.
            """
		])]
	Swipe,
	[EventDoc(
		Info = """
        Creates a button for this frame, executes if the button is pressed.
        """,
		ParamInfos = [
			"""
            Type of the button, one of <link type="con">BUTTON_TYPE</>.
            """
		])]
	Button,
	[EventDoc(
		Info = """
        Executes when an object (object2) collides with object1.
        """,
		ParamInfos = [
			"""
            The object to detect collisions for.
            """,
			"""
            The object which object1 collided with.
            """,
			"""
            Impact force of the collision.
            """,
			"""
            Direction of impact from object2 to object1.
            """,
		],
		Remarks = [
			"""
            If object1 is colliding with multiple objects, the most forceful collision will be reported.
            """,
			"""
            If you're overriding physics to move objects (using Set Position) then no collisions will be detected.
            """
		])]
	Collision,
	[EventDoc(
		Info = """
        Executes multiple times from start to stop.
        """,
		ParamInfos = [
			"""
            The start value (inclusive).
            """,
			"""
            The end value (exclusive).
            """,
			"""
            The current value.
            """
		],
		Examples = """
        <codeblock lang="fcs">
        on Loop(0, 5, out inline float i)
        {
            inspect(i) // [0, 1, 2, 3, 4]
        }
        </>
        <codeblock lang="fcs">
        on Loop(5, 0, out inline float i)
        {
            inspect(i) // [5, 4, 3, 2, 1]
        }
        </>
        """,
		Remarks = [
			"""
            The counter always steps by 1 (or -1, if start is greater than stop).
            """,
			"""
            The counter does not output stop.
            """,
			"""
            If a non-integer value is provided for start, it's rounded down to the next smallest integer.
            """,
			"""
            If a non-integer value is provided for stop, it's rounded up to the next biggest integer.
            """
		])]
	Loop,
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1649:File name should match first type name", Justification = "It does, but enums aren't detected for some reason")]
public static class EventTypeE
{
	private static readonly EventCollection Types = new EventCollection()
	{
		new EventTypeInfo(EventType.Play, []),
		new EventTypeInfo(EventType.LateUpdate, []),
		new EventTypeInfo(EventType.BoxArt, []),
		new EventTypeInfo(EventType.Touch, [
			new ParameterSymbol("screenX", Modifiers.Out, TypeSymbol.Float),
			new ParameterSymbol("screenY", Modifiers.Out, TypeSymbol.Float),
			new ParameterSymbol("TOUCH_STATE", Modifiers.Constant, TypeSymbol.Float),
			new ParameterSymbol("TOUCH_FINGER", Modifiers.Constant, TypeSymbol.Float),
		]),
		new EventTypeInfo(EventType.Swipe, [
			new ParameterSymbol("direction", Modifiers.Out, TypeSymbol.Vector3),
		]),
		new EventTypeInfo(EventType.Button, [
			new ParameterSymbol("BUTTON_TYPE", Modifiers.Constant, TypeSymbol.Float),
		]),
		new EventTypeInfo(EventType.Collision, [
			new ParameterSymbol("object1", 0, TypeSymbol.Object),
			new ParameterSymbol("object2", Modifiers.Out, TypeSymbol.Object),
			new ParameterSymbol("impulse", Modifiers.Out, TypeSymbol.Float),
			new ParameterSymbol("normal", Modifiers.Out, TypeSymbol.Vector3),
		]),
		new EventTypeInfo(EventType.Loop, [
			new ParameterSymbol("start", 0, TypeSymbol.Float),
			new ParameterSymbol("stop", 0, TypeSymbol.Float),
			new ParameterSymbol("counter", Modifiers.Out, TypeSymbol.Float),
		]),
	};

	public static EventTypeInfo GetInfo(this EventType sbt)
		=> Types[sbt];

	private sealed class EventCollection : KeyedCollection<EventType, EventTypeInfo>
	{
		protected override EventType GetKeyForItem(EventTypeInfo item)
			=> item.Type;
	}
}

public record EventTypeInfo(EventType Type, ImmutableArray<ParameterSymbol> Parameters)
{
	public override string ToString()
	{
		StringBuilder builder = new StringBuilder()
			.Append("on ")
			.Append(Type)
			.Append('(');

		for (int i = 0; i < Parameters.Length; i++)
		{
			var param = Parameters[i];

			if (i != 0)
			{
				builder.Append(", ");
			}

			if (param.Modifiers != 0)
			{
				param.Modifiers.ToSyntaxString(builder);
				builder.Append(' ');
			}

			builder.Append(param.Type.ToString());
			builder.Append(' ');
			builder.Append(param.Name);
		}

		return builder
			.Append(')')
			.ToString();
	}
}
