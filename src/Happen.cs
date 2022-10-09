using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Atmo;
/// <summary>
/// A "World event": sealed class that carries custom code in form of callbacks
/// </summary>
public sealed class Happen : IEquatable<Happen>, IComparable<Happen>
{
    public Happen(HappenConfig _cfg)
    {
        cfg = _cfg;
    }
    private Guid guid = Guid.NewGuid();
    internal HappenConfig cfg;
    internal void Call_AbstUpdate (AbstractRoom absroom, int time) { On_AbstUpdate?.Invoke(absroom, time); }
    /// <summary>
    /// Attach to this to receive a call once per abstract update, for every affected room
    /// </summary>
    public event HappenCallbacks.AbstractUpdate? On_AbstUpdate;
    internal void Call_RealUpdate (Room room) { On_RealUpdate?.Invoke(room); }

    /// <summary>
    /// Attach to this to receive a call once per realized update, for every affected room
    /// </summary>
    public event HappenCallbacks.RealizedUpdate? On_RealUpdate;
    internal void Call_Init(World world) { On_Init?.Invoke(world); }
    /// <summary>
    /// Subscribe to this to receive one call when abstract update is first ran.
    /// </summary>
    public event HappenCallbacks.Init? On_Init;

    internal void CoreUpdate(RainWorldGame rwg)
    {
        foreach (var tr in cfg.when) tr.Update(rwg);
        On_CoreUpdate?.Invoke(rwg);
    }
    /// <summary>
    /// Subscribe to this to receive an update once per frame
    /// </summary>
    public event HappenCallbacks.CoreUpdate? On_CoreUpdate;

    /// <summary>
    /// Checks if happen should be running
    /// </summary>
    /// <param name="rwg">RainWorldGame instance to check</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public bool IsOn(RainWorldGame rwg)
    {
        foreach (var t in cfg.when) { if (!t.ShouldRun(rwg)) return false; }
        return true;
    }

    public int CompareTo(Happen other)
    {
        return guid.CompareTo(other.guid);
    }

    public bool Equals(Happen other)
    {
        return guid.Equals(other.guid);
    }

    public bool InitRan { get; internal set; }
}
