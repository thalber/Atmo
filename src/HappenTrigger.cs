using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RWCustom;

using static Atmo.Utils;
using URand = UnityEngine.Random;
using TXT = System.Text.RegularExpressions;

namespace Atmo;
/// <summary>
/// base class for triggers. Triggers determine when happens are allowed to run; to have a happen run on a given frame, each one of its triggers must be ready.
/// </summary>
public abstract class HappenTrigger
{
    #region fields
    /// <summary>
    /// Strings that evaluate to bool.true
    /// </summary>
    public static readonly string[] trueStrings = new[] { "true", "1", "yes", };
    /// <summary>
    /// Happen that owns this instance.
    /// </summary>
    protected Happen? owner;
    #endregion
    /// <summary>
    /// Provide an owning happen if you need to.
    /// </summary>
    /// <param name="ow"></param>
    public HappenTrigger(Happen? ow = null) { owner = ow; }
    /// <summary>
    /// Answers if a trigger is currently ready.
    /// </summary>
    /// <param name="game"></param>
    /// <returns></returns>
    public abstract bool ShouldRunUpdates();
    /// <summary>
    /// called every <see cref="Happen.CoreUpdate(RainWorldGame)"/>, *once* every frame
    /// </summary>
    public virtual void Update() { }
    /// <summary>
    /// Called after every eval to signal expression's final result for current frame.
    /// </summary>
    /// <param name="res"></param>
    public virtual void EvalResults(bool res) { }
    /// <summary>
    /// Intermediary abstract class for triggers that require RainWorldGame state access.
    /// </summary>
    public abstract class NeedsRWG : HappenTrigger
    {
        public NeedsRWG(RainWorldGame rwg, Happen? ow = null) : base(ow) { this.rwg = rwg; }
        /// <summary>
        /// The required rain world instance.
        /// </summary>
        protected RainWorldGame rwg;
    }
    /// <summary>
    /// Sample trigger, Always true.
    /// </summary>
    public sealed class Always : HappenTrigger
    {
        public Always(Happen ow) : base(ow)
        {
        }

        public override bool ShouldRunUpdates() => true;
    }
    /// <summary>
    /// Sample trigger, works after rain starts. Supports an optional delay (in frames)
    /// </summary>
    public sealed class AfterRain : NeedsRWG
    {
        public AfterRain(RainWorldGame rwg, Happen ow, int delay = 0) : base(rwg, ow)
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
        public BeforeRain(RainWorldGame rwg, Happen ow, int delay = 0) : base(rwg, ow)
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
        public EveryX(int x, Happen ow) : base(ow) { period = x; }

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
    public sealed class Maybe : HappenTrigger
    {
        public Maybe(float chance)
        {
            yes = URand.value < chance;
        }
        private bool yes;
        public override bool ShouldRunUpdates() => yes;
    }
    /// <summary>
    /// Turns on and off periodically.
    /// </summary>
    public sealed class Flicker : HappenTrigger
    {
        private readonly int minOn;
        private readonly int maxOn;
        private readonly int minOff;
        private readonly int maxOff;
        private bool on;
        private int counter;

        public Flicker(int minOn, int maxOn, int minOff, int maxOff, bool startOn = true)
        {
            this.minOn = minOn;
            this.maxOn = maxOn;
            this.minOff = minOff;
            this.maxOff = maxOff;
            ResetCounter(startOn);
        }

        private void ResetCounter(bool next)
        {
            on = next;
            counter = on switch
            {
                true => URand.Range(minOn, maxOn),
                false => URand.Range(minOff, maxOff),
            };
        }
        public override bool ShouldRunUpdates() => on;
        public override void Update() { if (counter-- < 0) ResetCounter(!on); }
    }
    /// <summary>
    /// Requires specific karma levels
    /// </summary>
    public sealed class OnKarma : NeedsRWG
    {
        private readonly List<int> levels = new();
        //private readonly List<>;
        public OnKarma(RainWorldGame rwg, string[] options, Happen? ow = null) : base(rwg, ow)
        {
            foreach (var op in options)
            {
                if (int.TryParse(op, out int r)) levels.Add(r);
                var spl = TXT.Regex.Split(op, "\\s*-\\s*");
                if (spl.Length == 2)
                {
                    int.TryParse(spl[0], out var min);
                    int.TryParse(spl[1], out var max);
                    for (int i = min; i <= max; i++) if (!levels.Contains(i)) levels.Add(i);
                }
            }
        }
        public override bool ShouldRunUpdates()
            => levels.Contains((rwg.Players[0].realizedCreature as Player)?.Karma ?? 0);
    }
    /// <summary>
    /// Activates after any player visits a specific set of rooms.
    /// </summary>
    public sealed class AfterVisit : NeedsRWG
    {
        private string[] rooms;
        private bool visit = false;
        public AfterVisit(RainWorldGame rwg, string[] roomnames) : base(rwg)
        {
            rooms = roomnames;
        }
        public override void Update()
        {
            if (visit) return;
            foreach (var player in rwg.Players) if (rooms.Contains(player.Room.name)) visit = true;
        }
        public override bool ShouldRunUpdates()
            => visit;
    }
    /// <summary>
    /// Fries and goes inactive for a duration if the happen stays on for too long.
    /// </summary>
    public sealed class Fry : HappenTrigger
    {
        private readonly int limit;
        private readonly int cd;
        private int counter;
        private bool active;
        public Fry(int limit, int cd)
        {
            this.limit = limit;
            this.cd = cd;
            counter = 0;
            active = true;
        }
        public override bool ShouldRunUpdates()
            => active;
        public override void EvalResults(bool res)
        {
            if (active)
            {
                if (res) counter++; else { counter = 0; }
                if (counter > limit) { active = false; counter = cd; }
            }
            else
            {
                counter--;
                if (counter == 0) { active = true; counter = 0; }
            }
        }
    }
    /// <summary>
    /// Activates after another event is tripped, with a customizeable spinup/spindown delay.
    /// </summary>
    public sealed class AfterOther : HappenTrigger
    {
        internal readonly Happen tar;
        //internal readonly System.Collections.BitArray inertia;
        internal readonly int delay;
        internal bool tarWasOn;
        internal bool amActive;
        internal readonly List<int> switchOn = new();
        internal readonly List<int> switchOff = new();
        public AfterOther(Happen owner, string tarname, int delay) : base(owner)
        {
            this.delay = delay;
            tar = owner.set.AllHappens.FirstOrDefault(x => x.name == tarname);
            //inertia = new(new bool[delay]);
        }

        public override void Update()
        {
            if (tar.Active != tarWasOn)
            {
                if (tar.Active)
                {
                    switchOn.Add(delay);
                }
                else
                {
                    switchOff.Add(delay);
                }
            }
            for (int i = 0; i < switchOn.Count; i++)
            {
                switchOn[i]--;
            }
            if (switchOn.FirstOrDefault() < 0)
            {
                switchOn.RemoveAt(0);
                amActive = true;
            }
            for (int i = 0; i < switchOff.Count; i++)
            {
                switchOff[i]--;
            }
            if (switchOff.FirstOrDefault() < 0)
            {
                switchOff.RemoveAt(0);
                amActive = false;
            }
            tarWasOn = tar.Active;
        }
        public override bool ShouldRunUpdates()
            => amActive;
    }
    /// <summary>
    /// Activates after a set delay.
    /// </summary>
    public sealed class AfterDelay : NeedsRWG
    {
        private int delay;
        /// <summary>
        /// Creates an instance with delay in set bounds.
        /// </summary>
        /// <param name="dmin">Lower bound</param>
        /// <param name="dmax">Upper bound</param>
        /// <param name="rwg">RWG instance to check the clock</param>
        public AfterDelay(int dmin, int dmax, RainWorldGame rwg) : base(rwg)
        {
            delay = URand.Range(dmin, dmax);
        }
        /// <summary>
        /// Creates an instance with static delay.
        /// </summary>
        /// <param name="d">Delay</param>
        /// <param name="rwg">RWG instance to check the clock.</param>
        public AfterDelay(int d, RainWorldGame rwg) : base(rwg)
        {
            delay = d;
        }
        public override bool ShouldRunUpdates()
            => rwg.world.rainCycle.timer > delay;
    }
}
