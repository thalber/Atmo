using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using static Atmo.Atmod;
using static Atmo.HappenTrigger;
using static Atmo.HappenBuilding;

namespace Atmo;
public static class API
{
    #region fields
    internal readonly static Dictionary<string, Create_RawHappenBuilder> namedActions = new();
    internal readonly static Dictionary<string, Create_RawTriggerFactory> namedTriggers = new();
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
    /// callback for attaching custom behaviour to happens. Can be directly attached to <see cref="EV_MakeNewHappen"/>
    /// </summary>
    /// <param name="ha"></param>
    public delegate void Create_RawHappenBuilder(Happen ha);
    /// <summary>
    /// Delegate for registering named callbacks.
    /// Used by <see cref="AddNamedAction(string, Create_NamedHappenBuilder, bool)"/>.
    /// </summary>
    /// <param name="ha"></param>
    /// <param name="args"></param>
    public delegate void Create_NamedHappenBuilder(Happen ha, string[] args);
    /// <summary>
    /// Delegate for including custom triggers. Can be directly attached to <see cref="EV_MakeNewTrigger"/>.
    /// </summary>
    /// <param name="name">Trigger name (id)</param>
    /// <param name="args">optional arguments.</param>
    /// <param name="rwg">Game instance.</param>
    /// <returns>Child of <see cref="HappenTrigger"/> if subscriber wishes to claim the trigger; null if not.</returns>
    public delegate HappenTrigger? Create_RawTriggerFactory(string name, string[] args, RainWorldGame rwg);
    /// <summary>
    /// Delegate for registering named triggers.
    /// </summary>
    /// <param name="args"></param>
    /// <param name="rwg"></param>
    public delegate HappenTrigger? Create_NamedTriggerFactory(string[] args, RainWorldGame rwg);
    #endregion
    #region API proper
    /// <summary>
    /// Registers a named action. Up to one callback for every lifecycle event.
    /// </summary>
    /// <param name="action">Action name. Case insensitive.</param>
    /// <param name="au">Abstract update callback.</param>
    /// <param name="ru">Realized update callback.</param>
    /// <param name="oi">Init callback.</param>
    /// <param name="cu">Core update callback.</param>
    /// <param name="ignoreCase">Whether action name matching should be case sensitive.</param>
    /// <returns>True if successfully added; false if name already taken.</returns>
    public static bool AddNamedAction(
        string action,
        lc_AbstractUpdate? au = null,
        lc_RealizedUpdate? ru = null,
        lc_Init? oi = null,
        lc_CoreUpdate? cu = null,
        bool ignoreCase = true)
    {
        //if (ignoreCase) action = action.ToLower();
        if (namedActions.ContainsKey(action)) return false;
        //API_MakeNewHappen += 
        Create_RawHappenBuilder newCb = (ha) =>
        {
            if (ha.actions.ContainsKey( ignoreCase ? action.ToLower() : action))
            {
                ha.On_AbstUpdate += au;
                ha.On_RealUpdate += ru;
                ha.On_Init += oi;
                ha.On_CoreUpdate += cu;
            }
        };
        namedActions.Add(action, newCb);
        EV_MakeNewHappen += newCb;
        return true;
    }
    /// <summary>
    /// Registers a named action. Uses a <see cref="Create_RawHappenBuilder"/> to add any amount of callbacks to events. 
    /// </summary>
    /// <param name="action">Action name. Case insensitive.</param>
    /// <param name="builder">User builder callback.</param>
    /// <param name="ignoreCase">Whether action name matching should be case sensitive.</param>
    /// <returns>true if successfully attached, false if name already taken.</returns>
    public static bool AddNamedAction(
        string action,
        Create_NamedHappenBuilder builder,
        bool ignoreCase = true)
    {
        //action = action.ToLower();
        if (namedTriggers.ContainsKey(action)) return false;
        Create_RawHappenBuilder newCb =
            (ha) => { if (ha.actions.ContainsKey(ignoreCase ? action.ToLower() : action)) builder?.Invoke(ha, ha.actions[action]); };
        namedActions.Add(action, newCb);
        EV_MakeNewHappen += newCb;
        return true;
    }
    /// <summary>
    /// Removes a named callback.
    /// </summary>
    /// <param name="action"></param>
    public static void RemoveNamedAction(string action)
    {
        if (!namedActions.ContainsKey(action)) return;
        EV_MakeNewHappen -= namedActions[action];
        namedActions.Remove(action);
    }
    /// <summary>
    /// Registers a named trigger.
    /// </summary>
    /// <param name="name">Trigger name.</param>
    /// <param name="fac">User trigger factory callback.</param>
    /// <param name="ignoreCase">Whether trigger name should be case sensitive.</param>
    /// <returns>true if successfully attached, false if name already taken.</returns>
    public static bool AddNamedTrigger(
        string name,
        Create_NamedTriggerFactory fac,
        bool ignoreCase = true)
    {
        //name = name.ToLower();
        if (namedTriggers.ContainsKey(ignoreCase ? name.ToLower() : name)) return false;
        Create_RawTriggerFactory newCb = (n, args, rwg) =>
        {
            if (n == name) return fac(args, rwg);
            return null;
        };
        namedTriggers.Add(name, newCb);
        EV_MakeNewTrigger += newCb;
        return true;
    }

    public static void RemoveNamedCallback(string name)
    {
        if (!namedTriggers.ContainsKey(name)) return;
        EV_MakeNewTrigger -= namedTriggers[name];
        namedTriggers.Remove(name);
    }
    /// <summary>
    /// Subscribe to this to attach your custom callbacks to newly created happen objects. You can also use <see cref="AddNamedAction(string, Create_NamedHappenBuilder)"/> and <see cref="AddNamedAction(string, lc_AbstractUpdate?, lc_RealizedUpdate?, lc_Init?, lc_CoreUpdate?)"/> as name-safe shorthands.
    /// </summary>
    public static event Create_RawHappenBuilder? EV_MakeNewHappen;
    internal static IEnumerable<Create_RawHappenBuilder?>? MNH_invl
        => EV_MakeNewHappen?.GetInvocationList().Cast<Create_RawHappenBuilder?>();//.ToArray();
    /// <summary>
    /// Subscribe to this to dispense your custom triggers. You can also use <see cref="AddNamedTrigger(string, Create_NamedTriggerFactory)"/> as a name-safe shorthand.
    /// </summary>
    public static event Create_RawTriggerFactory? EV_MakeNewTrigger;
    internal static IEnumerable<Create_RawTriggerFactory?>? MNT_invl
        => EV_MakeNewTrigger?.GetInvocationList()?.Cast<Create_RawTriggerFactory?>();//.ToArray();
    #endregion
}
