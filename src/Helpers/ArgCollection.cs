using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Atmo.Helpers;

public sealed class ArgCollection : IList<ArgWrap>
{
	public ArgCollection(string[] rawargs)
	{
		for (int i = 0; i < rawargs.Length; i++)
		{
			ArgWrap newarg = new(rawargs[i]);
			_args.Add(newarg);
			if (newarg .Name is not null) { _named.Add(newarg.Name, newarg); }
		}
	}

	private readonly List<ArgWrap> _args = new();
	private readonly Dictionary<string, ArgWrap> _named = new();
	public ArgWrap? this[string argname] => _named.TryGetValue(argname, out var val) ? val : null;
	#region interface
	#region ilist
	public ArgWrap this[int index] { get => ((IList<ArgWrap>)_args)[index]; set => ((IList<ArgWrap>)_args)[index] = value; }
	public int Count => ((ICollection<ArgWrap>)_args).Count;
	public bool IsReadOnly => ((ICollection<ArgWrap>)_args).IsReadOnly;
	public void Add(ArgWrap item)
	{
		((ICollection<ArgWrap>)_args).Add(item);
	}
	public void Clear()
	{
		((ICollection<ArgWrap>)_args).Clear();
	}
	public bool Contains(ArgWrap item)
	{
		return ((ICollection<ArgWrap>)_args).Contains(item);
	}
	public void CopyTo(ArgWrap[] array, int arrayIndex)
	{
		((ICollection<ArgWrap>)_args).CopyTo(array, arrayIndex);
	}
	public IEnumerator<ArgWrap> GetEnumerator()
	{
		return ((IEnumerable<ArgWrap>)_args).GetEnumerator();
	}
	public int IndexOf(ArgWrap item)
	{
		return ((IList<ArgWrap>)_args).IndexOf(item);
	}
	public void Insert(int index, ArgWrap item)
	{
		((IList<ArgWrap>)_args).Insert(index, item);
	}

	public bool Remove(ArgWrap item)
	{
		return ((ICollection<ArgWrap>)_args).Remove(item);
	}

	public void RemoveAt(int index)
	{
		((IList<ArgWrap>)_args).RemoveAt(index);
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return ((IEnumerable)_args).GetEnumerator();
	}
	#endregion ilist
	#endregion interface;

	#region casts
	public static implicit operator ArgCollection(string[] argsraw) => new(argsraw);
	#endregion
}
