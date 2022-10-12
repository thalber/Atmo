using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using static Atmo.Atmod;
using static Atmo.HappenTrigger;

namespace Atmo;
public static class API
{
    #region fields
    internal readonly static List<string> takenActionNames = new();
    internal readonly static List<string> takenTriggerNames = new();
    #endregion
    #region dels
    /// <summary>
    /// delegate for calling by happens on abstract updates
    /// </summary>
    /// <param name="absroom">absroom</param>
    /// <param name="time">absupdate step</param>
    public delegate void lc_AbstractUpdate(AbstractRoom absroom, int time);
    /// <summary>
    /// delegate for being called by happens on realized updates
    /// </summary>
    /// <param name="room">room</param>
    /// <param name="eu"></param>
    public delegate void lc_RealizedUpdate(Room room);
    /// <summary>
    /// Delegate for being called on first abstract update
    /// </summary>
    /// <param name="world"></param>
    public delegate void lc_Init(World world);
    /// <summary>
    /// Delegate for being called on core update
    /// </summary>
    /// <param name="rwg"></param>
    public delegate void lc_CoreUpdate(RainWorldGame rwg);
    /// <summary>
    /// callback for attaching custom behaviour to happens.
    /// </summary>
    /// <param name="ha"></param>
    public delegate void Create_HappenBuilder(Happen ha);
    /// <summary>
    /// Delegate for including custom triggers.
    /// </summary>
    /// <param name="name">Trigger name (id)</param>
    /// <param name="args">optional arguments.</param>
    /// <param name="rwg">Game instance.</param>
    /// <returns>Your child of <see cref="HappenTrigger"/> if the name is yours; null if not.</returns>
    public delegate HappenTrigger? Create_RawTriggerFactory(string name, string[] args, RainWorldGame rwg);
    /// <summary>
    /// Delegate for registering new triggers.
    /// </summary>
    /// <param name="args"></param>
    /// <param name="rwg"></param>
    public delegate HappenTrigger? Create_SafeTriggerFactory(string[] args, RainWorldGame rwg);
    #endregion

    internal static void NewEvent(Happen ha)
    {
        //add callbacks
        AddDefaultCallbacks(ha);
        //ha.On_AbstUpdate += au;
        //ha.On_RealUpdate += ru;
        //ha.On_Init += oi;
        API_MakeNewHappen?.Invoke(ha);
    }
    internal static void AddDefaultCallbacks(Happen ha)
    {
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
        foreach (Create_RawTriggerFactory inv in API_MakeNewTrigger?.GetInvocationList() ?? new Create_RawTriggerFactory[0])
        {
            if (res is not null) break;
            res ??= inv(id, args, rwg);
        }
        //finish:
        res ??= new Always();
        return res;
    }
    #region API
    public static bool API_AddCallbacksOnAction(string action, lc_AbstractUpdate? au = null, lc_RealizedUpdate? ru = null, lc_Init? oi = null, lc_CoreUpdate? cu = null)
    {
        if (takenActionNames.Contains(action)) return false;
        API_MakeNewHappen += (ha) =>
        {
            if (ha.cfg.actions.Contains(action))
            {
                ha.On_AbstUpdate += au;
                ha.On_RealUpdate += ru;
                ha.On_Init += oi;
                ha.On_CoreUpdate += cu;
            }
        };
        takenActionNames.Add(action);
        return true;
    }
    public static bool API_MakeTriggerOnName(string name, Create_SafeTriggerFactory fac)
    {
        if (takenTriggerNames.Contains(name)) return false;
        API_MakeNewTrigger += (n, args, rwg) =>
        {
            if (n == name) return fac(args, rwg);
            return null;
        };
        return true;
    }
    /// <summary>
    /// Subscribe to this to attach your custom callbacks to newly created happen objects.
    /// </summary>
    public static event Create_HappenBuilder? API_MakeNewHappen;
    /// <summary>
    /// Subscribe to this to dispense your custom triggers.
    /// </summary>
    public static event Create_RawTriggerFactory? API_MakeNewTrigger;
    //todo: move to Atmod, add convenience shorthands
    #endregion
}
