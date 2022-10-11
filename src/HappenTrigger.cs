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
    public static HappenTrigger CreateTrigger(string text, RainWorldGame rwg)
    {
        throw new NotImplementedException();
    }
    /// <summary>
    /// Answers if a trigger is currently ready.
    /// </summary>
    /// <param name="game"></param>
    /// <returns></returns>
    public abstract bool ShouldRunUpdates();
    /// <summary>
    /// called every <see cref="Happen.CoreUpdate(RainWorldGame)"/>, *once* every frame
    /// </summary>
    /// <param name="rwg"></param>
    public virtual void Update() { }

    public abstract class NeedsRWG : HappenTrigger
    {
        protected RainWorldGame rwg;
    }
    /// <summary>
    /// Sample trigger, Always true.
    /// </summary>
    public sealed class Always : HappenTrigger {
        public override bool ShouldRunUpdates() => true;
    }
    /// <summary>
    /// Sample trigger, works after rain starts. Supports an optional delay (in frames)
    /// </summary>
    public sealed class AfterRain : NeedsRWG
    {
        public AfterRain(int delay = 0)
        {
            this.delay = delay;
        }
        private int delay;
        public override bool ShouldRunUpdates()
        {
            return rwg.world.rainCycle.TimeUntilRain + delay <= 0;
        }
    }
    /// <summary>
    /// Sample trigger, true until rain starts. Supports an optional delay.
    /// </summary>
    public sealed class BeforeRain : NeedsRWG
    {
        public BeforeRain(int delay = 0)
        {
            this.delay = delay;
        }
        private int delay;
        public override bool ShouldRunUpdates()
        {
            return rwg.world.rainCycle.TimeUntilRain + delay >= 0;
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
        public override bool ShouldRunUpdates()
        {
            return counter is 0;
        }
        public override void Update()
        {
            if (--counter < 0) counter = period;
        }
    }
    /// <summary>
    /// Upon instantiation, rolls with given chance. If successful, stays on always.
    /// </summary>
    public sealed class Maybe
    {
        public Maybe(float chance)
        {
            yes = UnityEngine.Random.value < chance;
        }
        private bool yes;
    }


}
