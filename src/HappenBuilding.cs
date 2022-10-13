using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using static Atmo.Atmod;
using static Atmo.HappenTrigger;
using static Atmo.HappenBuilding;
using static Atmo.API;

using URand = UnityEngine.Random;

namespace Atmo;
/// <summary>
/// Manages happens' initialization and builtin behaviours.
/// </summary>
internal static class HappenBuilding
{
    internal static void NewEvent(Happen ha)
    {
        AddDefaultCallbacks(ha);
        if (MNH_invl is null) return;
        foreach (var cb in MNH_invl)
        {
            try
            {
                cb?.Invoke(ha);
            }
            catch (Exception ex)
            {
                inst.Plog.LogError(
                    $"Happenbuild: NewEvent:" +
                    $"Error invoking event factory {cb}//{cb?.Method.Name} for {ha}:" +
                    $"\n{ex}");
            }
        }
        //API_MakeNewHappen?.Invoke(ha);
    }
    internal static void AddDefaultCallbacks(Happen ha)
    {
        //todo: add default cases
        //inst.Plog.LogWarning("preblam");
        ha.On_Init += (w) => { inst.Plog.LogWarning($"Init! {ha}"); };
        ha.On_AbstUpdate += (absr, t) => { inst.Plog.LogWarning($"absup {absr.name}, {t} ticks"); };
        ha.On_RealUpdate += (rm) =>
        {
            if (URand.value < 0.03f) throw new Exception("Fuck you");
        };
    }
    /// <summary>
    /// Creates a new trigger with given ID, arguments using provided <see cref="RainWorldGame"/>.
    /// </summary>
    /// <param name="id">Name or ID</param>
    /// <param name="args">Optional arguments</param>
    /// <param name="rwg">game instance</param>
    /// <returns>Resulting trigger; an <see cref="Always"/> if something went wrong.</returns>
    internal static HappenTrigger CreateTrigger(
        string id,
        string[] args,
        RainWorldGame rwg)
    {
#warning untested
        HappenTrigger? res = null;
        res = DefaultTrigger(id, args, rwg);

        if (MNT_invl is null) goto finish;
        foreach (Create_RawTriggerFactory? cb in MNT_invl)
        {
            if (res is not null) break;
            try
            {
                res ??= cb?.Invoke(id, args, rwg);
            }
            catch (Exception ex)
            {
                inst.Plog.LogError(
                    $"Happenbuild: CreateTrigger: Error invoking trigger factory " +
                    $"{cb}//{cb?.Method.Name} for {id}({args.Aggregate(Utils.JoinWithComma)}):" +
                    $"\n{ex}");
            }
        }
    finish:
        if (res is null)
        {
            inst.Plog.LogWarning($"Failed to create a trigger! {id}, args: {args.Aggregate(Utils.JoinWithComma)}. Replacing with a stub");
            res = new Always();
        }
        return res;
    }

    private static HappenTrigger? DefaultTrigger(string id, string[] args, RainWorldGame rwg)
    {
        HappenTrigger? res = null;
        switch (id)
        {
            case "untilrain":
            case "beforerain":
                {
                    int.TryParse(args.AtOr(0, "0"), out var delay);
                    res = new BeforeRain(rwg, delay);
                }
                break;
            case "afterrain":
                {
                    int.TryParse(args.AtOr(0, "0"), out var delay);
                    res = new AfterRain(rwg, delay);
                }
                break;
            case "everyx":
            case "every":
                {
                    var def = "40";
                    int.TryParse(args.AtOr(0, "40"), out var period);
                    res = new EveryX(period);
                }
                break;
            case "maybe":
            case "chance":
                {
                    float.TryParse(args.AtOr(0, "0.5"), out var ch);
                    res = new Maybe(ch);
                }
                break;
            case "flicker":
                {
                    int[] argsp = new int[4];
                    for (int i = 0; i < 4; i++)
                    {
                        int.TryParse(args.AtOr(i, "300"), out argsp[i]);
                    }
                    bool startOn = trueStrings.Contains(args.AtOr(4, "1").ToLower());
                    res = new Flicker(argsp[0], argsp[1], argsp[2], argsp[3], startOn);
                }
                break;
            case "karma":
                res = new OnKarma(rwg, args);
                break;
        }

        return res;
    }
}
