using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using static Atmo.Atmod;
using static Atmo.HappenTrigger;
using static Atmo.HappenBuilding;
using static Atmo.API;
using static Atmo.Utils;

using UAD = UpdatableAndDeletable;
using URand = UnityEngine.Random;
using TXT = System.Text.RegularExpressions;

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
                    {
                        Make_Playergrav(ha, args);
                    }
                    break;
                case "sound":
                case "playsound":
                    {
                        Make_Sound(ha, args);
                    }
                    break;
                case "rumble":
                case "screenshake":
                    {
                        Make_Rumble(ha, args);
                    }
                    break;
            }
        }
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
        if (!TryParseEnum(args[0], out SoundID soundid))
        {
            return;
        }

        int cooldown = -1;
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
            }
        }
        int counter = 0;
        ha.On_RealUpdate += (room) =>
        {
            if (counter != 0) return;
            for (int i = 0; i < room.updateList.Count; i++)
            {
                if (room.updateList[i] is Player p)
                {
                    var em = room.PlaySound(soundid, p.firstChunk, false, vol, pitch);
                    counter = cooldown;
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
            room.ScreenMovement(null, URand.insideUnitCircle * intensity, shake);
        };
    }
}
