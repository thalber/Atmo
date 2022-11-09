using SerDict = System.Collections.Generic.Dictionary<string, object>;
using Save = Atmo.Helpers.Utils.VT<int, int>;
using NamedVars = System.Collections.Generic.Dictionary<Atmo.Helpers.VarRegistry.BIVar, Atmo.Helpers.Arg>;


namespace Atmo.Helpers;

public static partial class VarRegistry
{
	#region fields
	internal static readonly NamedVars BuiltinVars = new();
	#endregion;
	internal static Arg? GetBuiltin (string name)
	{
		BIVar tp = BuiltinForName(name);
		if (tp is BIVar.NONE) return null;
		return BuiltinVars[tp];
	}
	private static void FillBuiltins()
	{
		BuiltinVars.Clear();
		foreach (BIVar tp in Enum.GetValues(typeof(BIVar)))
		{
			BuiltinVars.Add(tp, tp switch
			{
				BIVar.NONE => 0,
				BIVar.version => Ver,
				BIVar.time => DateTime.Now.ToString(),
				BIVar.utctime => DateTime.UtcNow.ToString(),
				BIVar.cycletime => 0,
				BIVar.root => RootFolderDirectory(),
				BIVar.realm => FindAssembliesByName("Realm").Count() > 0, //check if right
				BIVar.os => Environment.OSVersion.Platform.ToString(),
				_ => 0,
			});
		}
	}
	private static BIVar BuiltinForName(string name)
		=> name.ToLower() switch
		{
			"root" or "rootfolder" => BIVar.root,
			"now" or "time" => BIVar.time,
			"utcnow" or "utctime" => BIVar.utctime,
			"version" or "atmover" or "atmoversion" => BIVar.version,
			"cycletime" or "cycle" => BIVar.cycletime,
			"realm" => BIVar.realm,
			"os" => BIVar.os,
			_ => BIVar.NONE,
		};
	internal enum BIVar
	{
		NONE,
		version,
		time,
		utctime,
		cycletime,
		root,
		realm,
		os,
	}
}
