// <copyright file="BuiltinFunctions.Physics.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FanScript.Compiler.Symbols.Functions;
using FanScript.Compiler.Symbols.Variables;
using FanScript.Documentation.Attributes;
using FanScript.FCInfo;

namespace FanScript.Compiler.Symbols;

internal static partial class BuiltinFunctions
{
	private static class Physics
	{
		[FunctionDoc(
			Info = """
            Adds <link type="param">force</> and/or <link type="param">torque</> to <link type="param">object</>.
            """,
			ParameterInfos = [
				"""
                The object that the force will be applied to.
                """,
				"""
                The force to apply to <link type="param">object</>.
                """,
				"""
                Where on <link type="param">object</> should <link type="param">force</> be applied at (center of mass by default).
                """,
				"""
                The rotational force to apply to <link type="param">object</>.
                """
			],
			Related = [
				"""
                <link type="func">setVelocity;obj;vec3;vec3</>
                """,
				"""
                <link type="func">getVelocity;obj;vec3;vec3</>
                """
			])]
		public static readonly FunctionSymbol AddForce
			= new BuiltinFunctionSymbol(
				PhysicsNamespace,
				"addForce",
				[
					new ParameterSymbol("object", TypeSymbol.Object),
					new ParameterSymbol("force", TypeSymbol.Vector3),
					new ParameterSymbol("applyAt", TypeSymbol.Vector3),
					new ParameterSymbol("torque", TypeSymbol.Vector3),
				],
				TypeSymbol.Void,
				(call, context) => EmitAX0(call, context, Blocks.Physics.AddForce))
			{
				IsMethod = true,
			};

		[FunctionDoc(
			Info = """
            Gets the <link type="param">object</>'s velocity.
            """,
			ParameterInfos = [
				null,
				"""
                Linear velocity of <link type="param">object</> (units/second).
                """,
				"""
                Angular velocity of <link type="param">object</> (degrees/second).
                """
			],
			Related = [
				"""
                <link type="func">setVelocity;obj;vec3;vec3</>
                """,
				"""
                <link type="func">addForce;obj;vec3;vec3;vec3</>
                """
			])]
		public static readonly FunctionSymbol GetVelocity
			= new BuiltinFunctionSymbol(
				PhysicsNamespace,
				"getVelocity",
				[
					new ParameterSymbol("object", TypeSymbol.Object),
					new ParameterSymbol("velocity", Modifiers.Out, TypeSymbol.Vector3),
					new ParameterSymbol("spin", Modifiers.Out, TypeSymbol.Vector3),
				],
				TypeSymbol.Void,
				(call, context) => EmitXX(call, context, 2, Blocks.Physics.GetVelocity))
			{
				IsMethod = true,
			};

		[FunctionDoc(
			Info = """
            Sets the linear and angular velocity of <link type="param">object</>.
            """,
			ParameterInfos = [
				null,
				"""
                The linear velocity (units/second).
                """,
				"""
                The angular velocity (degrees/second).
                """
			],
			Related = [
				"""
                <link type="func">getVelocity;obj;vec3;vec3</>
                """,
				"""
                <link type="func">addForce;obj;vec3;vec3;vec3</>
                """
			])]
		public static readonly FunctionSymbol SetVelocity
			= new BuiltinFunctionSymbol(
				PhysicsNamespace,
				"setVelocity",
				[
					new ParameterSymbol("object", TypeSymbol.Object),
					new ParameterSymbol("velocity", TypeSymbol.Vector3),
					new ParameterSymbol("spin", TypeSymbol.Vector3),
				],
				TypeSymbol.Void,
				(call, context) => EmitAX0(call, context, Blocks.Physics.SetVelocity))
			{
				IsMethod = true,
			};

		[FunctionDoc(
			Info = """
            Restricts <link type="param">object</>'s movement, forces applied to <link type="param">object</> are multiplied by <link type="param">position</> and <link type="param">rotation</>.
            """,
			ParameterInfos = [
				null,
				"""
                The movement multiplier.
                """,
				"""
                The rotation multiplier.
                """
			],
			Remarks = [
				"""
                Negative numbers reverse physics and numbers bigger than 1 increase them.
                """
			])]
		public static readonly FunctionSymbol SetLocked
			= new BuiltinFunctionSymbol(
				PhysicsNamespace,
				"setLocked",
				[
					new ParameterSymbol("object", TypeSymbol.Object),
					new ParameterSymbol("position", TypeSymbol.Vector3),
					new ParameterSymbol("rotation", TypeSymbol.Vector3),
				],
				TypeSymbol.Void,
				(call, context) => EmitAX0(call, context, Blocks.Physics.SetLocked))
			{
				IsMethod = true,
			};

		[FunctionDoc(
			Info = """
            Sets the mass of <link type="param">object</>.
            """,
			ParameterInfos = [
				null,
				"""
                The new mass of <link type="param">object</> (the default values is determined by the volume of <link type="param">object</>'s collider).
                """
			])]
		public static readonly FunctionSymbol SetMass
			= new BuiltinFunctionSymbol(
				PhysicsNamespace,
				"setMass",
				[
					new ParameterSymbol("object", TypeSymbol.Object),
					new ParameterSymbol("mass", TypeSymbol.Float),
				],
				TypeSymbol.Void,
				(call, context) => EmitAX0(call, context, Blocks.Physics.SetMass))
			{
				IsMethod = true,
			};

		[FunctionDoc(
			Info = """
            Sets the friction of <link type="param">object</>.
            """,
			ParameterInfos = [
				null,
				"""
                How much friction to apply to <link type="param">object</> when colliding with other objects (0.5 by default).
                """
			])]
		public static readonly FunctionSymbol SetFriction
			= new BuiltinFunctionSymbol(
				PhysicsNamespace,
				"setFriction",
				[
					new ParameterSymbol("object", TypeSymbol.Object),
					new ParameterSymbol("friction", TypeSymbol.Float),
				],
				TypeSymbol.Void,
				(call, context) => EmitAX0(call, context, Blocks.Physics.SetFriction))
			{
				IsMethod = true,
			};

		[FunctionDoc(
			Info = """
            Sets how bouncy <link type="param">object</> is (how much momentum it retains after each bounce).
            """,
			ParameterInfos = [
				null,
				"""
                How much momentum <link type="param">object</> retains after each bounce (0 - 1, if higher, <link type="param">object</>'s velocity will increase after each jump).
                """
			])]
		public static readonly FunctionSymbol SetBounciness
			= new BuiltinFunctionSymbol(
				PhysicsNamespace,
				"setBounciness",
				[
					new ParameterSymbol("object", TypeSymbol.Object),
					new ParameterSymbol("bounciness", TypeSymbol.Float),
				],
				TypeSymbol.Void,
				(call, context) => EmitAX0(call, context, Blocks.Physics.SetBounciness))
			{
				IsMethod = true,
			};

		[FunctionDoc(
			Info = """
            Sets the direction (and magnitude) in which any physics objects falls.
            """,
			ParameterInfos = [
				"""
                Default is (0, -9.8, 0).
                """
			])]
		public static readonly FunctionSymbol SetGravity
			= new BuiltinFunctionSymbol(
				PhysicsNamespace,
				"setGravity",
				[
					new ParameterSymbol("gravity", TypeSymbol.Vector3),
				],
				TypeSymbol.Void,
				(call, context) => EmitAX0(call, context, Blocks.Physics.SetGravity));

		[FunctionDoc(
			Info = """
            Creates a <link type="param">constraint</> (an invisible connection (rod) between 2 objects) between <link type="param">part</> and <link type="param">base</>.
            """,
			ParameterInfos = [
				"""
                The object that will be glued on.
                """,
				"""
                The object that will be glued on the <link type="param">base</>.
                """,
				"""
                The other end of the constraint rod.
                """,
				"""
                The created constraint.
                """
			],
			Related = [
				"""
                <link type="func">linearLimits;constr;vec3;vec3</>
                """,
				"""
                <link type="func">angularLimits;constr;vec3;vec3</>
                """,
				"""
                <link type="func">linearSpring;constr;vec3;vec3</>
                """,
				"""
                <link type="func">angularSpring;constr;vec3;vec3</>
                """,
				"""
                <link type="func">linearMotor;constr;vec3;vec3</>
                """,
				"""
                <link type="func">angularMotor;constr;vec3;vec3</>
                """
			])]
		public static readonly FunctionSymbol AddConstraint
			= new BuiltinFunctionSymbol(
				PhysicsNamespace,
				"addConstraint",
				[
					new ParameterSymbol("base", TypeSymbol.Object),
					new ParameterSymbol("part", TypeSymbol.Object),
					new ParameterSymbol("pivot", TypeSymbol.Vector3),
					new ParameterSymbol("constraint", Modifiers.Out, TypeSymbol.Constraint),
				],
				TypeSymbol.Void,
				(call, context) => EmitAXX(call, context, 1, Blocks.Physics.AddConstraint))
			{
				IsMethod = true,
			};

		[FunctionDoc(
			Info = """
            Sets the linear limits of <link type="param">constraint</>.
            """,
			ParameterInfos = [
				null,
				"""
                The lower limit.
                """,
				"""
                The upper limit.
                """
			],
			Related = [
				"""
                <link type="func">addConstraint;obj;obj;vec3;constr</>
                """
			])]
		public static readonly FunctionSymbol LinearLimits
			= new BuiltinFunctionSymbol(
				PhysicsNamespace,
				"linearLimits",
				[
					new ParameterSymbol("constraint", TypeSymbol.Constraint),
					new ParameterSymbol("lower", TypeSymbol.Vector3),
					new ParameterSymbol("upper", TypeSymbol.Vector3),
				],
				TypeSymbol.Void,
				(call, context) => EmitAX0(call, context, Blocks.Physics.LinearLimits))
			{
				IsMethod = true,
			};

		[FunctionDoc(
			Info = """
            Sets the angular limits of <link type="param">constraint</>.
            """,
			ParameterInfos = [
				null,
				"""
                The lower angular limit (in degrees).
                """,
				"""
                The upper angular limit (in degrees).
                """
			],
			Related = [
				"""
                <link type="func">addConstraint;obj;obj;vec3;constr</>
                """
			])]
		public static readonly FunctionSymbol AngularLimits
			= new BuiltinFunctionSymbol(
				PhysicsNamespace,
				"angularLimits",
				[
					new ParameterSymbol("constraint", TypeSymbol.Constraint),
					new ParameterSymbol("lower", TypeSymbol.Vector3),
					new ParameterSymbol("upper", TypeSymbol.Vector3),
				],
				TypeSymbol.Void,
				(call, context) => EmitAX0(call, context, Blocks.Physics.AngularLimits))
			{
				IsMethod = true,
			};

		[FunctionDoc(
			Info = """
            Makes the constraint springy, <link type="func">linearLimits;constr;vec3;vec3</> must be called before for linear spring to work.
            """,
			ParameterInfos = [
				null,
				"""
                How stiff the sping will be.
                """,
				"""
                How much damping (drag) to apply.
                """
			],
			Related = [
				"""
                <link type="func">addConstraint;obj;obj;vec3;constr</>
                """
			])]
		public static readonly FunctionSymbol LinearSpring
			= new BuiltinFunctionSymbol(
				PhysicsNamespace,
				"linearSpring",
				[
					new ParameterSymbol("constraint", TypeSymbol.Constraint),
					new ParameterSymbol("stiffness", TypeSymbol.Vector3),
					new ParameterSymbol("damping", TypeSymbol.Vector3),
				],
				TypeSymbol.Void,
				(call, context) => EmitAX0(call, context, Blocks.Physics.LinearSpring))
			{
				IsMethod = true,
			};

		[FunctionDoc(
			Info = """
            Makes the constraint springy, <link type="func">angularLimits;constr;vec3;vec3</> must be called before for angular spring to work.
            """,
			ParameterInfos = [
				null,
				"""
                How stiff the sping will be.
                """,
				"""
                How much damping (drag) to apply.
                """
			],
			Related = [
				"""
                <link type="func">addConstraint;obj;obj;vec3;constr</>
                """
			])]
		public static readonly FunctionSymbol AngularSpring
			= new BuiltinFunctionSymbol(
				PhysicsNamespace,
				"angularSpring",
				[
					new ParameterSymbol("constraint", TypeSymbol.Constraint),
					new ParameterSymbol("stiffness", TypeSymbol.Vector3),
					new ParameterSymbol("damping", TypeSymbol.Vector3),
				],
				TypeSymbol.Void,
				(call, context) => EmitAX0(call, context, Blocks.Physics.AngularSpring))
			{
				IsMethod = true,
			};

		[FunctionDoc(
			Info = """
            Makes <link type="param">constraint</> move.
            """,
			ParameterInfos = [
				null,
				"""
                The speed at which to move at.
                """,
				"""
                How much force to apply.
                """
			],
			Related = [
				"""
                <link type="func">addConstraint;obj;obj;vec3;constr</>
                """
			])]
		public static readonly FunctionSymbol LinearMotor
			= new BuiltinFunctionSymbol(
				PhysicsNamespace,
				"linearMotor",
				[
					new ParameterSymbol("constraint", TypeSymbol.Constraint),
					new ParameterSymbol("speed", TypeSymbol.Vector3),
					new ParameterSymbol("force", TypeSymbol.Vector3),
				],
				TypeSymbol.Void,
				(call, context) => EmitAX0(call, context, Blocks.Physics.LinearMotor))
			{
				IsMethod = true,
			};

		[FunctionDoc(
			Info = """
            Makes <link type="param">constraint</> rotate.
            """,
			ParameterInfos = [
				null,
				"""
                The speed at which to rotate at.
                """,
				"""
                How much force to apply.
                """
			],
			Related = [
				"""
                <link type="func">addConstraint;obj;obj;vec3;constr</>
                """
			])]
		public static readonly FunctionSymbol AngularMotor
			= new BuiltinFunctionSymbol(
				PhysicsNamespace,
				"angularMotor",
				[
					new ParameterSymbol("constraint", TypeSymbol.Constraint),
					new ParameterSymbol("speed", TypeSymbol.Vector3),
					new ParameterSymbol("force", TypeSymbol.Vector3),
				],
				TypeSymbol.Void,
				(call, context) => EmitAX0(call, context, Blocks.Physics.AngularMotor))
			{
				IsMethod = true,
			};

		private static readonly Namespace PhysicsNamespace = BuiltinNamespace + "physics";
	}
}
