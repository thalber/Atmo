﻿using System.Collections;
using Atmo.Body;

namespace Atmo.Data;

/// <summary>
/// A set of <see cref="Arg"/>s. Implements IList, so can be indexed by position (<see cref="this[int]"/>), as well as <see cref="Arg"/>'s name <see cref="this[string[]]"/>.
/// <para>
///		ArgSets can be created from string arrays, explicitly (<see cref="ArgSet(string[], bool)"/>)
///		or implicitly (a cast that invokes the same constructor). When instantiating, ArgSet creates 
///		a dictionary-based index of named args. All args are created with linkage. 
/// </para>
/// <para>
///		This class is typically used by delegates in <see cref="Atmo.API"/>,
///		although nothing stops a user from applying it in an unrelated context.
///		Use of ArgSet over simple string arrays was enforced to ensure 
///		consistent argument syntax in various <see cref="Happen"/> actions.
/// </para>
/// </summary>
public sealed class ArgSet : IList<Arg> {
	/// <summary>
	/// Creates the instance from a given array of raw string arguments.
	/// </summary>
	/// <param name="rawargs"></param>
	/// <param name="linkage">Whether to parse argument names and variable links on instantiation. On by default.</param>
	public ArgSet(string[] rawargs, bool linkage = true) {
		for (int i = 0; i < rawargs.Length; i++) {
			Arg newarg = new(
				rawargs[i] ?? string.Empty,
				linkage);
			_args.Add(newarg);
			if (newarg.Name is not null) {
				_named.Add(newarg.Name, newarg);
			}
		}
	}
	private readonly List<Arg> _args = new();
	private readonly Dictionary<string, Arg> _named = new();
	/// <summary>
	/// Checks given names and returns first matching named argument, if any.
	/// <para>
	/// This indexer can be used to easily fetch an argument that may have variant names, for example:
	/// <code>
	///		ArgSet set = myStringArray;
	///		Arg
	///		  cooldown = set["cd", "cooldown"] ?? 80,
	///		  power = set["pow", "power"] ?? 0.3f;
	/// </code>
	/// </para>
	/// </summary>
	/// <param name="names">Names to check</param>
	/// <returns>An <see cref="Arg"/> if one is found, null otherwise.</returns>
	public Arg? this[params string[] names] {
		get {
			foreach (string? name in names) if (_named.TryGetValue(name, out Arg val)) return val;
			return null;
		}
	}
#pragma warning disable CS1591
	#region interface
	/// <summary>
	/// Simple order-based indexing. For non-throwing alternative, you can use <see cref="Utils.AtOr{T}(IList{T}, int, T)"/>.
	/// </summary>
	/// <param name="index"></param>
	/// <returns></returns>
	public Arg this[int index] { get => _args[index]; set => _args[index] = value; }
	public int Count
		=> _args.Count;
	public bool IsReadOnly
		=> false;
	public void Add(Arg item) {
		_args.Add(item);
	}

	public void Clear() {
		_args.Clear();
	}

	public bool Contains(Arg item) {
		return _args.Contains(item);
	}

	public void CopyTo(Arg[] array, int arrayIndex) {
		_args.CopyTo(array, arrayIndex);
	}

	public IEnumerator<Arg> GetEnumerator() {
		return _args.GetEnumerator();
	}

	public int IndexOf(Arg item) {
		return _args.IndexOf(item);
	}

	public void Insert(int index, Arg item) {
		_args.Insert(index, item);
	}

	public bool Remove(Arg item) {
		return _args.Remove(item);
	}

	public void RemoveAt(int index) {
		_args.RemoveAt(index);
	}

	IEnumerator IEnumerable.GetEnumerator() {
		return _args.GetEnumerator();
	}
	#endregion interface;
#pragma warning restore
	/// <summary>
	/// Creates a new ArgSet from a specified string array.
	/// </summary>
	/// <param name="raw"></param>
	public static implicit operator ArgSet(string[] raw) {
		return new(raw);
	}
}
