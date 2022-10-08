using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Atmo
{
    /// <summary>
    /// A "World event": sealed class that carries custom code in form of callbacks
    /// </summary>
    public sealed class Happen
    {
        public Happen(HappenConfig _cfg)
        {
            cfg = _cfg;
        }

        internal HappenConfig cfg;
        public void Call_AbstUpdate (AbstractRoom absroom, int time) { On_AbstUpdate?.Invoke(absroom, time); }
        public event HappenCallbacks.AbstractUpdate? On_AbstUpdate;
        public void Call_RealUpdate (Room room) { On_RealUpdate?.Invoke(room); }
        public event HappenCallbacks.RealizedUpdate? On_RealUpdate;
        public void Call_Init(World world) { On_Init?.Invoke(world); }
        public event HappenCallbacks.Init? On_Init;

        public void CoreUpdate(RainWorldGame rwg)
        {
            foreach (var tr in cfg.when) tr.Update(rwg);
            On_CoreUpdate?.Invoke(rwg);
        }
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

        public bool init_ran { get; internal set; }
    }
}
