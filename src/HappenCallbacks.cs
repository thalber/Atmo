using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using static Atmo.Atmod;
using static Atmo.HappenTrigger;

namespace Atmo;
public static class HappenCallbacks
{
    #region dels
    /// <summary>
    /// delegate for calling by happens on abstract updates
    /// </summary>
    /// <param name="absroom">absroom</param>
    /// <param name="time">absupdate step</param>
    public delegate void AbstractUpdate(AbstractRoom absroom, int time);
    /// <summary>
    /// delegate for being called by happens on realized updates
    /// </summary>
    /// <param name="room">room</param>
    /// <param name="eu"></param>
    public delegate void RealizedUpdate(Room room);
    /// <summary>
    /// Delegate for being called on first abstract update
    /// </summary>
    /// <param name="world"></param>
    public delegate void Init(World world);
    /// <summary>
    /// Delegate for being called on core update
    /// </summary>
    /// <param name="rwg"></param>
    public delegate void CoreUpdate(RainWorldGame rwg);
    /// <summary>
    /// callback for adding callbacks to happens.
    /// </summary>
    /// <param name="ha"></param>
    public delegate void Create_AddCallbacks(Happen ha);
    /// <summary>
    /// Callback for adding triggers to happens.
    /// </summary>
    /// <param name="name">Trigger name (id)</param>
    /// <param name="args">optional arguments.</param>
    /// <param name="rwg">Game instance.</param>
    /// <returns></returns>
    public delegate HappenTrigger Create_MakeTrigger(string name, string[] args, RainWorldGame rwg);
    //public delegate IEnumerable<HappenTrigger> Create_AddTriggers(Happen ha);
    #endregion

    internal static void NewEvent(Happen ha)
    {
        //add callbacks
        GetDefaultCallbacks(in ha, out var au, out var ru, out var oi);
        ha.On_AbstUpdate += au;
        ha.On_RealUpdate += ru;
        ha.On_Init += oi;
        RegisterNewHappen?.Invoke(ha);
    }
    internal static void GetDefaultCallbacks(in Happen ha, out AbstractUpdate au, out RealizedUpdate ru, out Init oi)
    {
        au = (absr, t) =>
        {
            inst.Plog.LogWarning($"Test absup in {absr.name}! time passed: {t}");
        };
        ru = (rm) =>
        {
            if (!rm.BeingViewed) return;
            Player? p = rm.updateList.FirstOrDefault(x => x is Player) as Player;
            if (p is null) return;
            p.gravity = 0.5f;
        };
        oi = (w) =>
        {
            inst.Plog.LogWarning("Init!");
        };
        //todo: add default cases
    }
    /// <summary>
    /// Creates a new trigger with given ID, arguments using provided <see cref="RainWorldGame"/>.
    /// </summary>
    /// <param name="id">Name or ID</param>
    /// <param name="args">Optional arguments</param>
    /// <param name="rwg">game instance</param>
    /// <returns>Resulting trigger; an <see cref="Always"/> if something went wrong.</returns>
    internal static HappenTrigger CreateTrigger(string id, string[] args, RainWorldGame rwg)
    {
#warning untested
        HappenTrigger res = null;
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
        //if (RegisterMakeTrigger is null) goto finish;
        foreach (Create_MakeTrigger inv in RegisterMakeTrigger?.GetInvocationList() ?? new Create_MakeTrigger[0])
        {
            if (res is not null) break;
            res ??= inv(id, args, rwg);
        }
        //finish:
        res ??= new Always();
        return res;
    }
    /// <summary>
    /// Subscribe to this to attach your custom callbacks to newly created happen objects.
    /// </summary>
    public static event Create_AddCallbacks? RegisterNewHappen;
    /// <summary>
    /// Subscribe to this to dispense your custom triggers.
    /// </summary>
    public static event Create_MakeTrigger? RegisterMakeTrigger;
    //todo: move to Atmod, add convenience shorthands
}
