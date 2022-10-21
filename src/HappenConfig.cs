using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Atmo;
public struct HappenConfig
{
	public string name;
	public List<string> groups;
	public List<string> include;
	public List<string> exclude;
	public Dictionary<string, string[]> actions;
	public List<string> when;
	public PredicateInlay? conditions;
	public HappenConfig(string name)
	{
		this.name = name;
		groups = new();
		actions = new();
		when = new();
		include = new();
		exclude = new();
		conditions = null;
	}
}
