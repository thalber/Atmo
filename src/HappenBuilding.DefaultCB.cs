using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using static Atmo.HappenBuilding;
using static Atmo.HappenTrigger;
using static UnityEngine.Mathf;
using static Atmo.Utils;
using static Atmo.Atmod;
using static Atmo.API;

using TXT = System.Text.RegularExpressions;
using UAD = UpdatableAndDeletable;
using RND = UnityEngine.Random;
using LOG = BepInEx.Logging;

namespace Atmo;

internal static partial class HappenBuilding
{
    internal static void AddDefaultCallbacks(Happen ha)
    {
        foreach (var action in ha.actions)
        {
            string acname = action.Key;
            string[] args = action.Value;
            switch (action.Key.ToLower())
            {
                case "playergrav":
                case "playergravity":
                    Make_Playergrav(ha, args);
                    break;
                case "sound":
                case "playsound":
                    Make_Sound(ha, args);
                    break;
                case "rumble":
                case "screenshake":
                    Make_Rumble(ha, args);
                    break;
                case "karma":
                case "setkarma":
                    Make_SetKarma(ha, args);
                    break;
                case "karmacap":
                case "maxkarma":
                case "setmaxkarma":
                    Make_SetMaxKarma(ha, args);
                    break;
                case "settimer":
                case "setraintimer":
                    Make_SetRainTimer(ha, args);
                    break;
                case "log":
                case "message":
                    Make_LogCall(ha, args);
                    break;
                case "mark":
                case "givemark":
                    Make_GiveMark(ha, args);
                    break;
                case "music":
                case "playmusic":
                    Make_PlayMusic(ha, args);
                    break;
            }
        }
    }
    #region actions
#warning contributor notice: actions
    //add your custom actions in methods here
    //Use methods with the same structure. Don't forget to also add them to the switch above.
    //Do not remove the warning directive.
    private static void Make_PlayMusic(Happen ha, string[] args)
    {
#warning unfinished
        if (args.Length == 0) { inst.Plog.LogWarning($"Happen {ha.name}: music action: " +
            $"No arguments provided to Music action! won't do anything"); return; }
        string songname = args[0];
        float
            prio = 0.5f,
            maxthreat = 1f,
            vol = 0.3f;
        int
            roomsrange = -1,
            rest = 5;

        for (int i = 1; i < args.Length; i++)
        {
            var spl = args[i].Split('=');
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

        MusicEvent mev = new();
        

        ha.On_Init += (w) =>
        {
            w.game.manager.musicPlayer?.GameRequestsSong(mev);
        };

    }
    private static void Make_GiveMark(Happen ha, string[] args)
    {
        bool enabled = true;
        if (falseStrings.Contains(args.AtOr(0, "true").ToLower())) enabled = false;
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
        LOG.LogLevel sev = LOG.LogLevel.Debug;
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
        string result = string.Concat(output.ToArray());
        ha.On_Init += (w) =>
        {
            inst.Plog.Log(sev, result);
        };
    }
    private static void Make_SetRainTimer(Happen ha, string[] args)
    {
        int.TryParse(args.AtOr(0, "0"), out int target);
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
            int karma = dpsd.karma;
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
            int cap = dpsd.karmaCap;
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
            for (int i = 0; i < room.updateList.Count; i++)
            {
                UAD uad = room.updateList[i];
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

        int cooldown = -1,
            limit = int.MaxValue;
        float
            vol = 1f,
            pitch = 1f,
            pan = 1f;
        for (int i = 1; i < args.Length; i++)
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
        int counter = 0;
        ha.On_RealUpdate += (room) =>
        {
            if (counter != 0) return;
            if (limit < 1) return;
            for (int i = 0; i < room.updateList.Count; i++)
            {
                if (room.updateList[i] is Player p)
                {
                    var em = room.PlaySound(soundid, p.firstChunk, false, vol, pitch);
                    counter = cooldown;
                    limit++;
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

        for (int i = 0; i < args.Length; i++)
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
        int counter = 0;
        int active = duration;
        ha.On_RealUpdate += (room) =>
        {
            //if (active != 0)
            room.ScreenMovement(null, RND.insideUnitCircle * intensity, shake);
        };
    }
    #endregion
}
