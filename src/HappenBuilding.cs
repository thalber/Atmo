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
internal static partial class HappenBuilding
{
    internal static void NewEvent(Happen ha)
    {
        try
        {
            AddDefaultCallbacks(ha);
        }
        catch (Exception ex)
        {
            inst.Plog.LogError($"HappenBuild: NewEvent:" +
                $"Error adding default callbacks to {ha}:" +
                $"\n{ex}");
        }
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
        RainWorldGame rwg,
        Happen owner)
    {
#warning untested
        HappenTrigger? res = null;
        res = DefaultTrigger(id, args, rwg, owner);

        if (MNT_invl is null) goto finish;
        foreach (Create_RawTriggerFactory? cb in MNT_invl)
        {
            if (res is not null) break;
            try
            {
                res ??= cb?.Invoke(id, args, rwg, owner);
            }
            catch (Exception ex)
            {
                inst.Plog.LogError(
                    $"Happenbuild: CreateTrigger: Error invoking trigger factory " +
                    $"{cb}//{cb?.Method.Name} for {id}({(args.Length == 0 ? string.Empty : args.Aggregate(Utils.JoinWithComma))}):" +
                    $"\n{ex}");
            }
        }
    finish:
        if (res is null)
        {
            inst.Plog.LogWarning($"Failed to create a trigger! {id}, args: {(args.Length == 0 ? string.Empty : args.Aggregate(Utils.JoinWithComma))}. Replacing with a stub");
            res = new Always(owner);
        }
        return res;
    }

    private static HappenTrigger? DefaultTrigger(string id, string[] args, RainWorldGame rwg, Happen ha)
    {
        HappenTrigger? res = null;
        switch (id.ToLower())
        {
            case "always":
                res = new Always(ha);
                break;
            case "untilrain":
            case "beforerain":
                {
                    float.TryParse(args.AtOr(0, "0"), out var delay);
                    res = new BeforeRain(rwg, ha, (int)(delay * 40f));
                }
                break;
            case "afterrain":
                {
                    float.TryParse(args.AtOr(0, "0"), out var delay);
                    res = new AfterRain(rwg, ha, (int)(delay * 40f));
                }
                break;
            case "everyx":
            case "every":
                {
                    float.TryParse(args.AtOr(0, "4"), out var period);
                    res = new EveryX((int)(period * 40f), ha);
                }
                break;
            case "maybe":
            case "chance":
                {
                    float.TryParse(args.AtOr(0, "0.5"), out var ch);
                    res = new Maybe(ch, ha);
                }
                break;
            case "flicker":
                {
                    int[] argsp = new int[4];
                    for (int i = 0; i < 4; i++)
                    {
                        float.TryParse(args.AtOr(i, "5"), out var pres);
                        argsp[i] = (int)(pres * 40f);
                    }
                    bool startOn = trueStrings.Contains(args.AtOr(4, "1").ToLower());
                    res = new Flicker(argsp[0], argsp[1], argsp[2], argsp[3], startOn);
                }
                break;
            case "karma":
                res = new OnKarma(rwg, args);
                break;
            case "visited":
            case "playervisited":
                res = new AfterVisit(rwg, args);
                break;
            case "fry":
                {
                    float.TryParse(args.AtOr(0, "5"), out var lim);
                    float.TryParse(args.AtOr(1, "10"), out var cd);
                    res = new Fry((int)(lim * 40f), (int)(cd * 40f));
                }
                break;
            case "after":
                {
                    if (args.Length < 2) break;
                    string other = args[0];
                    int.TryParse(args[1], out var delay);
                    res = new AfterOther(ha, other, delay);
                }
                break;

        }

        return res;
    }
}
