using SerDict = System.Collections.Generic.Dictionary<string, object>;
using Save = Atmo.Helpers.Utils.VT<int, int>;
using NamedVars = System.Collections.Generic.Dictionary<Atmo.Helpers.VarRegistry.SpVar, Atmo.Helpers.Arg>;

namespace Atmo.Helpers;

public static partial class VarRegistry
{
	#region fields
	internal static readonly NamedVars SpecialVars = new();
	#endregion;
	private static Arg? GetFmt(string text, int saveslot, int character)
	{
		TXT.Match match;
		if (!(match = TXT.Regex.Match(text, "\\$FMT\\((.*)\\)")).Success) return null;
		text = match.Groups[1].Value;
		plog.DbgVerbose($"Creating format string from \"{text}\"...");
		List<PredicateInlay.Token> tokens = PredicateInlay.Tokenize(text);
		string format = tokens.AtOr(0, new(PredicateInlay.TokenType.Literal, string.Empty)).val;
		IEnumerable<Arg>? values = tokens.Select(x => GetVar(x.val, saveslot, character));
		return new Arg(new ReadOnlyEventful()
		{
			getStr = () => string.Format(format, values
			.Select(x => x.Str)
			.ToArray()),
		});
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
			SpecialVars.Add(tp, tp switch
			{
				SpVar.NONE => 0,
				SpVar.version => Ver,
				SpVar.time => new Arg(new ReadOnlyEventful()
				{
					getStr = () => DateTime.Now.ToString()
				}),
				SpVar.utctime => new Arg(new ReadOnlyEventful()
				{
					getStr = () => DateTime.UtcNow.ToString()
				}),
				SpVar.cycletime => 0,
				SpVar.root => RootFolderDirectory(),
				SpVar.realm => FindAssembliesByName("Realm").Count() > 0, //check if right
				SpVar.os => Environment.OSVersion.Platform.ToString(),
				SpVar.memused => new Arg(new ReadOnlyEventful()
				{
					getStr = () => GC.GetTotalMemory(false).ToString()
				}),
				SpVar.memtotal => new Arg(new ReadOnlyEventful()
				{
					getStr = () => DBG.Process.GetCurrentProcess().PrivateMemorySize64.ToString()
				}),
				_ => 0,
			});
		}
	}
	private static SpVar SpecialForName(string name)
		=> name.ToLower() switch
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
			_ => SpVar.NONE,
		};
	internal enum SpVar
	{
		NONE,
		version,
		time,
		utctime,
		cycletime,
		root,
		realm,
		os,
		memused,
		memtotal
	}
}
