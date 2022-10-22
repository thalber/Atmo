﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using static Atmo.Helpers.Utils;

namespace Atmo.Helpers;
/// <summary>
/// Wraps a string argument for easy conversion into several other language primitives. Can be named (named arguments come in form of "name=value").
/// <para>
/// Args most frequently come in form of <seealso cref="ArgSet"/>s. Arg supports several primitive types: <see cref="int"/>, <see cref="float"/>, <see cref="string"/> and <see cref="bool"/>, and does its best to convert between them (for more details, see docstrings for property accessors). You can implicitly cast from supported primitive types to Arg:
/// <code>
///		<see cref="Arg"/> x = 1,
///		y = 2f,
///		z = "three",
///		w = false;
/// </code>
/// and do explicit conversions the other way around
/// (alternatively, use getters of <see cref="I32"/>/<see cref="F32"/>/<see cref="Bool"/>/<see cref="Str"/>):
/// <code>
///		<see cref="Arg"/> arg = new(12);
///		<see cref="float"/> fl = (<see cref="float"/>)arg; // 12f
///		<see cref="bool"/> bo = (<see cref="bool"/>)arg; // true
///		<see cref="string"/> st = (<see cref="string"/>)arg; // "12"
/// </code>
/// The reason conversions to primitives are explicit is because in Rain World modding 
/// you will often have tangled math blocks, where an incorrectly inferred int/float division 
/// can cause a hard to catch rounding logic error and be very annoying to debug.
/// </para>
/// <para>
/// When created from a string (<see cref="Arg(string, bool)"/> constructor, when <c>linkage</c> is <c>true</c>), an Arg can:
/// <list type="bullet">
///		<item>
///			Become named. This happens when the provided string contains at least one equal sign character (<c>=</c>).
///			Part before that becomes Arg's name, part after that is parsed into contents.
///		</item>
///		<item>
///			Become linked to a variable. This happens when value part of the source string begins with a
///			Dollar sign (<c>$</c>). An Arg that references a variable ignores 
///			its own inner state and accesses the variable object instead.
///			See <seealso cref="VarRegistry"/> for var storage details.
///		</item>
/// </list>
/// </para>
/// </summary>
public sealed class Arg
{
	private bool _skipparse;
	private Arg? _var;
	private string _raw;
	private string _str;
	private int _i32;
	private float _f32;
	private bool _bool;
	private void _parseStr()
	{
		float.TryParse(_str, out _f32);
		//F32 = asfloat;
		int.TryParse(_str, out _i32);
		bool.TryParse(_str, out _bool);
		if (trueStrings.Contains(_str.ToLower())) _bool = true;
		if (falseStrings.Contains(_str.ToLower())) _bool = false;
	}
#pragma warning disable CS8602 // Dereference of a possibly null reference.
	/// <summary>
	/// Raw string previously used to create the argument.
	/// </summary>
	public string Raw
	{
		get => IsVar ? _var.Raw : _raw;
		private set
		{
			if (value is null) throw new ArgumentNullException(nameof(value));
			if (IsVar) { _var.Raw = value; return; }
			_raw = value;
			Name = null;
			var splPoint = _raw.IndexOf('=');
			if (splPoint is not -1 && splPoint < _raw.Length - 1)
			{
				Name = _raw.Substring(0, splPoint);
				value = value.Substring(splPoint + 1);
			}
			if (value.StartsWith("$"))
			{
				_var = VarRegistry.GetVar(value.Substring(1));
			}
			else
			{
				_skipparse = false;
				Str = value;
			}

			//_parseStr();
		}
	}
	/// <summary>
	/// String value of the argument. If the argument is unnamed, this is equivalent to <see cref="Raw"/>; if the argument is named, returns everything after first "=" character.
	/// </summary>
	public string Str
	{
		get => IsVar ? _var.Str : _str;
		private set
		{
			if (IsVar) { _var.Str = value; }
			else
			{
				_str = value;
				if (!_skipparse) _parseStr();
				_skipparse = false;
			}
		}
	}
	/// <summary>
	/// Int value of the argument. 0 if int value couldn't be parsed; rounded if <see cref="Arg"/> is created from a float; 1 or 0 if created from a bool.
	/// </summary>
	public int I32
	{
		get => IsVar ? _var.I32 : _i32;
		private set
		{
			if (IsVar) { _var.I32 = value; return; }
			_skipparse = true;
			_i32 = value;
			_f32 = value;
			_bool = value is not 0;
			Str = value.ToString();
		}
	}
	/// <summary>
	/// Float value of the argument; 0f if float value couldn't be parsed; equal to <see cref="I32"/> if <see cref="Arg"/> is created from an int (may lose precision on large values!); 1f/0f if created from a bool.
	/// </summary>
	public float F32
	{
		get => IsVar ? _var.F32 : _f32;
		private set
		{
			if (IsVar) { _var.F32 = value; return; }
			_f32 = value;
			_i32 = (int)value;
			_bool = value is not 0f;
			_skipparse = true;
			Str = value.ToString();
		}
	}
	/// <summary>
	/// Boolean value of the argument; false by default. False if original string is found in <see cref="Utils.falseStrings"/>, or if <see cref="Arg"/> is created from a zero int or float; True if original string is found in <see cref="Utils.trueStrings"/>, or of <see cref="Arg"/> is created from a non-zero int or float.
	/// </summary>
	public bool Bool
	{
		get => IsVar ? _var.Bool : _bool;
		private set
		{
			if (IsVar) { _var.Bool = value; return; }
			_bool = value;
			_i32 = value ? 1 : 0;
			_f32 = value ? 1 : 0;
			_skipparse = true;
			Str = value.ToString();
		}
	}
	/// <summary>
	/// Attempts to convert value of the current instance into a specified enum. Perf note: each call parses <see cref="Str"/> or invokes <see cref="Convert.ChangeType(object, Type)"/> to convert <see cref="I32"/>'s current value if parsing fails.
	/// </summary>
	/// <typeparam name="T">Type of the enum.</typeparam>
	/// <param name="value">Out param. Contains resulting enum value.</param>
#pragma warning restore CS8602 // Dereference of a possibly null reference.
	public void GetEnum<T> (out T value) 
		where T : Enum
	{
		if (TryParseEnum(Str, out value)) { return; }
		value = (T)Convert.ChangeType(I32, Enum.GetUnderlyingType(typeof(T)));
	}
	/// <summary>
	/// Sets value of current instance to specified enum. Perf note: each call invokes <see cref="Convert.ChangeType(object, Type)"/>. Will throw if your enum's underlying type can not be converted into an I32 (if value is out of range). Will set <see cref="I32"/> and <see cref="F32"/> to value of provided enum variant.
	/// </summary>
	/// <typeparam name="T">Type of the enum.</typeparam>
	/// <param name="value">Value to be set.</param>
	public void SetEnum<T>(in T value)
		where T : Enum
	{
		_str = value.ToString();
		I32 = (int)Convert.ChangeType(value, typeof(int));
	}
	/// <summary>
	/// Name of the argument; null if unnamed.
	/// </summary>
	public string? Name { get; private set; } = null;
	/// <summary>
	/// Indicates whether this instance is linked to a variable. If yes, all property accessors will lead to associated variable, and the instance's internal state will be ignored.
	/// </summary>
	public bool IsVar => _var is not null;
	//todo: allow changing names?
	#region ctors
	/// <summary>
	/// Creates the structure from a given string.
	/// </summary>
	/// <param name="orig">String to create argument from. Named arguments receive "name=value" type strings here. Can not be null.</param>
	/// <param name="linkage">Whether to check the provided string's structure, determining name and linking to a variable if needed. Off by default, for implicit casts</param>
	public Arg(string orig!!, bool linkage = false)
	{
		//todo: add a variant that allows to construct from a string without checking structure?
		_raw = orig;
		_f32 = default;
		_i32 = default;
		_bool = default;
		if (linkage) Raw = orig;
		else
		{
			_str = orig;
			_parseStr();
		}
		_str ??= string.Empty;
	}
	/// <summary>
	/// Creates the structure from a given int. Always unnamed. Mostly used for implicit casts.
	/// </summary>
	/// <param name="val"></param>
	public Arg(int val)
	{
		I32 = val;
		_raw = val.ToString();
		_str ??= string.Empty;
	}
	/// <summary>
	/// Creates the structure from a given float. Always unnamed. Mostly used for implicit casts.
	/// </summary>
	/// <param name="val"></param>
	public Arg(float val)
	{
		F32 = val;
		_raw = val.ToString();
		_str ??= string.Empty;
	}
	/// <summary>
	/// Creates the structure from a given bool. Always unnamed. Mostly used for implicit casts.
	/// </summary>
	/// <param name="val"></param>
	public Arg(bool val)
	{
		Bool = val;
		_raw = val.ToString();
		_str ??= string.Empty;
	}
	/// <summary>
	/// Creates a new instance that wraps another as a variable, with an optional name.
	/// </summary>
	/// <param name="val">Another arg instance that serves as a variable. Must not be null.</param>
	/// <param name="name">Name of the new instance.</param>
	public Arg(Arg val!!, string? name = null)
	{
		_var = val;
		Name = name;
		_raw = _str = string.Empty;
	}
	#endregion
	#region casts
	/// <summary>
	/// Converts an instance into a string.
	/// </summary>
	/// <param name="arg"></param>
	public static explicit operator string(Arg arg) => arg.Str;
	/// <summary>
	/// Creates an instance from a string.
	/// </summary>
	/// <param name="src"></param>
	public static implicit operator Arg(string src) => new(src, false);
	/// <summary>
	/// Converts an instance into an int.
	/// </summary>
	/// <param name="arg"></param>
	public static explicit operator int(Arg arg) => arg.I32;
	/// <summary>
	/// Creates an unnamed instance from an int.
	/// </summary>
	/// <param name="src"></param>
	public static implicit operator Arg(int src) => new(src);
	/// <summary>
	/// Converts an instance into a float.
	/// </summary>
	/// <param name="arg"></param>
	public static explicit operator float(Arg arg) => arg.F32;
	/// <summary>
	/// Creates an unnamed instance from a float.
	/// </summary>
	/// <param name="src"></param>
	public static implicit operator Arg(float src) => new(src);
	/// <summary>
	/// Converts an instance into a bool.
	/// </summary>
	/// <param name="arg"></param>
	public static explicit operator bool(Arg arg) => arg.Bool;
	/// <summary>
	/// Creates an unnamed instance from a bool.
	/// </summary>
	/// <param name="src"></param>
	public static implicit operator Arg(bool src) => new(src);
	#endregion;
}