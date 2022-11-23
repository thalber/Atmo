using DC = DevConsole;
using DCMD = DevConsole.Commands;
using DCLI = DevConsole.GameConsole;

namespace Atmo;

internal static class ConsoleFace
{
	#region fields
	private static ulong mfinv_uid;
	#endregion;
	public static void Apply()
	{
		new DCMD.CommandBuilder("atmo_var")
			.AutoComplete(AtmoVar_ac)
			.Run(AtmoVar_run)
			.Help("""
			atmo_var get varname
				fetches value of specified variable
			atmo_var set varname value
				Sets specified variable to value
			""")
			.Register();
		new DCMD.CommandBuilder("atmo_metafunc")
			.AutoComplete(MetafunInv_ac)
			.Run(MetafunInv_run)
			.Help("""
			atmo_metafunc list
				Lists available Atmo metafunctions
			atmo_metafunc call name input print|save DECIMAL|INTEGER|VECTOR|BOOL|STRING
				Invokes one with given user input and
					a) prints result to console
					b) stores result in a variable with a set name
			""")
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
		if (args.Length < 2)
		{
			NotifyArgsMissing(AtmoVar_run, "action", "name");
			return;
		}
		Arg target = VarRegistry.GetVar(args[1], CurrentSaveslot ?? -1, CurrentCharacter ?? -1);
		if (args.AtOr(0, "get") is "get")
		{
			DCLI.WriteLine(target.ToString());
		}
		else
		{
			if (args.Length < 3)
			{
				NotifyArgsMissing(AtmoVar_run, "value");
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
				foreach (string name in API.namedMetafuncs.Keys) yield return name;
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
		switch (args.AtOr(0, "list"))
		{
		case "list":
		{
			DCLI.WriteLine($"Registered metafunctions: [ {API.namedMetafuncs.Keys.Stitch()} ]");
			break;
		}
		case "call":
		{
			int ss = CurrentSaveslot ?? -1,
				ch = CurrentCharacter ?? -1;
			if (args.Length < 3)
			{
				NotifyArgsMissing(MetafunInv_run, "name", "input");
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
