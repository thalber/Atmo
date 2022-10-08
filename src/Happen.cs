using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Atmo
{
    public sealed class Happen
    {
        public Happen(HappenConfig _cfg)
        {
            cfg = _cfg;
        }

        internal HappenConfig cfg;
        public void Call_AbstUpdate (AbstractRoom absroom, int time) { On_abst_update?.Invoke(absroom, time); }
        public event HappenCallbacks.AbstractUpdate? On_abst_update;
        public void Call_RealUpdate (Room room) { On_real_update?.Invoke(room); }
        public event HappenCallbacks.RealizedUpdate? On_real_update;
        public void Call_Init(World world) { On_init?.Invoke(world); }
        public event HappenCallbacks.Init? On_init;

        public void CoreUpdate(RainWorldGame rwg)
        {
            foreach (var tr in cfg.when) tr.Update(rwg);
        }

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
