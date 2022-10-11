using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using static Atmo.Atmod;

namespace Atmo;
public static class HappenCallbacks
{
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
    public delegate IEnumerable<HappenTrigger> Create_AddTriggers(Happen ha);

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
#warning test
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
    /// Subscribe to this to attach your custom callbacks to newly created happen objects.
    /// </summary>
    public static event Create_AddCallbacks? RegisterNewHappen;
}
