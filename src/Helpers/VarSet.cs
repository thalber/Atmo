using System.Runtime.Serialization;

using static Atmo.Helpers.VarRegistry;

using NamedVars = System.Collections.Generic.Dictionary<string, Atmo.Helpers.Arg>;
using SerDict = System.Collections.Generic.Dictionary<string, object>;
using Save = Atmo.Helpers.Utils.VT<int, int>;

namespace Atmo.Helpers;
/// <summary>
/// Represents variable storage for a single character save on a single slot.
/// </summary>
internal class VarSet
{
	//todo: add clearing
	#region fields
	private readonly NamedVars persistent = new();
	private readonly NamedVars normal = new();
	#endregion fields
	internal VarSet(Save save)
	{

	}

	internal Arg GetVar(string name)
	{
		DataSection sec = DataSection.Normal;
		if (name.StartsWith(PREFIX_PERSISTENT))
		{
			sec = DataSection.Persistent;
			name = name.Substring(PREFIX_PERSISTENT.Length);
		}
		return GetVar(name, sec);
	}
	internal Arg GetVar(string name, DataSection section)
	{
		NamedVars vars = DictForSection(section);
		Arg _def = Defarg;
		return vars.AddIfNone_Get(name, () => _def);
	}
	internal SerDict GetSer(DataSection section)
	{
		SerDict res = new();
		Arg _def = Defarg;
		NamedVars tdict = DictForSection(section);
		foreach (KeyValuePair<string, Arg> kvp in tdict)
		{
			if (kvp.Value.Equals(_def)) continue;
			res.Add(kvp.Key, kvp.Value._str);
		}
		return res;
	}
	internal void FillFrom(SerDict dict, DataSection section)
	{
		NamedVars tdict = DictForSection(section);
		tdict.Clear();
		foreach (KeyValuePair<string, object> kvp in dict) {
			tdict.Add(kvp.Key, kvp.Value.ToString());
		}
	}
	private NamedVars DictForSection(DataSection sec) => sec switch
	{
		DataSection.Normal => normal,
		DataSection.Persistent => persistent,
		_ => throw new ArgumentException("Invalid data section"),
	};
}
