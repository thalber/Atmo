using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Atmo
{
    public sealed class Event
    {
        public Event(EventConfig _cfg)
        {
            this.cfg = _cfg;
        }

        internal EventConfig cfg;
        public void call_abst_update (AbstractRoom absroom, int time) { on_abst_update?.Invoke(absroom, time); }
        public event Callbacks.AbstractUpdate? on_abst_update;
        public void call_real_update (Room room) { on_real_update?.Invoke(room); }
        public event Callbacks.RealizedUpdate? on_real_update;
        public void call_init(World world) { on_init?.Invoke(world); }
        public event Callbacks.Init? on_init;

        /// <summary>
        /// Checks if event should be running
        /// </summary>
        /// <param name="rwg">RainWorldGame instance to check</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public bool is_on(RainWorldGame rwg)
        {
            return cfg.when switch
            {
                EventConfig.TriggerType.Always => true,
                EventConfig.TriggerType.OnRain => rwg.globalRain is not null,
                _ => throw new ArgumentException(),
            };
        }

        public bool init_ran { get; internal set; }
    }
}
