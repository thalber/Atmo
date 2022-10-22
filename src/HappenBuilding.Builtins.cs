using static Atmo.API;
using static Atmo.HappenTrigger;
using static UnityEngine.Mathf;

namespace Atmo;
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
				inst.Plog.LogFatal($"HappenBuilding: Static init: " +
					$"failed to {initfun.Method}:" +
					$"\n{ex}");
			}
		}
	}
	private static void RegisterBuiltinTriggers()
	{
		AddNamedTrigger(new[] { "always" }, (args, rwg, ha) => new Always());
		AddNamedTrigger(new[] { "untilrain", "beforerain" }, (args, rwg, ha) =>
		{
			//float.TryParse(args.AtOr(0, "0"), out var delay);
			return new BeforeRain(rwg, ha, args.AtOr(0, 0f));
		});
		AddNamedTrigger(new[] { "afterrain" }, (args, rwg, ha) =>
		{
			//float.TryParse(args.AtOr(0, "0"), out var delay);
			return new AfterRain(rwg, ha, args.AtOr(0, 0f));
		});
		AddNamedTrigger(new[] { "everyx", "every" }, (args, rwg, ha) =>
		{
			//float.TryParse(args.AtOr(0, "4"), out var period);
			return new EveryX(args.AtOr(0, 4f), ha);
		});
		AddNamedTrigger(new[] { "maybe", "chance" }, (args, rwg, ha) =>
		{
			//float.TryParse(args.AtOr(0, "0.5"), out var ch);
			return new Maybe(args.AtOr(0, 0.5f));
		});
		AddNamedTrigger(new[] { "flicker" }, (args, rwg, ha) =>
		{
			return new Flicker(
				args.AtOr(0, 5.0f),
				args.AtOr(1, 5.0f),
				args.AtOr(2, 5.0f),
				args.AtOr(3, 5.0f),
				args.AtOr(4, true).Bool);
		});
		AddNamedTrigger(new[] { "karma", "onkarma" }, (args, rwg, ha) => new OnKarma(rwg, args));
		AddNamedTrigger(new[] { "visited", "playervisited", "playervisit" }, (args, rwg, ha) => new AfterVisit(rwg, args));
		AddNamedTrigger(new[] { "fry", "fryafter" }, (args, rwg, ha) =>
		{
			return new Fry(args.AtOr(0, 5f), args.AtOr(1, 10f));
		});
		AddNamedTrigger(new[] { "after", "afterother" }, (args, rwg, ha) =>
		{
			if (args.Count < 2)
			{
				inst.Plog.LogWarning($"HappenBuilding: builtins: after trigger on {ha.name} needs name of the target trigger and delay!");
				return null;
			}
			return new AfterOther(ha, args[0], args[1]);
		});
		AddNamedTrigger(new[] { "delay", "ondelay" }, (args, rwg, ha) => new AfterVisit(rwg, args));
		//todo: update docs to reflect shift to seconds in parameters rather than frames
	}
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
		//AddNamedAction(new[] { "music", "playmusic" }, Make_PlayMusic);
		//AddNamedAction()
	}
	#region actions
#warning contributor notice: actions
	//add your custom actions in methods here
	//Use methods with the same structure. Don't forget to also add them to the inst method above.
	//Do not remove the warning directive.
	private static void Make_SoundLoop(Happen ha, ArgSet args)
	{
		if (args.Count == 0)
		{
			inst.Plog.LogError($"Happen {ha.name}: soundloop action: " +
				$"No arguments provided.");
			return;
		}
		if (!TryParseEnum(args[0].Str, out SoundID soundid))
		{
			inst.Plog.LogError($"Happen {ha.name}: soundloop action: " +
				$"Invalid SoundID ({args[0]})");
			return;
		}
		Arg
			sid = args[0],
			vol = args["vol", "volume"]?.F32 ?? 1f,
			pitch = args["pitch"]?.F32 ?? 1f,
			pan = args["pan"]?.F32 ?? 0f;
		//smStep = args["smoothenstep", "step"]?.F32 ?? 0.05f,
		//smLerp = args["smoothenlerp", "lerp"]?.F32 ?? 0.07f
		;
		string lastSid = sid.Str;
		Dictionary<string, DisembodiedLoopEmitter> soundloops = new();//hashes = new();
		ha.On_RealUpdate += (rm) =>
		{
			for (int i = 0; i < rm.updateList.Count; i++)
			{
				if (rm.updateList[i] is DisembodiedLoopEmitter dle && soundloops.ContainsValue(dle))
				{
					dle.soundStillPlaying = true;
					dle.alive = true;
					return;
				}
			}
			inst.Plog.LogDebug("Need to create a new soundloop!");
			DisembodiedLoopEmitter? newdle = rm.PlayDisembodiedLoop(soundid, vol.F32, pitch.F32, pan.F32);
			newdle.requireActiveUpkeep = false;
			newdle.alive = true;
			newdle.soundStillPlaying = true;
			soundloops.AddOrUpdate(rm.abstractRoom.name, newdle);
		};
		ha.On_AbstUpdate += (ar, t) =>
		{
			//clean up hashes from abstractized rooms
			if (soundloops.TryGetValue(ar.name, out var here) && ar.realizedRoom is null) soundloops.Remove(ar.name);//here.Clear();
		};
		ha.On_CoreUpdate += (rwg) =>
		{
			//lazy enum parsing
			if (sid.Str != lastSid)
			{
				sid.AsEnum(out soundid);
			}
			lastSid = sid.Str;
			if (!ha.Active) { foreach (var dsl in soundloops.Values) dsl.requireActiveUpkeep = true; soundloops.Clear(); }
		};
	}
	private static void Make_Sound(Happen ha, ArgSet args)
	{
		if (args.Count == 0)
		{
			inst.Plog.LogError($"Happen {ha.name}: sound action: " +
				$"No arguments provided.");
			return;
		}

		if (!TryParseEnum(args[0].Str, out SoundID soundid))
		{
			inst.Plog.LogError($"Happen {ha.name}: sound action: " +
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
				sid.AsEnum(out soundid);
			}
			lastSid = sid.Str;
		};
	}
	private static void Make_PlayMusic(Happen ha, ArgSet args)
	{
#warning unfinished
		if (args.Count == 0)
		{
			inst.Plog.LogWarning($"Happen {ha.name}: music action: " +
			$"No arguments provided to Music action! won't do anything"); return;
		}
		Arg songname = args[0];
		float
			prio = 0.5f,
			maxthreat = 1f,
			vol = 0.3f;
		int
			roomsrange = -1,
			rest = 5;

		MusicEvent mev = new();
		for (var i = 1; i < args.Count; i++)
		{
			var spl = ((string)args[i]).Split('=');
			if (spl.Length != 2) continue;
			switch (spl[0].ToLower())
			{
			case "prio":
			case "priority":
			float.TryParse(spl[1], out prio);
			break;
			case "maxthreat":
			float.TryParse(spl[1], out maxthreat);
			break;
			case "vol":
			float.TryParse(spl[1], out vol);
			break;
			}
		}
		ha.On_Init += (w) =>
		{
			w.game.manager.musicPlayer?.GameRequestsSong(mev);
		};
	}
	private static void Make_Glow(Happen ha, ArgSet args)
	{
		Arg enabled = args.AtOr(0, true);
		ha.On_Init += (w) =>
		{
			var dspd = w.game.GetStorySession?.saveState;//./deathPersistentSaveData;
			if (dspd is null) return;
			dspd.theGlow = enabled.Bool;
		};
	}
	private static void Make_Mark(Happen ha, ArgSet args)
	{
		Arg enabled = args.AtOr(0, true);
		ha.On_Init += (w) =>
		{
			var dspd = w.game.GetStorySession?.saveState.deathPersistentSaveData;
			if (dspd is null) return;
			dspd.theMark = enabled.Bool;
		};
	}
	private static void Make_LogCall(Happen ha, ArgSet args)
	{
		//todo: revisit when variable support is here
		List<string> output = new();
		Arg sev = args["sev", "severity"] ?? new Arg(LOG.LogLevel.Message.ToString());
		sev.AsEnum(out LOG.LogLevel sevVal);
		string lastSev = sev.Str;

		Arg? onInit = args["init", "oninit"];
		Arg? onAbst = args["abst", "abstractupdate", "abstup"];
		if (onInit is not null) ha.On_Init += (w) =>
		{
			inst.Plog.Log(sevVal, onInit.Str);
		};
		if (onAbst is not null) ha.On_AbstUpdate += (abstr, t) =>
		{
			inst.Plog.Log(sevVal, $"\"{onAbst.Str}\":{abstr}:{t}");
		};
		ha.On_CoreUpdate += (_) =>
		{
			if (sev.Str != lastSev)
			{
				sev.AsEnum(out sevVal);
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
			var dpsd = w.game?.GetStorySession?.saveState?.deathPersistentSaveData;
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
			var dpsd = w.game?.GetStorySession?.saveState?.deathPersistentSaveData;
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
				if (uad is Player p) p.gravity = frac.F32;
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
					if (palA is not null) cam.ChangeMainPalette(palA.I32);
					//if (palB is not null) cam.ChangeFadePalette(palB.I32);
					inst.Plog.LogDebug("changing palette");
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
	#endregion
}
