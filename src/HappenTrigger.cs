using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Atmo;
/// <summary>
/// base class for triggers. Triggers determine when happens are allowed to run; to have a happen run on a given frame, each one of its triggers must be ready.
/// </summary>
public abstract class HappenTrigger
{
    /// <summary>
    /// Answers if a trigger is currently ready.
    /// </summary>
    /// <param name="game"></param>
    /// <returns></returns>
    public abstract bool ShouldRun(RainWorldGame game);
    /// <summary>
    /// called every <see cref="Happen.CoreUpdate(RainWorldGame)"/>
    /// </summary>
    /// <param name="rwg"></param>
    public virtual void Update(RainWorldGame rwg) { }
    /// <summary>
    /// Sample trigger, Always true.
    /// </summary>
    public sealed class Always : HappenTrigger {
        public override bool ShouldRun(RainWorldGame game) => true;
    }
    /// <summary>
    /// Sample trigger, works after rain starts
    /// </summary>
    public sealed class AfterRain : HappenTrigger
    {
        public override bool ShouldRun(RainWorldGame game)
        {
            return game.world.rainCycle.TimeUntilRain <= 0;
        }
    }
    /// <summary>
    /// Sample trigger, until rain starts
    /// </summary>
    public sealed class BeforeRain : HappenTrigger
    {
        public override bool ShouldRun(RainWorldGame game)
        {
            return game.world.rainCycle.TimeUntilRain >= 0;
        }
    }
    /// <summary>
    /// Sample trigger, fires every X frames.
    /// </summary>
    public sealed class EveryX : HappenTrigger
    {
        public EveryX(int x) { period = x; }

        private readonly int period;
        private int counter;
        public override bool ShouldRun(RainWorldGame game)
        {
            return counter is 0;
        }
        public override void Update(RainWorldGame rwg)
        {
            if (--counter < 0) counter = period;
        }
    }

}
