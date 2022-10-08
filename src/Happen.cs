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
            this.cfg = _cfg;
        }

        internal HappenConfig cfg;
        public void Call_AbstUpdate (AbstractRoom absroom, int time) { on_abst_update?.Invoke(absroom, time); }
        public event Callbacks.AbstractUpdate? on_abst_update;
        public void Call_RealUpdate (Room room) { on_real_update?.Invoke(room); }
        public event Callbacks.RealizedUpdate? on_real_update;
        public void Call_Init(World world) { on_init?.Invoke(world); }
        public event Callbacks.Init? on_init;

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
