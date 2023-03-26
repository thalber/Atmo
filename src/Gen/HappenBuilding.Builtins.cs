﻿using Atmo.Body;
using Atmo.Data.Payloads;
using static Atmo.API.V0;
using static Atmo.Body.HappenTrigger;
using static Atmo.Data.VarRegistry;
using static UnityEngine.Mathf;
using RoomFlasher = Atmo.Helpers.EventfulUAD.Extra<float, float, bool>;

namespace Atmo.Gen;
public static partial class HappenBuilding {
	internal static void __InitBuiltins() {
		foreach (Action initfun in new[] { __RegisterBuiltinActions, __RegisterBuiltinTriggers, __RegisterBuiltinMetafun }) {
			try {
				initfun();
			}
			catch (Exception ex) {
				__logger.LogFatal($"HappenBuilding: Static init: " +
					$"failed to {initfun.Method}:" +
					$"\n{ex}");
			}
		}
	}
	#region triggers
#warning contributor notice: triggers
	//Place your trigger registration code here.
	//Do not remove this warning directive.
	internal static void __RegisterBuiltinTriggers() {
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
		AddNamedTrigger(new[] { "difficulty", "ondifficulty", "campaign" }, TMake_Difficulty);
		AddNamedTrigger(new[] { "vareq", "varequal", "variableeq" }, TMake_VarEq);
		AddNamedTrigger(new[] { "varne", "varnot", "varnotequal" }, TMake_VarNe);
		AddNamedTrigger(new[] { "varmatch", "variableregex", "varregex" }, TMake_VarMatch);
		AddNamedTrigger(new[] { "ghost", "echo" }, TMake_EchoPresence);
		//todo: document all triggers below:
		//do not document:
		AddNamedTrigger(new[] { "thisbreaks" }, (args, rwg, ha) => {
			Arg when = args.AtOr(0, "eval");
			EventfulTrigger evt = new();
			switch (((string)when)) {
			case "eval": evt.On_EvalResults += delegate { throw new Exception("Intentional exception on Eval"); }; break;
			case "upd": evt.On_Update += delegate { throw new Exception("Intentional exception on Update"); }; break;
			case "sru": evt.On_ShouldRunUpdates += delegate { throw new Exception("Intentional exception on ShouldRun"); }; break;
			}
			return evt;
		});
	}
	private static HappenTrigger? TMake_ExpeditionEnabled(ArgSet args, RainWorldGame rwg, Happen ha) {
		EventfulTrigger evt = new() {
			On_ShouldRunUpdates = () => ModManager.Expedition
		};
		return evt;
	}
	private static HappenTrigger? TMake_JollyEnabled(ArgSet args, RainWorldGame rwg, Happen ha) {
		EventfulTrigger evt = new() {
			On_ShouldRunUpdates = () => ModManager.JollyCoop
		};
		return evt;
	}
	private static HappenTrigger? TMake_MMFEnabled(ArgSet args, RainWorldGame rwg, Happen ha) {
		EventfulTrigger evt = new() {
			On_ShouldRunUpdates = () => ModManager.MMF
		};
		return evt;
	}
	private static HappenTrigger? TMake_MSCEnabled(ArgSet args, RainWorldGame rwg, Happen ha) {
		EventfulTrigger evt = new() {
			On_ShouldRunUpdates = () => ModManager.MSC
		};
		return evt;
	}
	private static HappenTrigger? TMake_RemixModEnabled(ArgSet args, RainWorldGame rwg, Happen ha) {
		if (args.Count < 1) {
			__NotifyArgsMissing(TMake_RemixModEnabled, "modid");
			return null;
		}
		EventfulTrigger evt = new() {
			On_ShouldRunUpdates = () => ModManager.ActiveMods.Any(x => x.name == args[0].Str)
		};
		return evt;
	}
	private static HappenTrigger? TMake_EchoPresence(ArgSet args, RainWorldGame rwg, Happen ha) {
		EventfulTrigger evt = new() {
			On_ShouldRunUpdates = () => rwg.world?.worldGhost is not null
		};
		return evt;
	}
	/// <summary>
	/// Creates a trigger that is active based on difficulty. 
	/// </summary>
	private static HappenTrigger? TMake_Difficulty(ArgSet args, RainWorldGame rwg, Happen ha) {
		bool enabled = false;
		foreach (Arg arg in args) {
			arg.GetExtEnum(out SlugcatStats.Name? name);
			if (name == rwg.GetStorySession.characterStats.name) enabled = true;
		}
		return new EventfulTrigger() {
			On_ShouldRunUpdates = () => enabled,
		};
	}
	/// <summary>
	/// Creates a trigger that is active on given player counts.
	/// </summary>
	private static HappenTrigger? TMake_PlayerCount(ArgSet args, RainWorldGame rwg, Happen ha) {
		int[] accepted = args.Select(x => (int)x).ToArray();
		return new EventfulTrigger() {
			On_ShouldRunUpdates = () => accepted.Contains(rwg.Players.Count)
		};
	}
	/// <summary>
	/// Creates a new trigger that is active after set delay (in seconds). If one argument is given, the delay is static. If two+ arguments are given, delay is randomly selected between them. Returns null if zero args.
	/// </summary>
	private static HappenTrigger? TMake_Delay(ArgSet args, RainWorldGame rwg, Happen ha) {
		int? delay = args.Count switch {
			< 1 => null,
			1 => args[0].SecAsFrames,
			> 1 => RND.Range(
				args[0].SecAsFrames,
				args[1].SecAsFrames)
		};
		if (delay is null) {
			__NotifyArgsMissing(TMake_Delay, "delay/delaymin+delaymax");
			return null;
		}
		__logger.DbgVerbose($"Set delay: {delay.Value} ( {args.Select(x => x.SecAsFrames.ToString()).Stitch()} )");
		return new EventfulTrigger() {
			On_ShouldRunUpdates = () => rwg.world.rainCycle.timer > delay.Value
		};
	}
	/// <summary>
	/// Creates a trigger that is active after another happen. First argument is target happen, second is delay in seconds.
	/// </summary>
	/// <returns>Null if no arguments.</returns>
	private static HappenTrigger? TMake_AfterOther(ArgSet args, RainWorldGame rwg, Happen ha) {
		if (args.Count < 1) {
			__NotifyArgsMissing(TMake_AfterOther, "name");
			return null;
		}
		Happen? tar = null;
		string tarname = args.AtOr(0, "none").Str;
		int delay = (int?)(args.AtOr(1, 3f).F32 * 40) ?? 40;
		bool tarWasOn = false;
		bool amActive = false;
		List<int> switchOn = new();
		List<int> switchOff = new();

		return new EventfulTrigger() {
			On_Update = () => {
				tar ??= ha?.Set.AllHappens.FirstOrDefault(x => x.name == tarname);
				if (tar is null) return;
				if (tar.Active != tarWasOn) {
					if (tar.Active) {
						switchOn.Add(delay);
					}
					else {
						switchOff.Add(delay);
					}
				}
				for (int i = 0; i < switchOn.Count; i++) {
					switchOn[i]--;
				}
				if (switchOn.FirstOrDefault() < 0) {
					switchOn.RemoveAt(0);
					amActive = true;
				}
				for (int i = 0; i < switchOff.Count; i++) {
					switchOff[i]--;
				}
				if (switchOff.FirstOrDefault() < 0) {
					switchOff.RemoveAt(0);
					amActive = false;
				}
				tarWasOn = tar.Active;
			},
			On_ShouldRunUpdates = () => amActive,
		};
	}
	/// <summary>
	/// Creates a trigger that is active, but turns off for a while if the happen stays on for too long. First argument is max tolerable time, second is time it takes to recover.
	/// </summary>
	/// <returns></returns>
	private static HappenTrigger? TMake_Fry(ArgSet args, RainWorldGame rwg, Happen ha) {
		int limit = (int)(args.AtOr(0, 10f).F32 * 40f);
		int cd = (int)(args.AtOr(1, 15f).F32 * 40f);
		int counter = 0;
		bool active = true;
		return new EventfulTrigger() {
			On_EvalResults = (res) => {
				if (active) {
					if (res) counter++; else { counter = 0; }
					if (counter > limit) { active = false; counter = cd; }
				}
				else {
					counter--;
					if (counter == 0) { active = true; counter = 0; }
				}
			},
			On_ShouldRunUpdates = () => active
		};
	}
	/// <summary>
	/// Creates a trigger that activates once player visits one of the provided rooms.
	/// </summary>
	private static HappenTrigger? TMake_Visit(ArgSet args, RainWorldGame rwg, Happen ha) {
		string[] rooms = args.Select(x => x.Str).ToArray();
		bool visit = false;
		return new EventfulTrigger() {
			On_Update = () => {
				if (visit) return;
				foreach (AbstractCreature? player in rwg.Players) if (rooms.Contains(player.Room.name)) {
						visit = true;
					}
			},
			On_ShouldRunUpdates = () => visit
		};

		//return new AfterVisit(game, args);
	}
	/// <summary>
	/// Creates a trigger that flickers on and off. Arguments are: min time on, max time on, min time off, max time off, start active (yes/no)
	/// </summary>
	private static HappenTrigger? TMake_Flicker(ArgSet args, RainWorldGame rwg, Happen ha) {
		int minOn = args.AtOr(0, 5.0f).SecAsFrames,
			maxOn = args.AtOr(1, 5.0f).SecAsFrames,
			minOff = args.AtOr(2, 5.0f).SecAsFrames,
			maxOff = args.AtOr(3, 5.0f).SecAsFrames;
		bool on = args.AtOr(4, true).Bool;
		int counter = 0;

		return new EventfulTrigger() {
			On_Update = () => { if (counter-- < 0) ResetCounter(!on); },
			On_ShouldRunUpdates = () => on,
		};
		void ResetCounter(bool next) {
			on = next;
			counter = on switch {
				true => RND.Range(minOn, maxOn),
				false => RND.Range(minOff, maxOff),
			};
		}
	}
	/// <summary>
	/// Creates a trigger that is always active.
	/// </summary>
	private static HappenTrigger? TMake_Always(ArgSet args, RainWorldGame rwg, Happen ha) {
		return new EventfulTrigger() {
			On_ShouldRunUpdates = () => true
		};
	}

	/// <summary>
	/// Creates a trigger that is active each cycle with a given chance (default 50%)
	/// </summary>
	private static HappenTrigger? TMake_Maybe(ArgSet args, RainWorldGame rwg, Happen ha) {
		Arg ch = args.AtOr(0, 0.5f);
		//float rv = RND.value;
		bool yes = RND.value < ch.F32;
		//plog.DbgVerbose($"bet {yes}: {ch} / {rv}");
		return new EventfulTrigger() {
			On_ShouldRunUpdates = () => yes,
		};
	}
	/// <summary>
	/// Creates a trigger that is active at given karma levels.
	/// </summary>
	private static HappenTrigger? TMake_OnKarma(ArgSet args, RainWorldGame rwg, Happen _) {
		List<int> levels = new();
		foreach (Arg op in args) {
			if (int.TryParse(op.Str, out int r)) levels.Add(r);
			string[]? spl = TXT.Regex.Split(op.Str, "\\s*-\\s*");
			if (spl.Length == 2) {
				int.TryParse(spl[0], out int min);
				int.TryParse(spl[1], out int max);
				for (int i = min; i <= max; i++) if (!levels.Contains(i)) levels.Add(i);
			}
		}
		return new EventfulTrigger() {
			On_ShouldRunUpdates = ()
				=> levels.Contains((rwg.Players[0].realizedCreature as Player)?.Karma ?? 0)
		};
	}
	/// <summary>
	/// Creates a trigger that is active before rain, with an optional delay (in seconds).
	/// </summary>
	private static HappenTrigger? TMake_UntilRain(ArgSet args, RainWorldGame rwg, Happen ha) {
		int delay = (int?)(args.AtOr(0, 0)?.F32 * 40) ?? 0;
		return new EventfulTrigger() {
			On_ShouldRunUpdates = ()
			=> rwg.world.rainCycle.TimeUntilRain + delay >= 0
		};
	}
	/// <summary>
	/// Creates a trigger that is active after rain, with an optional delay (in seconds).
	/// </summary>
	private static HappenTrigger? TMake_AfterRain(ArgSet args, RainWorldGame rwg, Happen ha) {
		int delay = (int?)(args.AtOr(0, 0)?.F32 * 40) ?? 0;
		return new EventfulTrigger() {
			On_ShouldRunUpdates = ()
			=> rwg.world.rainCycle.TimeUntilRain + delay <= 0
		};
	}
	/// <summary>
	/// Creates a trigger that activates once every X seconds.
	/// </summary>
	private static HappenTrigger? TMake_EveryX(ArgSet args, RainWorldGame rwg, Happen ha) {
		if (args.Count < 1) {
			__NotifyArgsMissing(TMake_EveryX, "period");
			return null;
		}
		int period = (int)(args[0].F32 * 40f),
			counter = 0;// ?? 10;

		return new EventfulTrigger() {
			On_Update = () => { if (--counter < 0) counter = period; },
			On_ShouldRunUpdates = () => { return counter == 0; }
		};
	}
	/// <summary>
	/// Creates a trigger that checks variable's equality against a given value. First argument is variable name, second is value to check against.
	/// </summary>
	private static HappenTrigger? TMake_VarEq(ArgSet args, RainWorldGame rwg, Happen ha) {
		if (args.Count < 2) {
			__NotifyArgsMissing(TMake_VarEq, "varname/value");
			return null;
		}
		Arg tar = VarRegistry.GetVar(args[0].Str, __CurrentSaveslot ?? -1, __CurrentCharacter ?? __slugnameNotFound);
		Arg val = args[1];
		return new EventfulTrigger() {
			On_ShouldRunUpdates = () => tar.Str == val.Str,
		};
	}
	/// <summary>
	/// Creates a trigger that checks a variable's inequality against a given value. First argument is variable name, second is value to check against.
	/// </summary>
	public static HappenTrigger? TMake_VarNe(ArgSet args, RainWorldGame rwg, Happen ha) {
		if (args.Count < 2) {
			__NotifyArgsMissing(TMake_VarNe, "varname/value");
			return null;
		}
		Arg tar = VarRegistry.GetVar(args[0].Str, __CurrentSaveslot ?? -1, __CurrentCharacter ?? __slugnameNotFound);
		Arg val = args[1];
		return new EventfulTrigger() {
			On_ShouldRunUpdates = () => tar.Str != val.Str,
		};
	}
	/// <summary>
	/// Creates a trigger that checks variable's match against a given regex. Responds to changing values.
	/// </summary>
	private static HappenTrigger? TMake_VarMatch(ArgSet args, RainWorldGame rwg, Happen ha) {
		if (args.Count < 2) {
			__NotifyArgsMissing(TMake_VarMatch, "varname/pattern");
			return null;
		}
		Arg tar = VarRegistry.GetVar(args[0].Str, __CurrentSaveslot ?? -1, __CurrentCharacter ?? __slugnameNotFound);
		Arg val = args[1];
		TXT.Regex? matcher = null;
		string? prev_val = null;

		return new EventfulTrigger() {
			On_Update = () => {
				try {
					if (prev_val != val.Str) {
						matcher = new(val.Str);
						prev_val = val.Str;
					}
				}
				catch (Exception ex) {
					__logger.LogWarning($"Error creating a regex from variable input \"{val.Str}\": {ex.Message}");
					matcher = null;
				}
			},
			On_ShouldRunUpdates = () => {
				return matcher?.IsMatch(tar.Str) ?? false;
			}
		};
	}
	#endregion
	#region actions
#warning contributor notice: actions
	//Add your action registration code here.
	//Do not remove this warning directive.
	private static void __RegisterBuiltinActions() {
		AddNamedAction(new[] { "playergrav", "playergravity" }, Make_Playergrav);
		AddNamedAction(new[] { "sound", "playsound" }, Make_Sound);
		AddNamedAction(new[] { "soundloop" }, Make_SoundLoop);
		AddNamedAction(new[] { "rumble", "screenshake" }, Make_Rumble);
		AddNamedAction(new[] { "karma", "setkarma" }, Make_SetKarma);
		AddNamedAction(new[] { "karmacap", "maxkarma", "setmaxkarma" }, Make_SetMaxKarma);
		AddNamedAction(new[] { "log", "message" }, Make_LogCall);
		AddNamedAction(new[] { "mark", "themark", "setmark" }, Make_Mark);
		AddNamedAction(new[] { "glow", "theglow", "setglow" }, Make_Glow);
		AddNamedAction(new[] { "raintimer", "cycleclock" }, Make_SetRainTimer);
		AddNamedAction(new[] { "palette", "changepalette" }, Make_ChangePalette);
		AddNamedAction(new[] { "setvar", "setvariable" }, Make_SetVar);
		AddNamedAction(new[] { "fling", "force" }, Make_Fling);
		AddNamedAction(new[] { "light", "tempglow" }, Make_Tempglow);
		AddNamedAction(new[] { "stun" }, Make_Stun);
		AddNamedAction(new[] { "lightning" }, Make_Lightning);
		AddNamedAction(new[] { "flash" }, Make_Flash);
		//todo: document all actions below:
		//do not document:
	}
	private static void Make_Flash(Happen ha, ArgSet args) {
		Arg
			color = args.AtOr(0, (Vector4)Color.white),
			maxopacity = args.AtOr(1, 0.9f),
			lerp = args["lerp"] ?? 0.04f,
			step = args["step"] ?? 0.01f;
		List<Guid> flashers = new();
		//fields are:
		//currentpow, oldpow, alive
		ha.On_RealUpdate += (room) => {
			var mine = (RoomFlasher)room.updateList.FirstOrDefault(x => x is RoomFlasher flasher && flashers.Contains(flasher.id));
			if (mine is null) {
				mine = new(1f, 1f, true);
				__logger.DbgVerbose("Creating new room flasher " + mine.id);
				flashers.Add(mine.id);
				mine.onUpdate = (_) => {
					mine._1 = mine._0;
					if (!mine._2) mine._0 = Clamp(LerpAndTick(mine._0, 0f, lerp.F32, step.F32), 0f, 1f);
					mine._2 = false;
					if (mine._0 == 0f) mine.Destroy();
				};
				mine.onInitSprites = (leaser, cam) => {
					leaser.sprites = new FSprite[1];
					leaser.sprites[0] = new FSprite("pixel", true) {
						scaleX = 1366f,
						scaleY = 768f,
						anchorX = 0f,
						anchorY = 0f,
						color = Color.white
					};
					mine.AddToContainer(leaser, cam, null);
				};
				mine.onAddToContainer = (leaser, cam, cont) => {
					leaser.sprites[0].RemoveFromContainer();
					//todo: u sure it should be HUD or HUD2?
					cont ??= cam.ReturnFContainer("HUD");
					cont.AddChild(leaser.sprites[0]);
					//leaser.sprites[0].addT
				};
				mine.onDestroy = () => { flashers.Remove(mine.id); };
				mine.onDraw = (leaser, cam, ts, cpos) => {
					leaser.sprites[0].alpha = Lerp(mine._1, mine._0, ts) * maxopacity.F32;
				};
				mine._0 = 1f;
				mine._1 = 1f;
				room.AddObject(mine);
			}
			mine._0 = 1f;
		};
	}
	private static void Make_Lightning(Happen ha, ArgSet args) {
		__logger.DbgVerbose("Making lightning!");
		if (args.Count < 1) {
			__NotifyArgsMissing(source: Make_Lightning, "intensity");
		}
		Arg
			intensity = args[0],
			bkgonly = args.AtOr(1, false);
		Dictionary<Room, bool> registered = new();
		ha.On_RealUpdate += (rm) => {
			registered.EnsureAndGet(rm, () => {
				__logger.DbgVerbose($"Lightning for room {rm.abstractRoom.name}");
				if (rm.lightning is not null) {
					__logger.DbgVerbose($"Woops, not mine");
					return false;
				}
				Lightning l = new(room: rm, intensity.F32, bkgonly.Bool);
				rm.lightning = l;
				rm.AddObject(l);
				return true;
			});
			rm.lightning.intensity = intensity.F32;
		};
		ha.On_CoreUpdate += (_) => {
			if (!ha.Active) {
				foreach ((Room rm, bool mine) in registered) {
					if (mine) {
						rm.RemoveObject(rm.lightning);
						rm.lightning = null;
					}
					__logger.DbgVerbose($"Removing lightning for {rm.abstractRoom.name}");
				}
				registered.Clear();
			}

		};
	}
	private static void Make_Stun(Happen ha, ArgSet args) {
		Arg select = args["select", "filter", "who"] ?? ".*",
			duration = args["duration", "dur", "st"] ?? 10;
		__logger.DbgVerbose($"Making stun:");
		ha.On_RealUpdate += (rm) => {
			for (int i = 0; i < rm.updateList.Count; i++) {
				UpdatableAndDeletable? uad = rm.updateList[i];
				if
				(uad is Creature c && TXT.Regex.IsMatch(
					c.Template.type.ToString(),
					select.Str,
					System.Text.RegularExpressions.RegexOptions.IgnoreCase)
				) {
					c.Stun(duration.I32);
				}
			}
		};
	}
	private static void Make_Tempglow(Happen ha, ArgSet args) {
		if (args.Count < 1) {
			__NotifyArgsMissing(Make_Tempglow, "color");
		}
		Arg argcol = args[0],
			radius = args.AtOr(1, 300f);
		Dictionary<Player, bool> playersActive = new();
		ha.On_RealUpdate += (rm) => {
			foreach (UAD? uad in rm.updateList) {
				if (uad is Player p) {
					p.glowing = true;
					PlayerGraphics? pgraf = p.graphicsModule as PlayerGraphics;
					playersActive.Set(p, true);
					if (pgraf?.lightSource is null) continue;
					pgraf.lightSource.color = argcol.Vec.ToOpaqueCol();
					pgraf.lightSource.setRad = radius.F32;
				}
			}
		};
		ha.On_CoreUpdate += (game) => {
			foreach (AbstractCreature? absp in game.Players) {
				if (absp.realizedCreature is Player p) {
					if (!playersActive.EnsureAndGet(p, static () => false)) {
						p.glowing = game.GetStorySession?.saveState.theGlow ?? false;
						if (p.graphicsModule is PlayerGraphics pgraf && pgraf.lightSource is not null) {
							pgraf.lightSource.setRad = 300f;
							pgraf.lightSource.color
								= PlayerGraphics.SlugcatColor(p.playerState.slugcatCharacter);
							if (!p.glowing) {
								pgraf.lightSource.stayAlive = false;
								pgraf.lightSource = null;
							}
						}

					}
					else {
						playersActive[p] = false;
					}
				}
			}
		};
	}
	private static void Make_Fling(Happen ha, ArgSet args) {
		if (args.Count < 1) {
			__NotifyArgsMissing(Make_Fling, "force");
		}
		Arg force = args[0],
			filter = args["filter", "select"] ?? ".*",
			forceVar = args["variance", "var"] ?? 0f,
			spread = args["spread", "deviation", "dev"] ?? 0f;
		__logger.DbgVerbose($"{force}, {filter.Raw} / {filter.Str}, {spread}");
		Dictionary<int, VT<float, float>> variance = new();
		ha.On_RealUpdate += (rm) => {
			foreach (UpdatableAndDeletable? uad in rm.updateList) {
				if (uad is PhysicalObject obj) {
					string objtype = obj.abstractPhysicalObject.type.ToString();
					string? crittype = (obj as Creature)?.Template.type.ToString();
					if
					(
					TXT.Regex.IsMatch(objtype, filter.Str, System.Text.RegularExpressions.RegexOptions.IgnoreCase)
					||
					(crittype is not null && TXT.Regex.IsMatch(crittype, filter.Str, System.Text.RegularExpressions.RegexOptions.IgnoreCase))
					) {
						VT<float, float> cvar = variance.EnsureAndGet
						(obj.GetHashCode(), () => new(
							1f.Deviate(forceVar.F32),
							0f.Deviate(spread.F32),
							"FlingDeviation",
							"force",
							"angle")
						);

						foreach (BodyChunk ch in obj.bodyChunks) {
							ch.vel += RotateAroundOrigo((Vector2)(force.Vec * cvar.a), cvar.b);
						}
					}
				}
			}
		};
		ha.On_CoreUpdate += (rwg) => {
			if (!ha.Active) variance.Clear();
		};
	}
	private static void Make_SoundLoop(Happen ha, ArgSet args) {
		//BUG: doesn't turn off when exiting into a room where the happen isnt active. need to kill them manually?
		//does not work in HI (???). does not automatically get discontinued when leaving an affected room.
		//Breaks with warp.
		if (args.Count == 0) {
			__NotifyArgsMissing(Make_SoundLoop, "soundid");
			return;
		}
		if (!SoundID.TryParse(typeof(SoundID), args[0].Str, true, out ExtEnumBase r_soundid)) {
			__NotifyArgsMissing(Make_SoundLoop, "soundid");
			return;
		}
		SoundID? soundid = (SoundID)r_soundid;
		Arg
			sid = args[0],
			vol = args["vol", "volume"] ?? 1f,
			pitch = args["pitch"] ?? 1f,
			pan = args["pan"] ?? 0f,
			limit = args["lim", "limit"] ?? float.PositiveInfinity;
		string lastSid = sid.Str;
		int timeAlive = 0;
		//int timer = 0;
		__logger.DbgVerbose(args.Select(x => x.ToString()).Stitch());
		Dictionary<string, DisembodiedLoopEmitter> soundloops = new();//hashes = new();
		ha.On_RealUpdate += (rm) => {
			if (timeAlive * 40 > limit.F32) return;
			for (int i = 0; i < rm.updateList.Count; i++) {
				if (rm.updateList[i] is DisembodiedLoopEmitter dle && soundloops.ContainsValue(dle)) {
					dle.soundStillPlaying = true;
					dle.alive = true;
					//plog.DbgVerbose($"Found loop! {dle.room}");
					return;
				}
			}
			__logger.DbgVerbose($"{ha.name}: Need to create a new soundloop in {rm.abstractRoom.name}! {soundloops.GetHashCode()}");
			DisembodiedLoopEmitter? newdle = rm.PlayDisembodiedLoop(soundid, vol.F32, pitch.F32, pan.F32);
			newdle.requireActiveUpkeep = true;
			newdle.alive = true;
			newdle.soundStillPlaying = true;
			soundloops.Set(rm.abstractRoom.name, newdle);
		};
		ha.On_CoreUpdate += (rwg) => {
			//timer--;
			//if (timer < 0)
			//{
			//	plog.DbgVerbose(soundloops.Keys.Stitch());
			//	plog.DbgVerbose(soundloops.Values
			//		.Select(x => $"({x.alive}, {x.soundStillPlaying})")
			//		.Stitch()
			//		);
			//	timer = 40;
			//}
			if (ha.Active) timeAlive++;
			//lazy enum parsing
			if (sid.Str != lastSid) {
				sid.GetExtEnum(out soundid);
			}
			lastSid = sid.Str;
			if (!ha.Active) soundloops.Clear();
		};
	}
	private static void Make_Sound(Happen ha, ArgSet args) {
		if (args.Count == 0) {
			__NotifyArgsMissing(Make_Sound, "soundid");
			return;
		}

		if (!ExtEnumBase.TryParse(typeof(SoundID), args[0].Str, false, out ExtEnumBase objsid)) {
			__logger.LogError($"Happen {ha.name}: sound action: " +
				$"Invalid SoundID ({args[0]})");
			return;
		}
		SoundID? soundid = (SoundID)objsid;
		Arg sid = args[0];
		string lastSid = sid.Str;
		int cooldown = args["cd", "cooldown"]?.SecAsFrames ?? 2,
			limit = args["lim", "limit"]?.I32 ?? int.MaxValue;
		Arg
			vol = args["vol", "volume"] ?? 0.5f,
			pitch = args["pitch"] ?? 1f
			//pan = args["pan"]?.F32 ?? 1f
			;
		int counter = 1;
		ha.On_RealUpdate += (room) => {
			if (counter != 0) return;
			if (limit < 1) return;
			for (int i = 0; i < room.updateList.Count; i++) {
				if (room.updateList[i] is Player p) {
					ChunkSoundEmitter? em = room.PlaySound(soundid ?? SoundID.HUD_Karma_Reinforce_Bump, p.firstChunk, false, vol.F32, pitch.F32);
					counter = cooldown;
					limit--;
					return;
				}
			}
		};
		ha.On_CoreUpdate += (rwg) => {
			if (counter > 0) counter--;
			if (sid.Str != lastSid) {
				sid.GetExtEnum(out soundid);
			}
			lastSid = sid.Str;
		};
	}
	private static void Make_Glow(Happen ha, ArgSet args) {
		Arg enabled = args.AtOr(0, true);
		ha.On_Init += (w) => {
			SaveState? ss = w.game.GetStorySession?.saveState;//./deathPersistentSaveData;
			if (ss is null) return;
			ss.theGlow = enabled.Bool;
		};
	}
	private static void Make_Mark(Happen ha, ArgSet args) {
		Arg enabled = args.AtOr(0, true);
		ha.On_Init += (w) => {
			DeathPersistentSaveData? dspd = w.game.GetStorySession?.saveState.deathPersistentSaveData;
			if (dspd is null) return;
			dspd.theMark = enabled.Bool;
		};
	}
	private static void Make_LogCall(Happen ha, ArgSet args) {
		Arg sev = args["sev", "severity"] ?? new Arg(LOG.LogLevel.Message.ToString());
		sev.GetEnum(out LOG.LogLevel sevVal);
		string lastSev = sev.Str;

		Arg? onInit = args["init", "oninit"];
		Arg? onAbst = args["abst", "abstractupdate", "abstup"];
		if (onInit is not null) ha.On_Init += (w) => {
			__logger.Log(sevVal, $"{ha.name}:\"{onInit.Str}\"");
		};
		if (onAbst is not null) ha.On_AbstUpdate += (abstr, t) => {
			__logger.Log(sevVal, $"{ha.name}:\"{onAbst.Str}\":{abstr.name}:{t}");
		};
		ha.On_CoreUpdate += (_) => {
			if (sev.Str != lastSev) {
				sev.GetEnum(out sevVal);
			}
			lastSev = sev.Str;
		};
	}
	private static void Make_SetRainTimer(Happen ha, ArgSet args) {
		if (args.Count < 1) {
			__NotifyArgsMissing(Make_SetRainTimer, "value");
			return;
		}
		Arg target = args[0];
		//int.TryParse(args.AtOr(0, "0").Str, out var target);
		ha.On_Init += (w) => {
			w.rainCycle.timer = target.SecAsFrames;
		};
	}
	private static void Make_SetKarma(Happen ha, ArgSet args) {
		if (args.Count < 1) {
			__NotifyArgsMissing(Make_SetKarma, "level");
			return;
		}

		ha.On_Init += (w) => {
			DeathPersistentSaveData? dpsd = w.game?.GetStorySession?.saveState?.deathPersistentSaveData;
			if (dpsd is null || w.game is null) return;
			Arg ts = args[0];
			int karma = dpsd.karma;
			if (ts.Name is "add" or "+") karma += ts.I32;
			else if (ts.Name is "sub" or "substract" or "-") karma -= ts.I32;
			else karma = ts.I32 - 1;
			karma = Clamp(karma, 0, 9);
			dpsd.karma = karma;
			foreach (RoomCamera cam in w.game.cameras) { cam?.hud.karmaMeter?.UpdateGraphic(); }
		};
	}
	private static void Make_SetMaxKarma(Happen ha, ArgSet args) {
		ha.On_Init += (w) => {
			DeathPersistentSaveData? dpsd = w.game?.GetStorySession?.saveState?.deathPersistentSaveData;
			if (dpsd is null || w.game is null) return;
			Arg ts = args.AtOr(0, 0);
			int cap = dpsd.karmaCap;
			if (ts.Name is "add" or "+") cap += ts.I32;
			else if (ts.Name is "sub" or "-") cap -= ts.I32;
			else cap = ts.I32 - 1;
			cap = Clamp(cap, 4, 9);
			dpsd.karma = cap;
			foreach (RoomCamera? cam in w.game.cameras) { cam?.hud.karmaMeter?.UpdateGraphic(); }
		};
	}
	private static void Make_Playergrav(Happen ha, ArgSet args) {
		Arg frac = args.AtOr(0, 0.5f);
		//float.TryParse(args.AtOr(0, "0.5"), out var frac);
		ha.On_RealUpdate += (room) => {
			for (int i = 0; i < room.updateList.Count; i++) {
				UAD? uad = room.updateList[i];
				if (uad is Player p) {
					foreach (BodyChunk? bc in p.bodyChunks) {
						bc.vel.y += frac.F32;
					}
				}
			}
		};
	}
	private static void Make_Rumble(Happen ha, ArgSet args) {
		Arg intensity = args["int", "intensity"] ?? 1f,
			shake = args["shake"] ?? 0.5f;
		ha.On_RealUpdate += (room) => {
			room.ScreenMovement(null, RND.insideUnitCircle * intensity.F32, shake.F32);
		};
	}
	private static void Make_ChangePalette(Happen ha, ArgSet args) {
		if (args.Count < 1) {
			__NotifyArgsMissing(Make_ChangePalette, "pal");
			return;
		}
		//todo: support for fade palettes? make sure they dont fuck with rain
		Arg palA = args[0];//["palA", "A", "1"];
		string[]? lastRoomPerCam = null;
		ha.On_RealUpdate += (rm) => {
			if (lastRoomPerCam is null) return;
			for (int i = 0; i < lastRoomPerCam.Length; i++) {
				RoomCamera? cam = rm.game.cameras[i];
				if (cam.room != rm || !rm.BeingViewed || cam.AboutToSwitchRoom) continue;
				if (cam.room.abstractRoom.name != lastRoomPerCam[i]) {
					if (palA is not null) {
						cam.ChangeMainPalette(palA.I32);
						__logger.DbgVerbose($"changing palette to {palA.I32}");
					}
				}
			}
		};
		ha.On_CoreUpdate += (rwg) => {
			if (lastRoomPerCam is null) lastRoomPerCam = new string[rwg.cameras.Length];
			else for (int i = 0; i < rwg.cameras.Length; i++) {
					lastRoomPerCam[i] = rwg.cameras[i].room.abstractRoom.name;
				}
		};
	}
	private static void Make_SetVar(Happen ha, ArgSet args) {
		if (args.Count < 2) {
			__NotifyArgsMissing(Make_SetVar, "varname", "value");
			return;
		}
		Arg argn = args[0],
			argv = args[1],
			continuous = args.AtOr(2, false),
			forceType = args["dt", "datatype", "format"] ?? nameof(ArgType.STRING),
			target = VarRegistry.GetVar(argn.Str, __CurrentSaveslot ?? 0, __CurrentCharacter ?? __slugnameNotFound)
			;
		forceType.GetEnum(out ArgType datatype);
		string? dt_last_str = forceType.Str;
		ha.On_Init += (_) => {
			Assign(argv, target, datatype);
		};
		ha.On_CoreUpdate += (_) => {
			if (dt_last_str != forceType.Str) {
				forceType.GetEnum(out datatype);
				__logger.DbgVerbose($"Updating DT preference to {datatype}");
			}
			dt_last_str = forceType.Str;
			if (continuous.Bool) {
				Assign(argv, target, datatype);
			}
		};
	}
	#endregion
	#region metafunctions
	internal static readonly TXT.Regex __FMT_Is = new("\\$FMT\\((.*?)\\)");
	internal static readonly TXT.Regex __FMT_Split = new("{.+?}");
	internal static readonly TXT.Regex __FMT_Match = new("(?<={).+?(?=})");
	internal static void __RegisterBuiltinMetafun() {
		AddNamedMetafun(new[] { "FMT", "FORMAT" }, MMake_FMT);
		AddNamedMetafun(new[] { "FILEREAD", "FILE" }, MMAke_FileRead);
		AddNamedMetafun(new[] { "WWW", "WEBREQUEST" }, MMake_WWW);
		AddNamedMetafun(new[] { "CURRENTROOM", "VIEWEDROOM" }, MMake_CurrentRoom);
		AddNamedMetafun(new[] { "SCREENRES", "RESOLUTION" }, MMake_ScreenRes);
		AddNamedMetafun(new[] { "OWNSAPP", "OWNSGAME" }, MMake_AppFound);
		//todo: document metafuncs below:
		AddNamedMetafun(new[] { "LISTDIRECTORY", "ASSETDIR" }, MMake_ListDirectory);
		AddNamedMetafun(new[] { "RESOLVEFILEPATH", "ASSETPATH" }, MMake_ResolveFilepath);
		//do not document:
		AddNamedMetafun(new[] { "FILEREADWRITE", "TEXTIO" }, MMake_FileReadWrite);
	}
	private static IArgPayload? MMake_ListDirectory(string text, int ss, SlugcatStats.Name ch) {
		try {
			return Arg.Coerce(AssetManager.ListDirectory(text).Stitch((x, y) => $"{x}:{y}"));
		}
		catch {
			__logger.LogError($"INVALID assetpath {text}");
			return new Arg("");
		}
	}
	private static IArgPayload? MMake_ResolveFilepath(string text, int ss, SlugcatStats.Name ch) {
		try {
			return Arg.Coerce(AssetManager.ResolveFilePath(text));
		}
		catch {
			__logger.LogError($"INVALID assetpath {text}");
			return new Arg("");
		}
	}
	private static IArgPayload? MMake_AppFound(string text, int ss, SlugcatStats.Name ch) {
		uint.TryParse(text, out var id);
		var resr = Steamworks.SteamApps.BIsSubscribedApp(new(id));
		return Arg.Coerce(resr);
	}
	private static IArgPayload? MMake_ScreenRes(string text, int ss, SlugcatStats.Name ch) {
		return new ByCallbackGetOnly() {
			getVec = () => {
				Resolution res = UnityEngine.Screen.currentResolution;
				return new Vector4(res.width, res.height, res.refreshRate, 0f);
			},
			getStr = () => {
				Resolution res = UnityEngine.Screen.currentResolution;
				return $"{res.width}x{res.height}@{res.refreshRate}";
			}

		};
	}
	private static IArgPayload? MMake_CurrentRoom(string text, int ss, SlugcatStats.Name ch) {
		if (!int.TryParse(
			text,
			out int camnum)) camnum = 1;
		static AbstractRoom? findAbsRoom(int cam) => inst.RW?
				.processManager.FindSubProcess<RainWorldGame>()?
				.cameras.AtOr(cam - 1, null)?
				.room?.abstractRoom;

		Vector2 nosize = new(-1, -1);
		return new ByCallbackGetOnly() {
			getStr = ()
				=> findAbsRoom(camnum)?.name
				?? string.Empty,
			getI32 = () => findAbsRoom(camnum)?.index ?? -1,
			getVec = () => findAbsRoom(camnum)?.size.ToVector2() * 20f ?? nosize,
			getBool = () => false,
		};
	}

	private static IArgPayload? MMake_WWW(string text, int ss, SlugcatStats.Name ch) {
		WWW? www = new WWW(text);
		string? failed = null;
		return new ByCallbackGetOnly() {
			getStr = () => {
				if (failed is not null) return $"ERROR:{failed}";
				try {
					return www.isDone ? www.text : "[LOADING]";
				}
				catch (Exception ex) {
					failed = ex.Message;
					return failed;
				}
			}
		};
	}
	private static IArgPayload? MMAke_FileRead(string text, int ss, SlugcatStats.Name ch) {
		IO.FileInfo fi = new(text);
		DateTime? lw = null;
		string? contents = null;
		return new ByCallbackGetOnly() {
			getStr = () => {
				fi.Refresh();
				if (fi.Exists) {
					if (lw != fi.LastWriteTimeUtc) {
						using IO.StreamReader? sr = fi.OpenText();
						contents = sr?.ReadToEnd();
					}
					lw = fi.LastAccessTimeUtc;
				}
				return contents ?? string.Empty;
			}
		};
	}
	private static IArgPayload? MMake_FileReadWrite(string text, int ss, SlugcatStats.Name ch) {
		__logger.LogWarning($"CAUTION: {nameof(MMake_FileReadWrite)} DOES NO SAFETY CHECKS! Atmo developers are not responsible for any accidental damage by write");
		IO.FileInfo file = new(text);
		DateTime? lwt = null;//file.LastWriteTimeUtc;
		Arg pl = new(string.Empty);
		void UpdateFile() {
			file.Refresh();
			try {
				if (file.Exists) {
					if (file.LastWriteTimeUtc == lwt) {
						return;
					}
					lwt = file.LastWriteTimeUtc;
					using IO.StreamReader sr = file.OpenText();
					pl.Str = sr.ReadToEnd();
				}
			}
			catch (IO.IOException ex) {
				__logger.LogError($"Could not sync with file {file.FullName}: {ex}");
			}
		}
		string ReadFromFile() {
			UpdateFile();
			return pl.Str;
		}
		void WriteToFile(string val) {
			pl.Str = val;
			using IO.StreamWriter sw = file.CreateText();
			sw.Write(val);
			sw.Flush();
		}
		return new ByCallback() {
			prop_Str = new(ReadFromFile, WriteToFile)
		};
	}
	private static IArgPayload? MMake_FMT(string text, int ss, SlugcatStats.Name ch) {
		string[] bits = __FMT_Split.Split(text);
		TXT.MatchCollection names = __FMT_Match.Matches(text);
		Arg[] variables = new Arg[names.Count];
		for (int i = 0; i < names.Count; i++) {
			variables[i] = GetVar(names[i].Value, ss, ch);
		}
		int ind = 0;
		string format = bits.Stitch((x, y) => $"{x}{{{ind++}}}{y}");
		object[] getStrs() {
			return variables.Select(x => x.Str).ToArray();
		}
		return new ByCallbackGetOnly() {
			getStr = () => string.Format(format, getStrs())
		};

	}
	#endregion
	private static void __NotifyArgsMissing(Delegate source, params string[] args) {
		__logger.LogWarning($"{nameof(HappenBuilding)}.{source.Method.Name}: Missing argument(s): {args.Stitch()}");
	}
}
