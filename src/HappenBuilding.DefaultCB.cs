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
            //inst.Plog.LogWarning($"{acname}({(args.Length == 0 ? string.Empty : args.Aggregate(Utils.JoinWithComma))})");
            switch (action.Key.ToLower())
            {
                //multiplies player gravity. args: frac
                case "playergrav":
                case "playergravity":
                    {
                        float.TryParse(action.Value.AtOr(0, "0.5"), out var frac);
                        ha.On_RealUpdate += (room) =>
                        {
                            for (int i = 0; i < room.updateList.Count; i++)
                            {
                                UAD uad = room.updateList[i];
                                if (uad is Player p) p.gravity = frac;
                            }
                        };
                    }
                    break;
                //plays a sound. args: sound name, cooldown/cd, volume/vol, pitch, pan
                case "sound":
                case "playsound":
                    {
                        inst.Plog.LogWarning("making sound!!!");
                        SoundID soundid = RWCustom.Custom.ParseEnum<SoundID>(args[0]);
                        //var me = Guid.NewGuid();
                        //if (!TryParseEnum(args.AtOr(0, nameof(SoundID.HUD_Karma_Reinforce_Bump)),
                            //out SoundID soundid)) break;
                        //SoundID sid = Enum.Parse(typeof(SoundID), )
                        //int.TryParse(args.AtOr(1, "-1"), out var cooldown);
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
                        int ctr = 0;
                        //Box<int> ctr = new(0);
                        //Dictionary<string, int> counters = new();
                        ha.On_RealUpdate += (room) =>
                        {
                            //inst.Plog.LogWarning($"$Boing {ctr.val}");
                            for (int i = 0; i < room.updateList.Count; i++)
                            {
                                
                                if (room.updateList[i] is Player p && ctr == 0)
                                {
                                    var em = room.PlaySound(soundid, p.firstChunk, false, vol, pitch);
                                    ctr = cooldown;
                                    return;
                                }
                            }
                        };
                        ha.On_CoreUpdate += (rwg) =>
                        {
                            if (ctr > 0) ctr--;
                            //if (ctr.val == 0) inst.Plog.LogWarning("Boing");
                        };

                    }
                    //case "playsoundnative":
                    break;
            }
        }
    }
}
