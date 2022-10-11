﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RWCustom;

using static Atmo.Utils;
using URand = UnityEngine.Random;

namespace Atmo;
/// <summary>
/// base class for triggers. Triggers determine when happens are allowed to run; to have a happen run on a given frame, each one of its triggers must be ready.
/// </summary>
public abstract class HappenTrigger
{
    #region fields
    public static readonly string[] trueStrings = new[] { "true", "1", "yes", };
    #endregion
    public static HappenTrigger CreateTrigger(string id, string[] args, RainWorldGame rwg)
    {
        HappenTrigger res = null;
        switch (id)
        {
            case "untilrain":
            case "beforerain":
                {
                    int.TryParse(args.AtOr(0, "0"), out var delay);
                    res = new BeforeRain(rwg, delay);
                }
                break;
            case "afterrain":
                {
                    int.TryParse(args.AtOr(0, "0"), out var delay);
                    res = new AfterRain(rwg, delay);
                }
                break;
            case "everyx":
            case "every":
                {
                    var def = "40";
                    int.TryParse(args.AtOr(0, "40"), out var period);
                    res = new EveryX(period);
                }
                break;
            case "maybe":
            case "chance":
                {
                    float.TryParse(args.AtOr(0, "0.5"), out var ch);
                    res = new Maybe(ch);
                }
                break;
            case "flicker":
                {
                    int[] argsp = new int[4];
                    for (int i = 0; i < 4; i++)
                    {
                        int.TryParse(args.AtOr(i, "300"), out argsp[i]);
                    }
                    bool startOn = trueStrings.Contains(args.AtOr(4, "1").ToLower());
                    res = new Flicker(argsp[0], argsp[1], argsp[2], argsp[3], startOn);
                }
                break;
            default:
                //res = new Always();
                break;
        }
        res ??= new Always();
        return res;
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
        public NeedsRWG(RainWorldGame rwg) { this.rwg = rwg; }
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
        public AfterRain(RainWorldGame rwg, int delay = 0) : base (rwg)
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
        public BeforeRain(RainWorldGame rwg, int delay = 0) : base (rwg)
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
    public sealed class Maybe : HappenTrigger
    {
        public Maybe(float chance)
        {
            yes = UnityEngine.Random.value < chance;
        }
        private bool yes;
        public override bool ShouldRunUpdates() => yes;
    }

    public sealed class Flicker : HappenTrigger
    {
        private readonly int minOn;
        private readonly int maxOn;
        private readonly int minOff;
        private readonly int maxOff;
        private bool on;
        private int counter;

        public Flicker(int minOn, int maxOn, int minOff, int maxOff , bool startOn = true)
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
}
