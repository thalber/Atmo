using System.Text;
using Atmo.Data;

using NamedVars = System.Collections.Generic.Dictionary<Atmo.Data.VarRegistry.SpVar, Atmo.Data.Arg>;
using Save = Atmo.Helpers.VT<int, int>;
using SerDict = System.Collections.Generic.Dictionary<string, object>;

namespace Atmo.Data;

public static partial class VarRegistry
{
	#region fields
	internal static readonly NamedVars SpecialVars = new();
	private static readonly TXT.Regex FMT_Is = new("\\$FMT\\((.*?)\\)");
	private static readonly TXT.Regex Macro_Sub = new("(?<=\\w+\\().+?(?=\\))");
	#endregion;
	private static Arg? GetMacro(string text, in int saveslot, in int character)
	{
		TXT.Match _is;
		if (!(_is = Macro_Sub.Match(text)).Success) return null;
		string name = text.Substring(0, _is.Index - 1);
		plog.DbgVerbose($"Attempting to create macro from {text} (name {name}, match {_is.Value})");
		IEnumerable<API.Create_RawMacroHandler?>? invl = API.AM_invl;
		IArgPayload? res = null;
		if (invl is null)
		{
			plog.DbgVerbose("No macro handles attached");
			return null;
		}
		foreach (API.Create_RawMacroHandler? inv in invl)
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
				plog.LogError($"VarRegistry: Error invoking macro handler {inv}//{inv?.Method} for {name}, {_is.Value}:" +
					$"\n{ex}");
			}
		}
		plog.DbgVerbose($"No macro {name}, variable lookup continues as normal");
		return null;
		//if (!(_is = FMT_Is.Match(text)).Success) return null;
		//text = _is.Groups[1].Value;
		
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
			}); ;
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
