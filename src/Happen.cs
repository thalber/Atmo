using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using static Atmo.Atmod;

namespace Atmo;
/// <summary>
/// A "World event": sealed class that carries custom code in form of callbacks
/// </summary>
public sealed class Happen : IEquatable<Happen>, IComparable<Happen>
{
    //todo: add frame time profiling
    #region fields
    private readonly Guid guid = Guid.NewGuid();
    //internal HappenConfig cfg;
    internal PredicateInlay? conditions;
    public bool initRan;
    private bool active;
    #region fromcfg
    private HappenTrigger[] triggers;
    public readonly string name;
    private string[] actions;
    #endregion fromcfg
    #endregion fields
    internal Happen(HappenConfig cfg, HappenSet owner, RainWorldGame rwg)
    {
        name = cfg.name;
        actions = cfg.actions.Select(x => x.Key).ToArray();
        conditions = cfg.conditions;

        List<HappenTrigger> list_triggers = new();
        conditions?.Populate((id, args) =>
        {
            var nt = HappenBuilding.CreateTrigger(id, args, rwg);
            list_triggers.Add(nt);
            return nt.ShouldRunUpdates;
        });
        triggers = list_triggers.ToArray();
        HappenBuilding.NewEvent(this);
    }
    //private readonly List<Delegate> broken = new();
    #region lifecycle cbs
    internal void AbstUpdate (AbstractRoom absroom, int time) {
        if (On_AbstUpdate is null) return;
        foreach (API.lc_AbstractUpdate cb in On_AbstUpdate.GetInvocationList())
        {
            try
            {
                cb?.Invoke(absroom, time);
            }
            catch (Exception ex)
            {
                inst.Plog.LogError(ErrorMessage(LCE.abstup, cb, ex));
                On_AbstUpdate -= cb;
            }
        }
    }
    /// <summary>
    /// Attach to this to receive a call once per abstract update, for every affected room
    /// </summary>
    public event API.lc_AbstractUpdate? On_AbstUpdate;
    internal void RealUpdate (Room room) {
        if (On_RealUpdate is null) return;
        foreach (API.lc_RealizedUpdate cb in On_RealUpdate.GetInvocationList())
        {
            try
            {
                cb?.Invoke(room);
            }
            catch (Exception ex)
            {
                inst.Plog.LogError(ErrorMessage(LCE.realup, cb, ex));
            }
        }
    }

    /// <summary>
    /// Attach to this to receive a call once per realized update, for every affected room
    /// </summary>
    public event API.lc_RealizedUpdate? On_RealUpdate;
    internal void Init(World world) {
        initRan = true;
        if (On_Init is null) return;
        foreach (API.lc_Init cb in On_Init.GetInvocationList())
        {
            try
            {
                cb?.Invoke(world);
            }
            catch (Exception ex)
            {
                inst.Plog.LogError(ErrorMessage(LCE.init, cb, ex));
                On_Init -= cb;
            }
        }
    }
    /// <summary>
    /// Subscribe to this to receive one call when abstract update is first ran.
    /// </summary>
    public event API.lc_Init? On_Init;
    internal void CoreUpdate(RainWorldGame rwg)
    {
        //broken.Clear();
        foreach (var tr in triggers) tr.Update();
        active = conditions?.Eval() ?? true;
        if (On_CoreUpdate is null) return;
        foreach (API.lc_CoreUpdate cb in On_CoreUpdate.GetInvocationList())
        {
            try
            {
                cb(rwg);
            }
            catch (Exception ex)
            {
                inst.Plog.LogError(ErrorMessage(LCE.coreup, cb, ex));
                //inst.Plog.LogWarning("Removing problematic callback.");
                On_CoreUpdate -= cb;
                //broken.Add(cb);
            }
        }
        //foreach (API.lc_CoreUpdate bcb in broken) On_CoreUpdate -= bcb;
    }

    /// <summary>
    /// Subscribe to this to receive an update once per frame
    /// </summary>
    public event API.lc_CoreUpdate? On_CoreUpdate;
    #endregion

    /// <summary>
    /// Checks if happen should be running
    /// </summary>
    /// <param name="rwg">RainWorldGame instance to check</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public bool IsOn(RainWorldGame rwg)
        => active;
    public int CompareTo(Happen other)
    {
        return guid.CompareTo(other.guid);
    }
    public bool Equals(Happen other)
    {
        return guid.Equals(other.guid);
    }
    public override string ToString()
    {
        return $"{name}-{guid}[{actions.Aggregate(Utils.JoinWithComma)}]({triggers.Length} triggers)";
    }

    private enum LCE
    {
        abstup,
        realup,
        coreup,
        init,
    }
    private string ErrorMessage(LCE where, Delegate cb, Exception ex, bool removing = true)
        => $"Happen {this}: {where}: " +
        $"Error on callback {cb}//{cb.Method}:" +
        $"\n{ex}" + (removing ? "\nRemoving problematic callback." : string.Empty);
}
