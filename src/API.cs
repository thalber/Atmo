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
    #region API proper
    //todo: make safe callbacks undoable
    /// <summary>
    /// Tries to register callbacks to be added to happens that have a specified actions, up to one per lifecycle event.
    /// </summary>
    /// <param name="action">Action name. Case insensitive.</param>
    /// <param name="au">Abstract update callback.</param>
    /// <param name="ru">Realized update callback.</param>
    /// <param name="oi">Init callback.</param>
    /// <param name="cu">Init callback.</param>
    /// <returns>True if successfully added; false if name already taken.</returns>
    public static bool AddCallbacksOnAction(
        string action,
        lc_AbstractUpdate? au = null,
        lc_RealizedUpdate? ru = null,
        lc_Init? oi = null,
        lc_CoreUpdate? cu = null)
    {
        action = action.ToLower();
        if (takenActionNames.Contains(action)) return false;
        API_MakeNewHappen += (ha) =>
        {
            if (ha.actions.Contains(action))
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
    /// <summary>
    /// Registers a <see cref="Create_HappenBuilder"/> for actions with a specific name.
    /// </summary>
    /// <param name="action">Action name. Case insensitive.</param>
    /// <param name="builder">User builder callback.</param>
    /// <returns>true if successfully attached, false if name already taken.</returns>
    public static bool AddCallbacksOnAction(
        string action,
        Create_HappenBuilder builder)
    {
        action = action.ToLower();
        if (takenActionNames.Contains(action)) return false;
        API_MakeNewHappen += (ha) => { if (ha.actions.Contains(action)) builder?.Invoke(ha); };
        takenActionNames.Add(action);
        return true;
    }
    /// <summary>
    /// Registers a trigger factory for a specific name.
    /// </summary>
    /// <param name="name">Trigger name.</param>
    /// <param name="fac">User trigger factory callback.</param>
    /// <returns>true if successfully attached, false if name already taken.</returns>
    public static bool MakeTriggerOnName(
        string name,
        Create_SafeTriggerFactory fac)
    {
        name = name.ToLower();
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
    internal static IEnumerable<Create_HappenBuilder?>? MNH_invl
        => API_MakeNewHappen?.GetInvocationList().Cast<Create_HappenBuilder?>();//.ToArray();
    /// <summary>
    /// Subscribe to this to dispense your custom triggers.
    /// </summary>
    public static event Create_RawTriggerFactory? API_MakeNewTrigger;
    internal static IEnumerable<Create_RawTriggerFactory?>? MNT_invl
        => API_MakeNewTrigger?.GetInvocationList()?.Cast<Create_RawTriggerFactory?>();//.ToArray();
    #endregion
}
