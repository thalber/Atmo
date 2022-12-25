using Atmo.Data.Payloads;
//using static Atmo.API.V0;

using NamedVars = System.Collections.Generic.Dictionary<Atmo.Data.VarRegistry.SpVar, Atmo.Data.Arg>;

namespace Atmo.Data;

public static partial class VarRegistry
{
	#region fields
	internal static readonly NamedVars __SpecialVars = new();
	internal static readonly TXT.Regex __Metaf_Sub = new("^\\w+(\\s.+$|$)");
	internal static readonly TXT.Regex __Metaf_Name = new("^\\w+(?=\\s|$)");
	#endregion;
	/// <summary>
	/// Attempts constructing a metafunction under specified name for specified saveslot and character, wrapped in an Arg
	/// </summary>
	/// <param name="text">Raw text (including the name)</param>
	/// <param name="saveslot">Save slot to look at</param>
	/// <param name="character">Character to look at</param>
	/// <returns></returns>
	public static Arg? GetMetaFunction(string text, in int saveslot, in int character)
	{
		TXT.Match _is;
		if (!(_is = __Metaf_Sub.Match(text)).Success) return null;
		string name = __Metaf_Name.Match(text).Value;//text.Substring(0, Mathf.Max(_is.Index - 1, 0));
		plog.DbgVerbose($"Attempting to create metafun from {text} (name {name}, match {_is.Value})");
		IEnumerable<V0_Create_RawMetaFunction?>? invl = __AM_invl;
		IArgPayload? res = null;
		if (invl is null)
		{
			plog.DbgVerbose("No metafun handlers attached");
			return null;
		}
		foreach (V0_Create_RawMetaFunction? inv in invl)
		{
			try
			{
				if ((res = inv?.Invoke(name, _is.Value, saveslot, character)) is not null)
				{
					return new(res);
				}
			}
			catch (Exception ex)
			{
				plog.LogError($"VarRegistry: Error invoking metafun handler {inv}//{inv?.Method} for {name}, {_is.Value}:" +
					$"\n{ex}");
			}
		}
		plog.DbgVerbose($"No metafun {name}, variable lookup continues as normal");
		return null;
		//if (!(_is = FMT_Is.Match(text)).Success) return null;
		//text = _is.Groups[1].Value;
		
	}
	/// <summary>
	/// Attempts fetching a special variable by name.
	/// </summary>
	/// <param name="name">Supposed var name</param>
	/// <returns>null if not a special</returns>
	public static Arg? GetSpecial(string name)
	{
		SpVar tp = __SpecialForName(name);
		if (tp is SpVar.NONE) return null;
		return __SpecialVars[tp];
	}
	internal static void __FillSpecials()
	{
		__SpecialVars.Clear();
		foreach (SpVar tp in Enum.GetValues(typeof(SpVar)))
		{
			static RainWorldGame? FindRWG()
				=> inst.RW?.processManager?.FindSubProcess<RainWorldGame>();
			static int findKarma()
				=> FindRWG()?.GetStorySession?.saveState.deathPersistentSaveData.karma ?? -1;
			static int findKarmaCap()
				=> FindRWG()?.GetStorySession?.saveState.deathPersistentSaveData.karmaCap ?? -1;
			static int findClock() => FindRWG()?.world.rainCycle?.cycleLength ?? __temp_World?.rainCycle.cycleLength ?? -1;
			__SpecialVars.Add(tp, tp switch
			{
				SpVar.NONE => 0,
				SpVar.version => Ver,
				SpVar.time => new(new ByCallbackGetOnly()
				{
					getStr = () => DateTime.Now.ToString()
				}),
				SpVar.utctime => new(new ByCallbackGetOnly()
				{
					getStr = () => DateTime.UtcNow.ToString()
				}),
				SpVar.cycletime => new(new ByCallbackGetOnly()
				{
					getI32 = findClock,
					getF32 = () => findClock() / 40,
					getStr = () => $"{findClock() / 40} seconds / {findClock()} frames"
				}),
				SpVar.root => RootFolderDirectory(),
				SpVar.realm => FindAssembliesByName("Realm").Count() > 0, //check if right
				SpVar.os => Environment.OSVersion.Platform.ToString(),
				SpVar.memused => new(new ByCallbackGetOnly()
				{
					getStr = () => GC.GetTotalMemory(false).ToString()
				}),
				SpVar.memtotal => new(new ByCallbackGetOnly()
				{
					getStr = () => "???"
				}),
				SpVar.username => Environment.UserName,
				SpVar.machinename => Environment.MachineName,
				SpVar.karma => new(new ByCallbackGetOnly()
				{
					getI32 = findKarma,
					getF32 = static () => findKarma(),
				}),
				SpVar.karmacap => new(new ByCallbackGetOnly()
				{
					getI32 = findKarmaCap,
					getF32 = static () => findKarmaCap(),
				}),
				_ => 0,
			}); ;
		}
	}
	internal static SpVar __SpecialForName(string name)
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
