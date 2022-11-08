using Atmo.Body;
using static Atmo.API;
using static Atmo.HappenTrigger;
using static UnityEngine.Mathf;

namespace Atmo.Gen;
public static partial class HappenBuilding
{
	internal static void InitBuiltins()
	{
		foreach (Action initfun in new[] { RegisterBuiltinActions, RegisterBuiltinTriggers })
		{
			try
			{
				initfun();
			}
			catch (Exception ex)
			{
				plog.LogFatal($"HappenBuilding: Static init: " +
					$"failed to {initfun.Method}:" +
					$"\n{ex}");
			}
		}
	}
	#region triggers
	private static void RegisterBuiltinTriggers()
	{
		AddNamedTrigger(new[] { "always" }, TMake_Always);
		AddNamedTrigger(new[] { "untilrain", "beforerain" }, TMake_UntilRain);
		AddNamedTrigger(new[] { "afterrain" }, TMake_AfterRain);
		AddNamedTrigger(new[] { "everyx", "every" }, TMake_EveryX);
		AddNamedTrigger(new[] { "maybe", "chance" }, TMake_Maybe);
		AddNamedTrigger(new[] { "flicker" }, TMake_Flicker);
		AddNamedTrigger(new[] { "karma", "onkarma" }, TMake_OnKarma);
		AddNamedTrigger(new[] { "visit", "playervisited", "playervisit" }, TMake_Visit);
		AddNamedTrigger(new[] { "fry", "fryafter" }, TMake_Fry);
		AddNamedTrigger(new[] { "after", "afterother" }, TMake_AfterOther);
		AddNamedTrigger(new[] { "delay", "ondelay" }, TMake_Delay);
		AddNamedTrigger(new[] { "playercount" }, TMake_PlayerCount);
		AddNamedTrigger(new[] { "difficulty", "ondifficulty" }, TMake_Difficulty);
		//todo: update docs to reflect shift to seconds in parameters rather than frames
	}
	private static HappenTrigger? TMake_Difficulty(ArgSet args, RainWorldGame rwg, Happen ha)
	{
		bool enabled = false;
		foreach (Arg arg in args)
		{
			arg.GetEnum(out SlugcatStats.Name name);
			if (name == rwg.GetStorySession.characterStats.name) enabled = true;
		}
		return new EventfulTrigger()
		{
			On_ShouldRunUpdates = () => enabled,
		};
	}
	private static HappenTrigger? TMake_PlayerCount(ArgSet args, RainWorldGame rwg, Happen ha)
	{
		int[] accepted = args.Select(x => (int)x).ToArray();
		return new EventfulTrigger()
		{
			On_ShouldRunUpdates = () => accepted.Contains(rwg.Players.Count)
		};
	}
	private static HappenTrigger? TMake_Delay(ArgSet args, RainWorldGame rwg, Happen ha)
	{
		int delay = args.Count switch
		{
			< 1 => args.AtOr(0, 0).I32,
			1 => (int)(args[0].F32 * 40f),
			> 1 => RND.Range((int?)(args.AtOr(0, 0f)?.F32 * 40f) ?? 0, (int?)(args.AtOr(1, 2400f)?.F32 * 40f) ?? 2400)
		};
		return new EventfulTrigger()
		{
			On_ShouldRunUpdates = () => rwg.world.rainCycle.timer > delay
		};
	}
	private static HappenTrigger? TMake_AfterOther(ArgSet args, RainWorldGame rwg, Happen ha)
	{
		if (args.Count < 2)
		{
			plog.LogWarning($"HappenBuilding: builtins: after trigger on {ha.name} needs name of the target trigger and delay!");
			return null;
		}
		Happen? tar = null;
		string tarname = args.AtOr(0, "none").Str;
		int delay = (int?)(args.AtOr(1, 3f).F32 * 40) ?? 40;
		bool tarWasOn = false;
		bool amActive = false;
		List<int> switchOn = new();
		List<int> switchOff = new();

		return new EventfulTrigger()
		{
			On_Update = () =>
			{
				tar ??= ha?.set.AllHappens.FirstOrDefault(x => x.name == tarname);
				if (tar is null) return;
				if (tar.Active != tarWasOn)
				{
					if (tar.Active)
					{
						switchOn.Add(delay);
					}
					else
					{
						switchOff.Add(delay);
					}
				}
				for (var i = 0; i < switchOn.Count; i++)
				{
					switchOn[i]--;
				}
				if (switchOn.FirstOrDefault() < 0)
				{
					switchOn.RemoveAt(0);
					amActive = true;
				}
				for (var i = 0; i < switchOff.Count; i++)
				{
					switchOff[i]--;
				}
				if (switchOff.FirstOrDefault() < 0)
				{
					switchOff.RemoveAt(0);
					amActive = false;
				}
				tarWasOn = tar.Active;
			},
			On_ShouldRunUpdates = () => amActive,
		};
	}
	private static HappenTrigger? TMake_Fry(ArgSet args, RainWorldGame rwg, Happen ha)
	{
		int limit = (int)(args.AtOr(0, 10f).F32 * 40f);
		int cd = (int)(args.AtOr(1, 15f).F32 * 40f);
		int counter = 0;
		bool active = true;
		return new EventfulTrigger()
		{
			On_EvalResults = (res) =>
			{
				if (active)
				{
					if (res) counter++; else { counter = 0; }
					if (counter > limit) { active = false; counter = cd; }
				}
				else
				{
					counter--;
					if (counter == 0) { active = true; counter = 0; }
				}
			},
			On_ShouldRunUpdates = () => active
		};
	}
	private static HappenTrigger? TMake_Visit(ArgSet args, RainWorldGame rwg, Happen ha)
	{
		string[] rooms = args.Select(x => x.Str).ToArray();
		bool visit = false;
		return new EventfulTrigger()
		{
			On_Update = () =>
			{
				if (visit) return;
				foreach (AbstractCreature? player in rwg.Players) if (rooms.Contains(player.Room.name))
					{
						visit = true;
					}
			},
			On_ShouldRunUpdates = () => visit
		};

		//return new AfterVisit(game, args);
	}
	private static HappenTrigger? TMake_Flicker(ArgSet args, RainWorldGame rwg, Happen ha)
	{
		int minOn = args.AtOr(0, 5.0f).I32,
			maxOn = args.AtOr(1, 5.0f).I32,
			minOff = args.AtOr(2, 5.0f).I32,
			maxOff = args.AtOr(3, 5.0f).I32;
		bool on = args.AtOr(4, true).Bool;
		int counter = 0;

		return new EventfulTrigger()
		{
			On_Update = () => { if (counter-- < 0) ResetCounter(!on); },
			On_ShouldRunUpdates = () => on,
		};
		void ResetCounter(bool next)
		{
			on = next;
			counter = on switch
			{
				true => RND.Range(minOn, maxOn),
				false => RND.Range(minOff, maxOff),
			};
		}
	}
	private static HappenTrigger? TMake_Always(ArgSet args, RainWorldGame rwg, Happen ha)
		=> new EventfulTrigger()
		{
			On_ShouldRunUpdates = () => true
		};
	private static HappenTrigger? TMake_Maybe(ArgSet args, RainWorldGame rwg, Happen ha)
	{
		bool yes = RND.value < args.AtOr(0, 0.5f).F32;
		return new EventfulTrigger()
		{
			On_ShouldRunUpdates = () => yes,
		};
	}
	private static HappenTrigger? TMake_OnKarma(ArgSet args, RainWorldGame rwg, Happen _)
	{
		List<int> levels = new();
		foreach (Arg op in args)
		{
			if (int.TryParse(op.Str, out var r)) levels.Add(r);
			var spl = TXT.Regex.Split(op.Str, "\\s*-\\s*");
			if (spl.Length == 2)
			{
				int.TryParse(spl[0], out var min);
				int.TryParse(spl[1], out var max);
				for (var i = min; i <= max; i++) if (!levels.Contains(i)) levels.Add(i);
			}
		}
		return new EventfulTrigger()
		{
			On_ShouldRunUpdates = ()
				=> levels.Contains((rwg.Players[0].realizedCreature as Player)?.Karma ?? 0)
		};
	}
	private static HappenTrigger? TMake_UntilRain(ArgSet args, RainWorldGame rwg, Happen ha)
	{
		int delay = (int?)(args.AtOr(0, 0)?.F32 * 40) ?? 0;
		return new EventfulTrigger()
		{
			On_ShouldRunUpdates = ()
			=> rwg.world.rainCycle.TimeUntilRain + delay >= 0
		};
	}
	private static HappenTrigger? TMake_AfterRain(ArgSet args, RainWorldGame rwg, Happen ha)
	{
		int delay = (int?)(args.AtOr(0, 0)?.F32 * 40) ?? 0;
		return new EventfulTrigger()
		{
			On_ShouldRunUpdates = ()
			=> rwg.world.rainCycle.TimeUntilRain + delay <= 0
		};
	}
	private static HappenTrigger? TMake_EveryX(ArgSet args, RainWorldGame rwg, Happen ha)
	{
		int period = args.AtOr(0, 10).I32,
			counter = 0;// ?? 10;

		return new EventfulTrigger()
		{
			On_Update = () => { if (--counter < 0) counter = period; },
			On_ShouldRunUpdates = () => { return counter == 0; }
		};
	}
	#endregion
	#region actions
#warning contributor notice: actions
	private static void RegisterBuiltinActions()
	{
		AddNamedAction(new[] { "playergrav", "playergravity" }, Make_Playergrav);
		AddNamedAction(new[] { "sound", "playsound" }, Make_Sound);
		AddNamedAction(new[] { "soundloop" }, Make_SoundLoop);
		AddNamedAction(new[] { "rumble", "screenshake" }, Make_Rumble);
		AddNamedAction(new[] { "karma", "setkarma" }, Make_SetKarma);
		AddNamedAction(new[] { "karmacap", "maxkarma", "setmaxkarma" }, Make_SetMaxKarma);
		AddNamedAction(new[] { "log", "message" }, Make_LogCall);
		AddNamedAction(new[] { "mark", "themark" }, Make_Mark);
		AddNamedAction(new[] { "glow", "theglow" }, Make_Glow);
		AddNamedAction(new[] { "raintimer", "setraintimer" }, Make_SetRainTimer);
		AddNamedAction(new[] { "palette", "changepalette" }, Make_ChangePalette);
		AddNamedAction(new[] { "setvar", "setvariable" }, Make_SetVar);
		//AddNamedAction(new[] { "music", "playmusic" }, Make_PlayMusic);
		//AddNamedAction()
	}
	//add your custom actions in methods here
	//Use methods with the same structure. Don't forget to also add them to the inst method above.
	//Do not remove the warning directive.
	private static void Make_SoundLoop(Happen ha, ArgSet args)
	{
		//does not work in HI (???). does not automatically get discontinued when leaving an affected room.
		//todo: now works in HI. Breaks with warp.
		if (args.Count == 0)
		{
			plog.LogError($"Happen {ha.name}: soundloop action: " +
				$"No arguments provided.");
			return;
		}
		if (!TryParseEnum(args[0].Str, out SoundID soundid))
		{
			plog.LogError($"Happen {ha.name}: soundloop action: " +
				$"Invalid SoundID ({args[0]})");
			return;
		}
		Arg
			sid = args[0],
			vol = args["vol", "volume"]?.F32 ?? 1f,
			pitch = args["pitch"]?.F32 ?? 1f,
			pan = args["pan"]?.F32 ?? 0f,
			limit = args["lim", "limit"]?.F32 ?? float.PositiveInfinity;
		string lastSid = sid.Str;
		int timeAlive = 0;
		Dictionary<string, DisembodiedLoopEmitter> soundloops = new();//hashes = new();
		ha.On_RealUpdate += (rm) =>
		{
			if (timeAlive > limit.F32 * 40f) return;
			for (int i = 0; i < rm.updateList.Count; i++)
			{
				if (rm.BeingViewed && rm.updateList[i] is DisembodiedLoopEmitter dle && soundloops.ContainsValue(dle))
				{
					dle.soundStillPlaying = true;
					dle.alive = true;
					return;
				}
			}
			if (!rm.BeingViewed) return;
			plog.DbgVerbose($"{ha.name}: Need to create a new soundloop in {rm.abstractRoom.name}! {soundloops.GetHashCode()}");
			DisembodiedLoopEmitter? newdle = rm.PlayDisembodiedLoop(soundid, vol.F32, pitch.F32, pan.F32);
			newdle.requireActiveUpkeep = true;
			newdle.alive = true;
			newdle.soundStillPlaying = true;
			soundloops.Set(rm.abstractRoom.name, newdle);
		};
		//ha.On_AbstUpdate += (ar, t) =>
		//{
		//	//clean up hashes from abstractized rooms
		//	if (soundloops.TryGetValue(ar.name, out var here) && ar.realizedRoom is null) soundloops.Remove(ar.name);//here.Clear();
		//};
		ha.On_CoreUpdate += (rwg) =>
		{
			if (ha.Active) timeAlive++;
			//lazy enum parsing
			if (sid.Str != lastSid)
			{
				sid.GetEnum(out soundid);
			}
			lastSid = sid.Str;
			if (!ha.Active) soundloops.Clear();
		};
	}
	private static void Make_Sound(Happen ha, ArgSet args)
	{
		if (args.Count == 0)
		{
			plog.LogError($"Happen {ha.name}: sound action: " +
				$"No arguments provided.");
			return;
		}

		if (!TryParseEnum(args[0].Str, out SoundID soundid))
		{
			plog.LogError($"Happen {ha.name}: sound action: " +
				$"Invalid SoundID ({args[0]})");
			return;
		}
		Arg sid = args[0];
		string lastSid = sid.Str;
		int cooldown = args["cd", "cooldown"]?.I32 ?? 40,
			limit = args["lim", "limit"]?.I32 ?? int.MaxValue;
		float
			vol = args["vol", "volume"]?.F32 ?? 0.5f,
			pitch = args["pitch"]?.F32 ?? 1f,
			pan = args["pan"]?.F32 ?? 1f;
		int counter = 1;
		ha.On_RealUpdate += (room) =>
		{
			if (counter != 0) return;
			if (limit < 1) return;
			for (var i = 0; i < room.updateList.Count; i++)
			{
				if (room.updateList[i] is Player p)
				{
					ChunkSoundEmitter? em = room.PlaySound(soundid, p.firstChunk, false, vol, pitch);
					counter = cooldown;
					limit--;
					return;
				}
			}
		};
		ha.On_CoreUpdate += (rwg) =>
		{
			if (counter > 0) counter--;
			if (sid.Str != lastSid)
			{
				sid.GetEnum(out soundid);
			}
			lastSid = sid.Str;
		};
	}
	private static void Make_Glow(Happen ha, ArgSet args)
	{
		Arg enabled = args.AtOr(0, true);
		ha.On_Init += (w) =>
		{
			SaveState? ss = w.game.GetStorySession?.saveState;//./deathPersistentSaveData;
			if (ss is null) return;
			ss.theGlow = enabled.Bool;
		};
	}
	private static void Make_Mark(Happen ha, ArgSet args)
	{
		Arg enabled = args.AtOr(0, true);
		ha.On_Init += (w) =>
		{
			DeathPersistentSaveData? dspd = w.game.GetStorySession?.saveState.deathPersistentSaveData;
			if (dspd is null) return;
			dspd.theMark = enabled.Bool;
		};
	}
	private static void Make_LogCall(Happen ha, ArgSet args)
	{
		Arg sev = args["sev", "severity"] ?? new Arg(LOG.LogLevel.Message.ToString());
		sev.GetEnum(out LOG.LogLevel sevVal);
		string lastSev = sev.Str;

		Arg? onInit = args["init", "oninit"];
		Arg? onAbst = args["abst", "abstractupdate", "abstup"];
		if (onInit is not null) ha.On_Init += (w) =>
		{
			plog.Log(sevVal, $"{ha.name}:\"{onInit.Str}\"");
		};
		if (onAbst is not null) ha.On_AbstUpdate += (abstr, t) =>
		{
			plog.Log(sevVal, $"{ha.name}:\"{onAbst.Str}\":{abstr.name}:{t}");
		};
		ha.On_CoreUpdate += (_) =>
		{
			if (sev.Str != lastSev)
			{
				sev.GetEnum(out sevVal);
			}
			lastSev = sev.Str;
		};
	}
	private static void Make_SetRainTimer(Happen ha, ArgSet args)
	{
		Arg target = args.AtOr(0, 0);
		//int.TryParse(args.AtOr(0, "0").Str, out var target);
		ha.On_Init += (w) =>
		{
			w.rainCycle.timer = target.I32;
		};
	}
	private static void Make_SetKarma(Happen ha, ArgSet args)
	{
		ha.On_Init += (w) =>
		{
			DeathPersistentSaveData? dpsd = w.game?.GetStorySession?.saveState?.deathPersistentSaveData;
			if (dpsd is null || w.game is null) return;
			Arg ts = args.AtOr(0, 0);
			int karma = dpsd.karma;
			if (ts.Name is "add" or "+") karma += ts.I32;
			else if (ts.Name is "sub" or "substract" or "-") karma -= ts.I32;
			else karma = ts.I32;
			karma = Clamp(karma, 0, 9);
			dpsd.karma = karma;
			foreach (RoomCamera cam in w.game.cameras) { cam?.hud.karmaMeter?.UpdateGraphic(); }
		};
	}
	private static void Make_SetMaxKarma(Happen ha, ArgSet args)
	{
		ha.On_Init += (w) =>
		{
			DeathPersistentSaveData? dpsd = w.game?.GetStorySession?.saveState?.deathPersistentSaveData;
			if (dpsd is null || w.game is null) return;
			Arg ts = args.AtOr(0, 0);
			var cap = dpsd.karmaCap;
			if (ts.Name is "add" or "+") cap += ts.I32;
			else if (ts.Name is "sub" or "-") cap -= ts.I32;
			else cap = ts.I32;
			cap = Clamp(cap, 0, 9);
			dpsd.karma = cap;
			foreach (RoomCamera? cam in w.game.cameras) { cam?.hud.karmaMeter?.UpdateGraphic(); }
		};
	}
	private static void Make_Playergrav(Happen ha, ArgSet args)
	{
		Arg frac = args.AtOr(0, 0);
		//float.TryParse(args.AtOr(0, "0.5"), out var frac);
		ha.On_RealUpdate += (room) =>
		{
			for (var i = 0; i < room.updateList.Count; i++)
			{
				UAD? uad = room.updateList[i];
				if (uad is Player p)
				{
					foreach (BodyChunk? bc in p.bodyChunks)
					{
						bc.vel.y += frac.F32;
					}
				}
			}
		};
	}
	private static void Make_Rumble(Happen ha, ArgSet args)
	{
		Arg intensity = args["int", "intensity"]?.F32 ?? 1f,
			shake = args["shake"]?.F32 ?? 0.5f;
		ha.On_RealUpdate += (room) =>
		{
			room.ScreenMovement(null, RND.insideUnitCircle * intensity.F32, shake.F32);
		};
	}
	private static void Make_ChangePalette(Happen ha, ArgSet args)
	{
		//todo: support for fade palettes?
		Arg? palA = args.AtOr(0, 15);//["palA", "A", "1"];
		string[] lastRoomPerCam = null;
		ha.On_RealUpdate += (rm) =>
		{
			if (lastRoomPerCam is null) return;
			for (var i = 0; i < lastRoomPerCam.Length; i++)
			{
				RoomCamera? cam = rm.game.cameras[i];
				if (cam.room != rm || !rm.BeingViewed || cam.AboutToSwitchRoom) continue;
				if (cam.room.abstractRoom.name != lastRoomPerCam[i])
				{
					if (palA is not null)
					{
						cam.ChangeMainPalette(palA.I32);
						plog.DbgVerbose($"changing palette to {palA.I32}");
					}
				}
			}
		};
		ha.On_CoreUpdate += (rwg) =>
		{
			if (lastRoomPerCam is null) lastRoomPerCam = new string[rwg.cameras.Length];
			else for (var i = 0; i < rwg.cameras.Length; i++)
				{
					lastRoomPerCam[i] = rwg.cameras[i].room.abstractRoom.name;
				}
		};
	}
	private static void Make_SetVar(Happen ha, ArgSet args)
	{
		Arg argn = args.AtOr(0, "testvar"),
			argv = args.AtOr(1, "TESTVALUE");
		ha.On_Init += (_) =>
		{
			Arg target = VarRegistry.GetVar(argn.Str, CurrentSaveslot ?? 0, CurrentCharacter ?? 0);
			target.Str = argv.Str;
		};
	}
	#endregion
}
