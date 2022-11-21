using System.Text;
using Atmo.Data;

using NamedVars = System.Collections.Generic.Dictionary<Atmo.Helpers.VarRegistry.SpVar, Atmo.Data.Arg>;
using Save = Atmo.Helpers.VT<int, int>;
using SerDict = System.Collections.Generic.Dictionary<string, object>;

namespace Atmo.Helpers;

public static partial class VarRegistry
{
	#region fields
	internal static readonly NamedVars SpecialVars = new();
	private static readonly TXT.Regex FMT_Split = new("{.+?}");
	private static readonly TXT.Regex FMT_Match = new("(?<={).+?(?=})");
	private static readonly TXT.Regex FMT_Is = new("\\$FMT\\((.*?)\\)");
	
	#endregion;
	private static Arg? GetFmt(string text, in int saveslot, in int character)
	{
		TXT.Match _is;
		if (!(_is = FMT_Is.Match(text)).Success) return null;
		text = _is.Groups[1].Value;
		string[] bits = FMT_Split.Split(text);
		TXT.MatchCollection names = FMT_Match.Matches(text);
		Arg[] variables = new Arg[names.Count];
		for (int i = 0; i < names.Count; i++)
		{
			variables[i] = GetVar(names[i].Value, saveslot, character);
		}

		int ind = 0;
		string format = bits.Stitch((x, y) => $"{x}{{{ind++}}}{y}");
		object[] getStrs ()
		{
			return variables.Select(x => x.Str).ToArray();
		}
		return new(new GetOnlyCallbackPayload()
		{
			getStr = () => string.Format(format, getStrs())
		});
		//todo: update format string docs
	}
	internal static Arg? GetSpecial(string name)
	{
		SpVar tp = SpecialForName(name);
		if (tp is SpVar.NONE) return null;
		return SpecialVars[tp];
	}
	private static void FillSpecials()
	{
		SpecialVars.Clear();
		foreach (SpVar tp in Enum.GetValues(typeof(SpVar)))
		{
			static RainWorldGame? FindRWG() 
				=> inst.RW?.processManager?.FindSubProcess<RainWorldGame>();
			static int findKarma() 
				=> FindRWG()?.GetStorySession?.saveState.deathPersistentSaveData.karma ?? -1;
			static int findKarmaCap() 
				=> FindRWG()?.GetStorySession?.saveState.deathPersistentSaveData.karmaCap ?? -1;
			static int findClock() => FindRWG()?.world.rainCycle?.cycleLength ?? -1;
			SpecialVars.Add(tp, tp switch
			{
				SpVar.NONE => 0,
				SpVar.version => Ver,
				SpVar.time => new(new GetOnlyCallbackPayload()
				{
					getStr = () => DateTime.Now.ToString()
				}),
				SpVar.utctime => new(new GetOnlyCallbackPayload()
				{
					getStr = () => DateTime.UtcNow.ToString()
				}),
				SpVar.cycletime => new(new GetOnlyCallbackPayload()
				{
					getI32 = findClock,
					getF32 = () => findClock()
				}),
				SpVar.root => RootFolderDirectory(),
				SpVar.realm => FindAssembliesByName("Realm").Count() > 0, //check if right
				SpVar.os => Environment.OSVersion.Platform.ToString(),
				SpVar.memused => new(new GetOnlyCallbackPayload()
				{
					getStr = () => GC.GetTotalMemory(false).ToString()
				}),
				SpVar.memtotal => new(new GetOnlyCallbackPayload()
				{
					getStr = () => "???"
				}),
				SpVar.username => Environment.UserName,
				SpVar.machinename => Environment.MachineName,
				SpVar.karma => new(new GetOnlyCallbackPayload()
				{
					getI32 = findKarma,
					getF32 = static () => findKarma(),
				}),
				SpVar.karmacap => new(new GetOnlyCallbackPayload()
				{
					getI32 = findKarmaCap,
					getF32 = static () => findKarmaCap(),
				}),
				_ => 0,
			});;
		}
	}
	private static SpVar SpecialForName(string name)
	{
		return name.ToLower() switch
		{
			"root" or "rootfolder" => SpVar.root,
			"now" or "time" => SpVar.time,
			"utcnow" or "utctime" => SpVar.utctime,
			"version" or "atmover" or "atmoversion" => SpVar.version,
			"cycletime" or "cycle" => SpVar.cycletime,
			"realm" => SpVar.realm,
			"os" => SpVar.os,
			"memoryused" or "memused" => SpVar.memused,
			"memorytotal" or "memtotal" => SpVar.memtotal,
			"user" or "username" => SpVar.username,
			"machine" or "machinename" => SpVar.machinename,
			"karma" => SpVar.karma,
			"karmacap" or "maxkarma" => SpVar.karmacap,
			_ => SpVar.NONE,
		};
	}

	internal enum SpVar
	{
		NONE,
		time,
		utctime,
		os,
		root,
		realm,
		version,
		memused,
		memtotal,
		username,
		machinename,
		karma,
		karmacap,
		cycletime,
	}
}
