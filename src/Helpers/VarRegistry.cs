using SerDict = System.Collections.Generic.Dictionary<string, object>;
using NamedVars = System.Collections.Generic.Dictionary<string, Atmo.Helpers.Arg>;
using Save = Atmo.Helpers.Utils.VT<int, int>;

namespace Atmo.Helpers;
/// <summary>
/// Allows accessing a pool of variables, global or save-specific.
/// </summary>
public static class VarRegistry
{
	#region fields / consts / props
	/// <summary>
	/// <see cref="DeathPersistentSaveData"/> hash to slugcat number.
	/// </summary>
	private readonly static Dictionary<int, int> DPSD_Slug = new();
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
	internal static readonly Dictionary<int, NamedVars> VarsGlobal = new();
	//todo: special variables for internals
	#endregion
	#region lifecycle
	internal static void Clear()
	{
		plog.LogDebug("Clear VarRegistry hooks");
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
	internal static void Init()
	{
		//todo: globals ser
		plog.LogDebug("Init VarRegistry hooks");


		On.SaveState.LoadGame += ReadNormal;
		On.SaveState.SaveToString += WriteNormal;

		On.DeathPersistentSaveData.ctor += RegDPSD;
		On.DeathPersistentSaveData.FromString += ReadPers;
		On.DeathPersistentSaveData.SaveToString += WritePers;

		On.PlayerProgression.WipeAll += WipeAll;
		On.PlayerProgression.WipeSaveState += WipeSavestate;
	}
	private static void WipeSavestate(On.PlayerProgression.orig_WipeSaveState orig, PlayerProgression self, int saveStateNumber)
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
	private static void WipeAll(On.PlayerProgression.orig_WipeAll orig, PlayerProgression self)
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
			var data = VarsPerSave
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
			var data = VarsPerSave
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
	#endregion
	#region methods
	/// <summary>
	/// Fetches a stored variable. Creates a new one if does not exist. Looks up 
	/// </summary>
	/// <param name="name"></param>
	/// <param name="saveslot"></param>
	/// <param name="character"></param>
	/// <returns></returns>
	public static Arg GetVar(string name!!, int saveslot, int character = -1)
	{
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
		IO.FileInfo fi = new (GlobalFile(slot));
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
		=> CombinePath(SaveFolder(save), $"{save.b}_{section}.json");
	internal static string GlobalFile(int slot) => CombinePath(SaveFolder(new(slot, -1)), "global.json");
	#endregion pathbuild
	internal static Save MakeSD(int slot, int @char)
	{
		using (_ = new Save.Names("SaveData", "slot", "char"))
			return new(slot, @char, "SaveData", "slot", "char");
	}
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
	}
	#endregion
}
