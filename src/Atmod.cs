﻿using Atmo.Body;
using Atmo.Gen;
using BepInEx;

using CFG = BepInEx.Configuration;
//using VREG = Atmo.Helpers.VarRegistry;

namespace Atmo;

/// <summary>
/// Atmo is a scripting layer mod targeted at region makers. This is the main plugin class.
/// <para>
/// To interact with the mod, see <seealso cref="API"/>. For internal details, see <seealso cref="Atmo.Gen"/> and <seealso cref="Atmo.Body"/> namespace contents.
/// </para>
/// </summary>
[BepInPlugin(Id, DName, Ver)]
public sealed partial class Atmod : BaseUnityPlugin
{
	#region field/const/prop
	/// <summary>
	/// Mod version
	/// </summary>
	public const string Ver = "0.6";
	/// <summary>
	/// Mod UID
	/// </summary>
	public const string Id = "thalber.atmod";
	/// <summary>
	/// Mod display name
	/// </summary>
	public const string DName = "Atmo";
	/// <summary>
	/// Static singleton
	/// </summary>
#pragma warning disable CS8618
	public static Atmod inst { get; private set; }
#pragma warning restore CS8618
	/// <summary>
	/// publicized logger
	/// </summary>
	internal static LOG.ManualLogSource plog => inst.Logger;
	internal static CFG.ConfigEntry<bool>? log_verbose;
	private static int? _tempSSN;
	internal static int? CurrentSaveslot => inst.RW?.options?.saveSlot;
	internal static int? CurrentCharacter
		=> (int?)
		inst.RW?.
		processManager.FindSubProcess<RainWorldGame>()?
		.GetStorySession?
		.characterStats.name
		?? _tempSSN;
	private bool setupRan = false;
	private bool dying = false;
	/// <summary>
	/// Currently active <see cref="HappenSet"/>. Null if not in session, if in arena session, or if failed to read from session.
	/// </summary>
	public HappenSet? CurrentSet { get; private set; }
	/// <summary>
	/// Reference to current RainWorld vanilla object.
	/// </summary>
	public RainWorld? RW { get; private set; }
	//todo: make it smoothly work with region switching.
	#endregion
	/// <summary>
	/// Applies hooks and sets <see cref="inst"/>.
	/// </summary>
	public void OnEnable()
	{
		const string CFG_LOGGING = "logging";
		inst = this;
		log_verbose = Config.Bind(CFG_LOGGING, "verbose", true, "Enable more verbose logging. Can create clutter.");
		try
		{
			On.AbstractRoom.Update += RunHappensAbstUpd;
			On.RainWorldGame.Update += DoBodyUpdates;
			On.Room.Update += RunHappensRealUpd;
			On.World.LoadWorld += FetchHappenSet;
			On.OverWorld.LoadFirstWorld += SetTempSSN;
			VarRegistry.Init();
			HappenBuilding.InitBuiltins();
		}
		catch (Exception ex)
		{
			Logger.LogFatal($"Error on enable!\n{ex}");
		}
	}
	/// <summary>
	/// Undoes hooks and spins up a static cleanup member cleanup procedure.
	/// </summary>
	public void OnDisable()
	{
		dying = true;
		try
		{
			//On.World.ctor -= FetchHappenSet;
			On.Room.Update -= RunHappensRealUpd;
			On.RainWorldGame.Update -= DoBodyUpdates;
			On.AbstractRoom.Update -= RunHappensAbstUpd;
			On.World.LoadWorld -= FetchHappenSet;
			On.OverWorld.LoadFirstWorld -= SetTempSSN;
			VarRegistry.Clear();

			LOG.ManualLogSource? cleanup_logger =
				LOG.Logger.CreateLogSource("Atmo_Purge");
			DBG.Stopwatch sw = new();
			bool verbose = log_verbose?.Value ?? false;
			sw.Start();
			cleanup_logger.LogMessage("Spooling cleanup thread.");
			System.Threading.ThreadPool.QueueUserWorkItem((_) =>
			{
				List<string> success = new();
				List<string> failure = new();
				foreach (Type t in typeof(Atmod).Assembly.GetTypes())
				{
					try
					{
						VT<List<string>, List<string>> res = t.CleanupStatic();
						success.AddRange(res.a);
						failure.AddRange(res.b);
					}
					catch (Exception ex)
					{
						cleanup_logger
						.LogError($"{t}: Unhandled Error cleaning up static fields:" +
							$"\n{ex}");
					}
				}
				sw.Stop();

				static string aggregator(string x, string y)
				{
					return $"{x}\n\t{y}";
				}

				cleanup_logger.LogDebug($"Finished statics cleanup: {sw.Elapsed}.");
				if (verbose)
				{
					cleanup_logger.LogDebug(
						$"Successfully cleared: {success.Stitch(aggregator)}");
					cleanup_logger.LogDebug(
						$"\nErrored on: {failure.Stitch(aggregator)}");
				}
			});
		}
		catch (Exception ex)
		{
			Logger.LogFatal($"Error on disable!\n{ex}");
		}
		finally
		{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
			inst = null;
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
		}
	}
	/// <summary>
	/// Cleans up set if not ingame, updates some builtin variables.
	/// </summary>
	public void Update()
	{
		if (dying) return;
		RW ??= CRW;
		if (!setupRan && RW is not null)
		{
			//maybe put something here
			setupRan = true;
		}
		VarRegistry.SpecialVars[VarRegistry.SpVar.time].Str = DateTime.Now.ToString();
		VarRegistry.SpecialVars[VarRegistry.SpVar.utctime].Str = DateTime.UtcNow.ToString();

		if (RW is null || CurrentSet is null) return;
		if (RW.processManager.currentMainLoop is RainWorldGame) return;
		foreach (MainLoopProcess? proc in RW.processManager.sideProcesses) if (proc is RainWorldGame) return;
		Logger.LogDebug("No RainWorldGame in processmanager, erasing currentset");
		CurrentSet = null;
	}
	#region lifecycle hooks
	/// <summary>
	/// Temporarily forces currentindex during LoadFirstWorld. Needed for <see cref="Helpers.VarRegistry"/> function.
	/// </summary>
	/// <param name="orig"></param>
	/// <param name="self"></param>
	private void SetTempSSN(On.OverWorld.orig_LoadFirstWorld orig, OverWorld self)
	{
		_tempSSN = self.PlayerCharacterNumber;
		orig(self);
		_tempSSN = null;
	}
	private void FetchHappenSet(On.World.orig_LoadWorld orig, World self, int slugcatNumber, List<AbstractRoom> abstractRoomsList, int[] swarmRooms, int[] shelters, int[] gates)
	{
		orig(self, slugcatNumber, abstractRoomsList, swarmRooms, shelters, gates);
		if (self.singleRoomWorld) return;
		try
		{
			CurrentSet = HappenSet.TryCreate(self);
		}
		catch (Exception e)
		{
			Logger.LogError($"Could not create a happenset: {e}");
		}
	}
	/// <summary>
	/// Sends an Update call to all events for loaded world
	/// </summary>
	/// <param name="orig"></param>
	/// <param name="self"></param>
	private void DoBodyUpdates(On.RainWorldGame.orig_Update orig, RainWorldGame self)
	{
		orig(self);
		if (CurrentSet is null) return;
		if (self.pauseMenu != null) return;
		foreach (Happen? ha in CurrentSet.AllHappens)
		{
			if (ha is null) continue;
			try
			{
				ha.CoreUpdate();
			}
			catch (Exception e)
			{
				Logger.LogError($"Error doing body update for {ha.name}:\n{e}");
			}
		}
	}
	/// <summary>
	/// Runs abstract world update for events in a room
	/// </summary>
	/// <param name="orig"></param>
	/// <param name="self"></param>
	/// <param name="timePassed"></param>
	private void RunHappensAbstUpd(On.AbstractRoom.orig_Update orig, AbstractRoom self, int timePassed)
	{
		orig(self, timePassed);
		if (CurrentSet is null) return;
		IEnumerable<Happen>? haps = CurrentSet.GetHappensForRoom(self.name);
		foreach (Happen? ha in haps)
		{
			if (ha is null) continue;
			try
			{
				if (ha.Active)
				{
					if (!ha.InitRan) { ha.Init(self.world); ha.InitRan = true; }
					ha.AbstUpdate(self, timePassed);
				}
			}
			catch (Exception e)
			{
				Logger.LogError($"Error running event abstupdate for room {self.name}:\n{e}");
			}
		}
	}
	/// <summary>
	/// Runs realized updates for events in a room
	/// </summary>
	/// <param name="orig"></param>
	/// <param name="self"></param>
	private void RunHappensRealUpd(On.Room.orig_Update orig, Room self)
	{
		//#warning issue: for some reason geteventsforroom always returns none on real update
		//in my infinite wisdom i set SU_S04 as test room instead of SU_C04. everything worked as intended except for my brain

		orig(self);
		//DBG.Stopwatch sw = DBG.Stopwatch.StartNew();
		if (CurrentSet is null) return;
		IEnumerable<Happen>? haps = CurrentSet.GetHappensForRoom(self.abstractRoom.name);
		foreach (Happen? ha in haps)
		{
			try
			{
				if (ha.Active)
				{
					if (!ha.InitRan) { ha.Init(self.world); ha.InitRan = true; }
					ha.RealUpdate(self);
				}
			}
			catch (Exception e)
			{
				Logger.LogError($"Error running event realupdate for room {self.abstractRoom.name}:\n{e}");
			}
		}
	}
	#endregion lifecycle hooks
}
