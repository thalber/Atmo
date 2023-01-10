using DC = DevConsole;
using CMD = DevConsole.Commands;
using DCLI = DevConsole.GameConsole;
using Atmo.API;

namespace Atmo;

internal static class ConsoleFace
{
	#region fields
	private static ulong mfinv_uid;
	private const string Help_AtmoVar = """
		Invalid args!
		atmo_var get [varname] - fetches value of specified variable
		atmo_var set [varname] [value] - sets specified variable to value
		""";
	private const string Help_AtmoMetaf = """
		Invalid args!
		atmo_metafunc list - lists all available metafunctions
		atmo_metafunc call [name] [input] (print|save) (DECIMAL|INTEGER|VECTOR|BOOL|STRING?)=STRING - calls a specified metafunction with given input, and either prints result to console or stores it in a variable, using specified datatype. NOTE: Only the immediate result is stored, if result is dynamic, it will be discarded.
		""";
	#endregion;
	public static void Apply()
	{
		new CMD.CommandBuilder("atmo_var")
			.AutoComplete(AtmoVar_ac)
			.Run(AtmoVar_run)
			.Help("""
			atmo_var get [varname]
			atmo_var set [varname] [value]
			""")
			.Register();
		new CMD.CommandBuilder("atmo_metafunc")
			.AutoComplete(MetafunInv_ac)
			.Run(MetafunInv_run)
			.Help("""
			atmo_metafunc list
			atmo_metafunc call [name] [input] (print|save) (DECIMAL|INTEGER|VECTOR|BOOL|STRING?)=STRING
			""")
			.Register();
		new CMD.CommandBuilder("atmo_perf")
			.RunGame((game, args) =>
			{
				DCLI.WriteLine("""
					All times in milliseconds
					""");
				foreach (Body.Happen.Perf rec in inst.CurrentSet!.GetPerfRecords())
				{
					DCLI.WriteLine($"{rec.name}\t: {rec.avg_realup}\t: {rec.samples_realup}\t: {rec.avg_eval}\t: {rec.samples_eval}");
				}
			})
			//.Help("atmo_perf - Fetches performance records from currently active happens")
			.Register();
	}
	private static IEnumerable<string> AtmoVar_ac(string[] args)
	{
		switch (args.Length)
		{
		case 0:
		{
			yield return "get";
			yield return "set";
			yield break;
		}
		}
	}
	private static void AtmoVar_run(string[] args)
	{
		void showhelp()
		{
			DCLI.WriteLine(Help_AtmoVar);
		}
		if (args.Length < 2)
		{
			NotifyArgsMissing(AtmoVar_run, "action", "name");
			showhelp();
			return;
		}
		Arg target = VarRegistry.GetVar(args[1], __CurrentSaveslot ?? -1, __CurrentCharacter ?? -1);
		if (args.AtOr(0, "get") is "get")
		{
			DCLI.WriteLine(target.ToString());
		}
		else
		{
			if (args.Length < 3)
			{
				NotifyArgsMissing(AtmoVar_run, "value");
				showhelp();
				return;
			}
			target.Str = args[1];
		}

	}

	private static IEnumerable<string> MetafunInv_ac(string[] args)
	{
		switch (args.Length)
		{
		case 0:
		{
			yield return "list";
			yield return "call";
			yield break;
		}
		case 1:
		{
			if (args[0] is "call")
			{
				foreach (string name in __namedMetafuncs.Keys) yield return name;
			}
			yield break;
		}
		case 3:
		{
			if (args[0] is "call")
			{
				yield return "print";
				yield return "save";
			}
			yield break;
		}
		case 5:
		{
			if (args[0] is "call")
			{
				foreach (var name in Enum.GetNames(typeof(ArgType))) yield return name;
			}
			yield break;
		}
		}
	}
	private static void MetafunInv_run(string[] args)
	{
		void showhelp()
		{
			DCLI.WriteLine(Help_AtmoMetaf);
		}
		switch (args.AtOr(0, "list"))
		{
		case "list":
		{
			DCLI.WriteLine($"Registered metafunctions: [ {__namedMetafuncs.Keys.Stitch()} ]");
			break;
		}
		case "call":
		{
			int ss = __CurrentSaveslot ?? -1,
				ch = __CurrentCharacter ?? -1;
			if (args.Length < 3)
			{
				NotifyArgsMissing(MetafunInv_run, "name", "input");
				showhelp();
				break;
			}
			Arg? res = VarRegistry.GetMetaFunction($"{args[1]} {args[2]}", ss, ch);
			if (res is null)
			{
				DCLI.WriteLine("Metafunction not found!");
				break;
			}

			var dest = args.AtOr(4, $"v_DCLI_DUMP_{mfinv_uid++}");
			Arg target = VarRegistry.GetVar(dest, ss, ch);
			TryParseEnum(args.AtOr(5, nameof(ArgType.STRING)), out ArgType at);
			if (args.AtOr(3, "print") is "save")
			{
				plog.DbgVerbose($"Saving {res} to {dest}");
				Assign(res, target, at);
			}
			else
			{
				plog.DbgVerbose($"Printing {res}[{at}] to console");
				DCLI.WriteLine(res[at]?.ToString() ?? "NULL");
			}
			break;
		}
		}
	}

	private static void NotifyArgsMissing(Delegate where, params string[] args)
	{
		DCLI.WriteLine($"ATMO.CLI: {where.Method}: Missing arguments! " +
			$"Required arguments: {args.Stitch()}");
	}

}
