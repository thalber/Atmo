using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Atmo.Helpers;
using static Atmo.API;
using static Atmo.Atmod;
using static Atmo.HappenBuilding;
using static Atmo.HappenTrigger;
using static Atmo.Helpers.Utils;
using static UnityEngine.Mathf;
using LOG = BepInEx.Logging;
using RND = UnityEngine.Random;
using TXT = System.Text.RegularExpressions;
using UAD = UpdatableAndDeletable;

namespace Atmo;

internal static partial class HappenBuilding
{
	internal static void InitBuiltins()
	{
		foreach (var initfun in new[] { RegisterBuiltinActions, RegisterBuiltinTriggers })
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
		AddNamedTrigger(new[] { "always" }, (args, ha, rwg) => new Always());
		AddNamedTrigger(new[] { "untilrain", "beforerain" }, (args, ha, rwg) =>
		{
			float.TryParse(args.AtOr(0, "0"), out var delay);
			return new BeforeRain(rwg, ha, (int)(delay * 40f));
		});
		AddNamedTrigger(new[] { "afterrain" }, (args, ha, rwg) =>
		{
			float.TryParse(args.AtOr(0, "0"), out var delay);
			return new AfterRain(rwg, ha, (int)(delay * 40f));
		});
		AddNamedTrigger(new[] { "everyx", "every" }, (args, ha, rwg) =>
		{
			float.TryParse(args.AtOr(0, "4"), out var period);
			return new EveryX((int)(period * 40f), ha);
		});
		AddNamedTrigger(new[] { "maybe", "chance" }, (args, ha, rwg) =>
		{
			float.TryParse(args.AtOr(0, "0.5"), out var ch);
			return new Maybe(ch);
		});
		AddNamedTrigger(new[] { "flicker" }, (args, ha, rwg) =>
		{
			var argsp = new int[4];
			for (var i = 0; i < 4; i++)
			{
				float.TryParse(args.AtOr(i, "5"), out var pres);
				argsp[i] = (int)(pres * 40f);
			}
			var startOn = trueStrings.Contains(args.AtOr(4, "1").ToLower());
			return new Flicker(argsp[0], argsp[1], argsp[2], argsp[3], startOn);
		});
		AddNamedTrigger(new[] { "karma", "onkarma" }, (args, ha, rwg) => new OnKarma(rwg, args));
		AddNamedTrigger(new[] { "visited", "playervisited", "playervisit" }, (args, ha, rwg) => new AfterVisit(rwg, args));
		AddNamedTrigger(new[] { "fry", "fryafter" }, (args, ha, rwg) =>
		{
			float.TryParse(args.AtOr(0, "5"), out var lim);
			float.TryParse(args.AtOr(1, "10"), out var cd);
			return new Fry((int)(lim * 40f), (int)(cd * 40f));
		});
		AddNamedTrigger(new[] { "after", "afterother" }, (args, ha, rwg) =>
		{
			var other = args[0];
			int.TryParse(args[1], out var delay);
			return new AfterOther(ha, other, delay);
		});
		AddNamedTrigger(new[] { "delay", "ondelay" }, (args, ha, rwg) => new AfterVisit(rwg, args));
		//todo: update docs to reflect shift to seconds in parameters rather than frames
	}
	private static void RegisterBuiltinActions()
	{
		AddNamedAction(new[] { "playergrav", "playergravity" }, Make_Playergrav);
		AddNamedAction(new[] { "sound", "playsound" }, Make_Sound);
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
	private static void Make_PlayMusic(Happen ha, string[] rawargs)
	{
#warning unfinished
		ArgCollection args = rawargs;
		if (args.Count == 0)
		{
			inst.Plog.LogWarning($"Happen {ha.name}: music action: " +
			$"No arguments provided to Music action! won't do anything"); return;
		}
		var songname = args[0];
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
	private static void Make_Glow(Happen ha, string[] rawargs)
	{
		ArgCollection args = rawargs;
		var enabled = true;
		if (falseStrings.Contains(args.AtOr(0, "true").Str.ToLower())) enabled = false;
		else if (trueStrings.Contains(args.AtOr(0, "true").Str.ToLower())) enabled = true;
		ha.On_Init += (w) =>
		{
			var dspd = w.game.GetStorySession?.saveState;//./deathPersistentSaveData;
			if (dspd is null) return;
			dspd.theGlow = enabled;
		};
	}
	private static void Make_Mark(Happen ha, string[] args)
	{
		var enabled = true;
		if (falseStrings.Contains(args.AtOr(0, "true").ToLower())) enabled = false;
		else if (trueStrings.Contains(args.AtOr(0, "true").ToLower())) enabled = true;
		ha.On_Init += (w) =>
		{
			var dspd = w.game.GetStorySession?.saveState.deathPersistentSaveData;
			if (dspd is null) return;
			dspd.theMark = enabled;
		};
	}
	private static void Make_LogCall(Happen ha, string[] args)
	{
		//todo: revisit when variable support is here
		List<string> output = new();
		var sev = LOG.LogLevel.Debug;
		foreach (var arg in args)
		{
			string[] spl;
			if ((spl = arg.Split('=')).Length == 2)
			{
				switch (spl[0])
				{
				case "severity":
				case "level":
				if (TryParseEnum(spl[1], out LOG.LogLevel ll)) sev = ll;
				continue;
				}
				output.Add(arg);
			}
		}
		if (output.Count == 0) output.Add("[no message]");
		var result = string.Concat(output.ToArray());
		ha.On_Init += (w) =>
		{
			inst.Plog.Log(sev, result);
		};
	}
	private static void Make_SetRainTimer(Happen ha, string[] args)
	{
		int.TryParse(args.AtOr(0, "0"), out var target);
		ha.On_Init += (w) =>
		{
			w.rainCycle.timer = target;
		};
	}
	private static void Make_SetKarma(Happen ha, string[] args)
	{
		ha.On_Init += (w) =>
		{
			var dpsd = w.game?.GetStorySession?.saveState?.deathPersistentSaveData;
			if (dpsd is null || w.game is null) return;
			var ts = args.AtOr(0, "0");
			var karma = dpsd.karma;
			if (ts.StartsWith("+")) karma += int.Parse(ts.Substring(1));
			else if (ts.StartsWith("-")) karma -= int.Parse(ts.Substring(1));
			else karma = int.Parse(ts);
			karma = Clamp(karma, 0, 9);
			dpsd.karma = karma;
			foreach (var cam in w.game.cameras) { cam?.hud.karmaMeter?.UpdateGraphic(); }
		};
	}
	private static void Make_SetMaxKarma(Happen ha, string[] args)
	{
		ha.On_Init += (w) =>
		{
			var dpsd = w.game?.GetStorySession?.saveState?.deathPersistentSaveData;
			if (dpsd is null || w.game is null) return;
			var ts = args.AtOr(0, "0");
			var cap = dpsd.karmaCap;
			if (ts.StartsWith("+")) cap += int.Parse(ts.Substring(1));
			else if (ts.StartsWith("-")) cap -= int.Parse(ts.Substring(1));
			else cap = int.Parse(ts);
			cap = Clamp(cap, 0, 9);
			dpsd.karma = cap;
			foreach (var cam in w.game.cameras) { cam?.hud.karmaMeter?.UpdateGraphic(); }
		};
	}
	private static void Make_Playergrav(Happen ha, string[] args)
	{
		float.TryParse(args.AtOr(0, "0.5"), out var frac);
		ha.On_RealUpdate += (room) =>
		{
			for (var i = 0; i < room.updateList.Count; i++)
			{
				var uad = room.updateList[i];
				if (uad is Player p) p.gravity = frac;
			}
		};
	}
	private static void Make_Sound(Happen ha, string[] args)
	{
		//todo: add a version that does not cut off with room transition...
		if (args.Length == 0)
		{
			inst.Plog.LogError($"Happen {ha.name}: sound action: " +
				$"No arguments provided.");
			return;
		}

		if (!TryParseEnum(args[0], out SoundID soundid))
		{
			inst.Plog.LogError($"Happen {ha.name}: sound action: " +
				$"Invalid SoundID ({args[0]})");
			return;
		}

		int cooldown = 40,
			limit = int.MaxValue;
		float
			vol = 1f,
			pitch = 1f,
			pan = 1f;
		for (var i = 1; i < args.Length; i++)
		{
			var spl = TXT.Regex.Split(args[i], "=");
			if (spl.Length != 2) continue;
			switch (spl[0].ToLower())
			{
			case "cd":
			case "cooldown":
			int.TryParse(spl[1], out cooldown);
			break;
			case "vol":
			case "volume":
			float.TryParse(spl[1], out vol);
			break;
			case "pan":
			float.TryParse(spl[1], out pan);
			break;
			case "pitch":
			float.TryParse(spl[1], out pitch);
			break;
			case "lim":
			case "limit":
			int.TryParse(spl[1], out limit);
			break;
			}
		}
		var counter = 1;
		ha.On_RealUpdate += (room) =>
		{
			if (counter != 0) return;
			if (limit < 1) return;
			for (var i = 0; i < room.updateList.Count; i++)
			{
				if (room.updateList[i] is Player p)
				{
					var em = room.PlaySound(soundid, p.firstChunk, false, vol, pitch);
					counter = cooldown;
					limit--;
					return;
				}
			}
		};
		ha.On_CoreUpdate += (rwg) =>
		{
			if (counter > 0) counter--;
		};
	}
	private static void Make_Rumble(Happen ha, string[] args)
	{
		int cooldown = 200,
			duration = 80;
		float intensity = 1f,
			shake = 0.5f;

		for (var i = 0; i < args.Length; i++)
		{
			var spl = TXT.Regex.Split(args[i], "=");
			if (spl.Length != 2) continue;
			switch (spl[0].ToLower())
			{
			case "cooldown":
			case "cd":
			int.TryParse(spl[1], out cooldown);
			break;
			case "duration":
			int.TryParse(spl[1], out duration);
			break;
			case "intensity":
			float.TryParse(spl[1], out intensity);
			break;
			case "shake":
			float.TryParse(spl[1], out shake);
			break;
			}
		}
		var counter = 0;
		var active = duration;
		ha.On_RealUpdate += (room) =>
		{
			//if (active != 0)
			room.ScreenMovement(null, RND.insideUnitCircle * intensity, shake);
		};
	}
	private static void Make_ChangePalette(Happen ha, string[] args)
	{
		int.TryParse(args.AtOr(0, "15"), out var pal);
		string[] lastRoomPerCam = null;
		ha.On_RealUpdate += (rm) =>
		{
			if (lastRoomPerCam is null) return;
			for (var i = 0; i < lastRoomPerCam.Length; i++)
			{
				var cam = rm.game.cameras[i];
				if (cam.room != rm || !rm.BeingViewed || cam.AboutToSwitchRoom) continue;
				if (cam.room.abstractRoom.name != lastRoomPerCam[i])
				{
					cam.ChangeMainPalette(pal);
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
	//private class palchanger : UAD
	//{
	//    private bool done = false;
	//    internal int pal = 15;
	//    public override void Update(bool eu)
	//    {
	//        if (!done)
	//        {
	//            foreach (var cam in room.game.cameras)
	//            {
	//                if (cam.room == room) cam.ChangeMainPalette(pal);
	//            }
	//        }
	//    }
	//}
	#endregion
}
