using SerDict = System.Collections.Generic.Dictionary<string, object>;
using NamedVars = System.Collections.Generic.Dictionary<string, Atmo.Helpers.Arg>;
using Save = Atmo.Helpers.Utils.VT<int, int>;

namespace Atmo.Helpers;
/// <summary>
/// Allows accessing a pool of variables, global or save-specific.
/// You can use <see cref="GetVar(string, int, int)"/> to fetch variables by name.
/// Variables are returned as <see cref="Arg"/>s (NOTE: they may be mutable!
/// Be careful not to mess the values up, especially if you are requesting
/// a variable made by someone else). Use 'p_' prefix to look for a death-persistent variable,
/// and 'g_' prefix to look for a global variable.
/// <seealso cref="GetVar(string, int, int)"/> for more details on fetching, prefixes 
/// and some examples.
/// <para>
/// <see cref="VarRegistry"/>'s primary purpose is being used by arguments in .atmo files
/// written like '$varname'. Doing that will automatically call <see cref="GetVar"/> 
/// for given name, current slot and current character. 
/// </para>
/// </summary>
public static partial class VarRegistry
{
	#region fields / consts / props
	/// <summary>
	/// <see cref="DeathPersistentSaveData"/> hash to slugcat number.
	/// </summary>
	private static readonly Dictionary<int, int> DPSD_Slug = new();
	internal const string PREFIX_GLOBAL = "g_";
	internal const string PREFIX_PERSISTENT = "p_";
	internal static Arg Defarg => string.Empty;
	/// <summary>
	/// Var sets per save. Key is saveslot number + character index.
	/// </summary>
	internal static readonly Dictionary<Save, VarSet> VarsPerSave = new();
	/// <summary>
	/// Global vars per saveslot. key is saveslot number
	/// </summary>
	internal static readonly Dictionary<int, NamedVars> VarsGlobal = new();\
	#endregion
	#region lifecycle
	internal static void Clear()
	{
		plog.LogDebug("Clear VarRegistry hooks");
		try
		{
			On.RainCycle.ctor -= TrackCycleLength;

			On.SaveState.LoadGame -= ReadNormal;
			On.SaveState.SaveToString -= WriteNormal;

			On.DeathPersistentSaveData.ctor -= RegDPSD;
			On.DeathPersistentSaveData.FromString -= ReadPers;
			On.DeathPersistentSaveData.SaveToString -= WritePers;

			On.PlayerProgression.WipeAll -= WipeAll;
			On.PlayerProgression.WipeSaveState -= WipeSavestate;
			foreach (int slot in VarsGlobal.Keys)
			{
				WriteGlobal(slot);
			}
		}
		catch (Exception ex)
		{
			plog.LogFatal(ErrorMessage(Site.Clear, "Unhandled exception", ex));
		}
	}

	internal static void Init()
	{
		//todo: globals ser test
		plog.LogDebug("Init VarRegistry hooks");
		try
		{
			FillBuiltins();

			On.RainCycle.ctor += TrackCycleLength;

			On.SaveState.LoadGame += ReadNormal;
			On.SaveState.SaveToString += WriteNormal;

			On.DeathPersistentSaveData.ctor += RegDPSD;
			On.DeathPersistentSaveData.FromString += ReadPers;
			On.DeathPersistentSaveData.SaveToString += WritePers;

			On.PlayerProgression.WipeAll += WipeAll;
			On.PlayerProgression.WipeSaveState += WipeSavestate;
		}
		catch (Exception ex)
		{
			plog.LogFatal(ErrorMessage(Site.Init, "Unhandled exception", ex));
		}

	}

	private static void TrackCycleLength(
		On.RainCycle.orig_ctor orig,
		RainCycle self,
		World world,
		float minutes)
	{
		orig(self, world, minutes);
		BuiltinVars[BIVar.cycletime].F32 = minutes * 60f;
		plog.LogDebug($"Setting $cycletime to {BuiltinVars[BIVar.cycletime]}");
	}

	private static void WipeSavestate(
		On.PlayerProgression.orig_WipeSaveState orig,
		PlayerProgression self,
		int saveStateNumber)
	{
		Save save = default;
		try
		{
			save = MakeSD(CurrentSaveslot ?? -1, saveStateNumber);
			plog.LogDebug($"Wiping data for save {save}");
			EraseData(save);
		}
		catch (Exception ex)
		{
			plog.LogError(ErrorMessage(Site.HookWipe, $"Failed to wipe saveslot {save}", ex));
		}
		orig(self, saveStateNumber);
	}
	private static void WipeAll(
		On.PlayerProgression.orig_WipeAll orig,
		PlayerProgression self)
	{
		int ss = CurrentSaveslot ?? -1;
		try
		{
			plog.LogDebug($"Wiping data for slot {ss}");
			foreach ((Save save, VarSet set) in VarsPerSave)
			{
				if (save.a != ss) continue;
				EraseData(save);
				foreach (DataSection sec in Enum.GetValues(typeof(DataSection)))
				{
					set.FillFrom(null, sec);
				}
			}
		}
		catch (Exception ex)
		{
			plog.LogError(ErrorMessage(Site.HookWipe, $"Failed to wipe all saves for slot {ss}", ex));
		}
		orig(self);
	}

	private static void RegDPSD(
		On.DeathPersistentSaveData.orig_ctor orig,
		DeathPersistentSaveData self,
		int slugcat)
	{
		orig(self, slugcat);
		DPSD_Slug.Add(self.GetHashCode(), slugcat);
	}
	private static void ReadPers(
		On.DeathPersistentSaveData.orig_FromString orig,
		DeathPersistentSaveData self,
		string s)
	{
		try
		{
			int? ss = CurrentSaveslot;
			//int ch = CurrentCharacter ?? -1;
			if (ss is null)
			{
				plog.LogError(ErrorMessage(Site.HookPersistent, "Could not find current saveslot", null));
				return;
			}
			Save save = MakeSD(ss.Value, DPSD_Slug[self.GetHashCode()]);
			plog.LogDebug($"Attempting to load persistent vars for save {save}");
			SerDict? data = TryReadData(save, DataSection.Persistent);
			if (data is null) plog.LogDebug("Could not load file, varset will be empty");
			VarsPerSave
				.AddIfNone_Get(save, () => new(save))
				.FillFrom(data ?? new(), DataSection.Persistent);
			Arg aaa = VarsPerSave.AddIfNone_Get(save, () => new(save)).GetVar("AAA");//.F32 = 10f;
			if (aaa.Str is "") aaa.I32 = 10;
			plog.LogDebug(aaa);
		}
		catch (Exception ex)
		{
			plog.LogError(ErrorMessage(Site.HookPersistent, "Error on read", ex));
		}
		orig(self, s);
	}
	private static string WritePers(
		On.DeathPersistentSaveData.orig_SaveToString orig,
		DeathPersistentSaveData self,
		bool saveAsIfPlayerDied,
		bool saveAsIfPlayerQuit)
	{
		try
		{
			int? ss = CurrentSaveslot;
			if (ss is null)
			{
				plog.LogError(ErrorMessage(Site.HookPersistent, "Could not find current saveslot", null));
				goto done;
			}
			Save save = MakeSD(ss.Value, DPSD_Slug[self.GetHashCode()]);
			plog.LogDebug($"Attempting to write persistent vars for {save}");
			SerDict? data = VarsPerSave
				.AddIfNone_Get(save, () => new(save))
				.GetSer(DataSection.Normal);
			TryWriteData(save, DataSection.Persistent, data);
		}
		catch (Exception ex)
		{
			plog.LogError(ErrorMessage(Site.HookPersistent, "Error on write", ex));
		}
	done:
		return orig(self, saveAsIfPlayerDied, saveAsIfPlayerQuit);
	}

	private static void ReadNormal(
		On.SaveState.orig_LoadGame orig,
		SaveState self,
		string str,
		RainWorldGame game)
	{
		orig(self, str, game);
		try
		{
			int? ss = CurrentSaveslot;
			if (ss is null)
			{
				plog.LogError(ErrorMessage(Site.HookNormal, "Could not find current saveslot", null));
				return;
			}
			Save save = MakeSD(ss.Value, self.saveStateNumber);
			plog.LogDebug($"Attempting to load non-persistent vars for save {save}");
			SerDict? data = TryReadData(save, DataSection.Normal);
			if (data is null) plog.LogDebug("Could not load file, varset will be empty");
			VarsPerSave
				.AddIfNone_Get(save, () => new(save))
				.FillFrom(data ?? new(), DataSection.Normal);
			Arg aaa = VarsPerSave.AddIfNone_Get(save, () => new(save)).GetVar("AAA");//.F32 = 10f;
			if (aaa.Str is "") aaa.I32 = 10;
			plog.LogDebug(aaa);
		}
		catch (Exception ex)
		{
			plog.LogError(ErrorMessage(Site.HookNormal, "Error on read", ex));
		}
	}
	private static string WriteNormal(
		On.SaveState.orig_SaveToString orig,
		SaveState self)
	{
		try
		{
			int? ss = CurrentSaveslot;
			if (ss is null)
			{
				plog.LogError(ErrorMessage(Site.HookNormal, "Could not find current saveslot", null));
				goto done;
			}
			Save save = MakeSD(ss.Value, self.saveStateNumber);
			SerDict? data = VarsPerSave
				.AddIfNone_Get(save, () => new(save))
				.GetSer(DataSection.Normal);
			TryWriteData(save, DataSection.Normal, data);
		}
		catch (Exception ex)
		{
			plog.LogError(ErrorMessage(Site.HookNormal, "Error on write", ex));
		}
	done:
		return orig(self);
	}
	#endregion
	#region methods
	/// <summary>
	/// Fetches a stored variable. Creates a new one if does not exist. You can use prefixes to request death-persistent and global variables. Some examples:
	/// <para>
	/// <code>
	///	<see cref="VarRegistry"/>.GetVar("flag0", 0, 2)
	/// </code>
	/// will try fetching variable named "flag0" for saveslot 0 (first) for hunter (character 2). 
	/// Normal variables for a save are reset if you wipe character progress or the entire saveslot;
	/// <code>
	/// <see cref="VarRegistry"/>.GetVar("p_flag1", 1, 2)
	/// </code>
	/// will try fetching a *death-persistent* (saved with the same sort of persistence as
	/// flags indicating whether you visited echoes, finished the game, etc) variable 
	/// named "flag1" for saveslot 1 (second) for hunter (character 2).
	/// Persistent variables for a save are reset if you wipe character progress
	/// or the entire saveslot;
	/// <code>
	/// <see cref="VarRegistry"/>.GetVar("g_flag2", 1)
	/// </code>
	/// will try searching for a *global* variable called "flag2" for saveslot 1 (second).
	/// Global variables are exempt from data resets [for now, are you sure they should be?].
	/// Global variables are shared between all characters on a given slot,
	/// and will also be usable in arena should Atmo ever support arena.
	/// </para>
	/// </summary>
	/// <param name="name">Name of the variable, with prefix if needed. Must not be null.</param>
	/// <param name="saveslot">Save slot to look up data from (<see cref="RainWorld"/>.options.saveSlot for current)</param>
	/// <param name="character">Current character. 0 for survivor, 1 for monk, 2 for hunter.</param>
	/// <returns>Variable requested; if there was no variable with given name before, GetVar creates a blank one from an empty string.</returns>
	public static Arg GetVar(string name!!, int saveslot, int character = -1)
	{
		Arg? res;
		if ((res = GetBuiltin(name)) is not null)
		{
			return res;
		}
		if (name.StartsWith(PREFIX_GLOBAL))
		{
			name = name.Substring(PREFIX_GLOBAL.Length);
			return VarsGlobal
				.AddIfNone_Get(saveslot, () => ReadGlobal(saveslot))
				.AddIfNone_Get(name, () => Defarg);
		}
		Save save = MakeSD(saveslot, character);
		return VarsPerSave
			.AddIfNone_Get(save, () => new(save))
			.GetVar(name);
	}
	#region filemanip
	internal static NamedVars ReadGlobal(int slot)
	{
		NamedVars res = new();
		IO.FileInfo fi = new(GlobalFile(slot));
		if (!fi.Exists) return res;
		try
		{
			using IO.StreamReader reader = fi.OpenText();
			SerDict json = reader.ReadToEnd().dictionaryFromJson();
			foreach ((string name, object val) in json)
			{
				res.Add(name, val?.ToString() ?? string.Empty);
			}
		}
		catch (IO.IOException ex)
		{
			plog.LogError(ErrorMessage(Site.ReadData, $"Could not read global vars for slot {slot}", ex));
		}
		return res;
	}
	internal static void WriteGlobal(int slot)
	{
		IO.DirectoryInfo dir = new(SaveFolder(new(slot, -1)));
		IO.FileInfo fi = new(GlobalFile(slot));
		try
		{
			if (!dir.Exists) dir.Create();
			fi.Refresh();
			NamedVars dict = VarsGlobal.AddIfNone_Get(slot, () => new());

			using IO.StreamWriter writer = fi.CreateText();
			writer.Write(Json.Serialize(dict));
		}
		catch (IO.IOException ex)
		{
			plog.LogError(ErrorMessage(Site.WriteData, $"Could not write global vars for slot {slot}", ex));
		}
	}
	internal static SerDict? TryReadData(Save save, DataSection section)
	{
		IO.FileInfo fi = new(SaveFile(save, section));
		if (!fi.Exists) return null;
		try
		{
			using IO.StreamReader reader = fi.OpenText();
			return reader.ReadToEnd().dictionaryFromJson();
		}
		catch (Exception ex)
		{
			plog.LogError(ErrorMessage(Site.ReadData, $"error reading {section} for slot {save} ({fi.FullName})", ex));
			return null;
		}
	}
	internal static bool TryWriteData(Save save, DataSection section, SerDict dict)
	{
		IO.DirectoryInfo dir = new(SaveFolder(save));
		IO.FileInfo file = new(SaveFile(save, section));
		try
		{
			if (!dir.Exists) dir.Create();
			file.Refresh();
			using IO.StreamWriter writer = file.CreateText();
			writer.Write(Json.Serialize(dict));
			return true;
		}
		catch (Exception ex)
		{
			plog.LogError(ErrorMessage(Site.WriteData, $"error writing {section} for slot {save} ({file.FullName})", ex));
			return false;
		}
	}
	internal static void EraseData(in Save save)
	{
		foreach (DataSection sec in Enum.GetValues(typeof(DataSection)))
		{
			try
			{
				IO.FileInfo fi = new(SaveFile(save, sec));
				if (fi.Exists) fi.Delete();
			}
			catch (IO.IOException ex)
			{
				plog.LogError(ErrorMessage(Site.WipeData, $"Error erasing file for {save}", ex));
			}
		}
	}
	#endregion filemanip
	#region pathbuild
	internal static VarSet VarsForSave(Save save)
		=> VarsPerSave.AddIfNone_Get(save, () => new(save));
	internal static string SaveFolder(in Save save)
		=> CombinePath(RootFolderDirectory(), "UserData", "Atmo", $"{save.a}");
	internal static string SaveFile(in Save save, DataSection section)
		=> CombinePath(SaveFolder(save), $"{SlugName(save.b)}_{section}.json");
	internal static string GlobalFile(int slot) 
		=> CombinePath(SaveFolder(new(slot, -1)), "global.json");
	#endregion pathbuild
	internal static Save MakeSD(int slot, int @char) 
		=> new(slot, @char, "SaveData", "slot", "char");
	private static string ErrorMessage(Site site, string message, Exception? ex)
		=> $"{nameof(VarRegistry)}: {site}: {message}\nException: {ex?.ToString() ?? "NULL"}";
	#endregion methods
	#region nested
	internal enum DataSection
	{
		Normal,
		Persistent,
		//global
	}
	private enum Site
	{
		ReadData,
		WipeData,
		WriteData,
		HookWipe,
		HookNormal,
		HookPersistent,
		Init,
		Clear
	}
	#endregion
}
