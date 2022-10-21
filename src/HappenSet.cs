using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static Atmo.Atmod;
using static Atmo.Helpers.Utils;
using CRS = CustomRegions.Mod;
using IO = System.IO;
using TXT = System.Text.RegularExpressions;

namespace Atmo;
/// <summary>
/// Represents a set of Happens for a single region. Binds together room names, groups and happens.
/// </summary>
public sealed class HappenSet
{
	#region fields
	internal readonly RainWorldGame rwg;
	internal readonly World world;
	internal TwoPools<string, string> RoomsToGroups = new();
	internal TwoPools<string, Happen> GroupsToHappens = new();
	internal TwoPools<string, Happen> SpecificIncludeToHappens = new();
	internal TwoPools<string, Happen> SpecificExcludeToHappens = new();
	internal readonly List<Happen> AllHappens = new();
	#endregion
	private HappenSet(World world, IO.FileInfo? file = null)
	{
		this.world = world;
		rwg = world.game;
		if (world is null || file is null) return;
		HappenParser.Parse(file, this, rwg);
	}
	/// <summary>
	/// Yields all rooms a given happen should be active in.
	/// </summary>
	/// <param name="ha"></param>
	/// <returns></returns>
	public IEnumerable<string> GetRoomsForHappen(Happen ha)
	{
		List<string> returned = new();
		IEnumerable<string>? excludes = SpecificExcludeToHappens.IndexFromRight(ha);
		IEnumerable<string>? includes = SpecificIncludeToHappens.IndexFromRight(ha);
		foreach (var group in GroupsToHappens.IndexFromRight(ha))
		{
			foreach (var room in RoomsToGroups.IndexFromRight(group))
			{
				if (excludes.Contains(room)) break;
				//if (SpecificExcludeToHappens.IndexFromRight(ha).Contains(room)) continue;
				returned.Add(room);
				yield return room;
			}
		}
		foreach (var room in includes)
		{
			if (!returned.Contains(room)) yield return room;
		}
	}
	/// <summary>
	/// Yields all happens a given room should have active.
	/// </summary>
	/// <param name="roomname"></param>
	/// <returns></returns>
	public IEnumerable<Happen> GetHappensForRoom(string roomname)
	{
		List<Happen> returned = new();
		//goto _specific;
		if (!RoomsToGroups.LeftContains(roomname)) goto _specific;
		foreach (var group in RoomsToGroups.IndexFromLeft(roomname))
		{
			if (!GroupsToHappens.LeftContains(group)) continue;
			foreach (Happen? ha in GroupsToHappens.IndexFromLeft(group))
			{
				//exclude the minused
				if (SpecificExcludeToHappens.IndexFromRight(ha)
					.Contains(roomname)) continue;
				returned.Add(ha);
				yield return ha;
			}
		}
	_specific:
		if (!SpecificIncludeToHappens.LeftContains(roomname)) yield break;
		foreach (Happen? ha in SpecificIncludeToHappens.IndexFromLeft(roomname))
		{
			if (!returned.Contains(ha)) yield return ha;
		}
	}
	#region insertion
	/// <summary>
	/// Binds a given happen to a set of groups. Assumes that happen has already been added via <see cref="InsertHappens(IEnumerable{Happen})"/>.
	/// </summary>
	/// <param name="ha"></param>
	/// <param name="groups"></param>
	public void AddGrouping(Happen ha, IEnumerable<string> groups)
	{
		if (groups?.Count() is null or 0) return;
		Dictionary<string, List<string>> ins = new();
		foreach (var g in groups) { ins.Add(g, new(0)); }
		InsertGroups(ins);
		GroupsToHappens.AddLinksBulk(groups.Select(gr => new KeyValuePair<string, Happen>(gr, ha)));
	}
	/// <summary>
	/// Adds room excludes for a given happen. Assumes happen has already been added via <see cref="InsertHappens(IEnumerable{Happen})"/>
	/// </summary>
	/// <param name="ha"></param>
	/// <param name="excl"></param>
	public void AddExcludes(Happen ha, IEnumerable<string> excl)
	{
		if (excl?.Count() is null or 0) return;
		SpecificExcludeToHappens.InsertRangeLeft(excl);
		foreach (var ex in excl) SpecificExcludeToHappens.AddLink(ex, ha);
	}
	/// <summary>
	/// Adds room includes for a given happen. Assumes happen has already been added via <see cref="InsertHappens(IEnumerable{Happen})"/>
	/// </summary>
	/// <param name="ha"></param>
	/// <param name="incl"></param>
	public void AddIncludes(Happen ha, IEnumerable<string> incl)
	{
		if (incl?.Count() is null or 0) return;
		SpecificIncludeToHappens.InsertRangeLeft(incl);
		foreach (var @in in incl) SpecificIncludeToHappens.AddLink(@in, ha);
	}
	/// <summary>
	/// Adds a group with its contents.
	/// </summary>
	/// <param name="groups"></param>
	public void InsertGroups(IDictionary<string, List<string>> groups)
	{
		RoomsToGroups.InsertRangeRight(groups.Keys);
		GroupsToHappens.InsertRangeLeft(groups.Keys);
		foreach (KeyValuePair<string, List<string>> kvp in groups)
		{
			RoomsToGroups.InsertRangeLeft(kvp.Value);
			foreach (var room in kvp.Value) { RoomsToGroups.AddLink(room, kvp.Key); }
		}
	}
	/// <summary>
	/// Inserts a set of happens without binding them to anything.
	/// </summary>
	/// <param name="haps"></param>
	public void InsertHappens(IEnumerable<Happen> haps)
	{
		AllHappens.AddRange(haps);
		GroupsToHappens.InsertRangeRight(haps);
		SpecificExcludeToHappens.InsertRangeRight(haps);
		SpecificIncludeToHappens.InsertRangeRight(haps);
	}
	#endregion
	/// <summary>
	/// Yields performance records for all happens. Consume or discard the enumerable on the same frame.
	/// </summary>
	/// <returns></returns>
	public IEnumerable<Happen.Perf> GetPerfRecords()
	{
		foreach (Happen? ha in AllHappens)
		{
			yield return ha.PerfRecord();
		}
	}
	#region statics
	internal static HappenSet? TryCreate(World world)
	{
		HappenSet? res = null;
#if REMIX
        throw new NotImplementedException("REMIX behaviour is not implemented yet!");
#else
		try
		{
			Dictionary<string, CRS.CustomWorldStructs.RegionPack>? packs = CRS.API.InstalledPacks;
			Dictionary<string, string>? active = CRS.API.ActivatedPacks;
			foreach (KeyValuePair<string, CRS.CustomWorldStructs.RegionPack> kvp in packs)
			{
				string? name = kvp.Key;
				CRS.CustomWorldStructs.RegionPack data = kvp.Value;
				//skip inactive
				if (!active.ContainsKey(name)) continue;
				var tarpath = CRS.API.BuildPath(
					regionPackFolder: data.folderName,
					folderName: "World",
					regionID: world.name,
					file: $"{world.name}.atmo",
					folder: IO.Path.Combine("Regions", world.name),
					includeRoot: true);
				var tarfile = new IO.FileInfo(tarpath);
				inst.Plog.LogDebug($"Checking regpack {name} (path {tarpath})");
				if (tarfile.Exists)
				{
					inst.Plog.LogDebug("Found a .atmo file, reading a happenset...");
					HappenSet gathered = new(world, tarfile);
					if (res is null) res = gathered;
					else res += gathered;
				}
				else
				{
					inst.Plog.LogDebug("No XX.atmo file found.");
				}
			}
			inst.Plog.LogDebug(Stitch(res?.GetHappensForRoom("SU_C04").Select(x => x.name)));
			return res;
		}
		catch (Exception ex)
		{
			inst.Plog.LogError($"Could not load event setup for {world.name}:\n{ex}");
			return null;
		}
#endif
	}
	/// <summary>
	/// Concatenates two instances together. Used when merging from several files.
	/// </summary>
	/// <param name="l"></param>
	/// <param name="r"></param>
	/// <returns></returns>
	public static HappenSet operator +(HappenSet l, HappenSet r)
	{
		HappenSet res = new(l.world ?? r.world)
		{
			SpecificIncludeToHappens = TwoPools<string, Happen>.Stitch(
				l.SpecificIncludeToHappens,
				r.SpecificIncludeToHappens),
			RoomsToGroups = TwoPools<string, string>.Stitch(
				l.RoomsToGroups,
				r.RoomsToGroups),
			GroupsToHappens = TwoPools<string, Happen>.Stitch(
				l.GroupsToHappens,
				r.GroupsToHappens),
			SpecificExcludeToHappens = TwoPools<string, Happen>.Stitch(
				l.SpecificExcludeToHappens,
				r.SpecificExcludeToHappens),
		};
		res.AllHappens.AddRange(l.AllHappens);
		res.AllHappens.AddRange(r.AllHappens);
		foreach (Happen? ha in res.AllHappens)
		{
			inst.Plog.LogDebug($"{ha.name}: switching ownership");
			ha.set = res;
		}
		return res;
		//throw new NotImplementedException();
	}
	#endregion statics
}
