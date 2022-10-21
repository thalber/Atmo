using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Atmo.Helpers;

/// <summary>
/// A set of <see cref="Arg"/>s. Implements IList, so can be indexed by position, as well as <see cref="Arg"/>'s name.
/// </summary>
public sealed class ArgSet : IList<Arg>
{
	/// <summary>
	/// Creates the instance from a given array of raw string arguments.
	/// </summary>
	/// <param name="rawargs"></param>
	public ArgSet(string[] rawargs)
	{
		for (int i = 0; i < rawargs.Length; i++)
		{
			Arg newarg = new(rawargs[i]);
			_args.Add(newarg);
			if (newarg .Name is not null) { _named.Add(newarg.Name, newarg); }
		}
	}
	private readonly List<Arg> _args = new();
	private readonly Dictionary<string, Arg> _named = new();
	/// <summary>
	/// Checks given names and returns first matching named argument, if any.
	/// </summary>
	/// <param name="names">Names to check</param>
	/// <returns>An <see cref="Arg"/> if one is found, null otherwise.</returns>
	public Arg? this[params string[] names]
	{
		get
		{
			foreach (var name in names) if (_named.TryGetValue(name, out Arg val)) return val;
			return null;
		}
	}

#pragma warning disable CS1591
	#region interface
	#region ilist
	public Arg this[int index] { get => ((IList<Arg>)_args)[index]; set => ((IList<Arg>)_args)[index] = value; }
	public int Count => ((ICollection<Arg>)_args).Count;
	public bool IsReadOnly => ((ICollection<Arg>)_args).IsReadOnly;
	public void Add(Arg item)
	{
		((ICollection<Arg>)_args).Add(item);
	}
	public void Clear()
	{
		((ICollection<Arg>)_args).Clear();
	}
	public bool Contains(Arg item)
	{
		return ((ICollection<Arg>)_args).Contains(item);
	}
	public void CopyTo(Arg[] array, int arrayIndex)
	{
		((ICollection<Arg>)_args).CopyTo(array, arrayIndex);
	}
	public IEnumerator<Arg> GetEnumerator()
	{
		return ((IEnumerable<Arg>)_args).GetEnumerator();
	}
	public int IndexOf(Arg item)
	{
		return ((IList<Arg>)_args).IndexOf(item);
	}
	public void Insert(int index, Arg item)
	{
		((IList<Arg>)_args).Insert(index, item);
	}

	public bool Remove(Arg item)
	{
		return ((ICollection<Arg>)_args).Remove(item);
	}

	public void RemoveAt(int index)
	{
		((IList<Arg>)_args).RemoveAt(index);
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return ((IEnumerable)_args).GetEnumerator();
	}
	#endregion ilist
	#endregion interface;
#pragma warning restore
}
