namespace Atmo;
/// <summary>
/// Base class for triggers. Triggers determine when happens are allowed to run; They are composed into a <see cref="PredicateInlay"/> instance, that acts as a logical expression tree.
/// <para>
/// This expression: <code> WHEN: (maybe 0.7 OR karma 1 2 3) AND fry 25 5 </code> will be turned into a set of happentrigger children:
/// <list type="number">
/// <item>A <see cref="Maybe"/>, that takes argument 0.7. Each cycle, this will either be active with 70% chance, or inactive with 30% chance.</item>
/// <item>A <see cref="OnKarma"/>, that is active when the player's current karma is 1, 2 or 3.</item>
/// <item>A <see cref="Fry"/>, that takes two arguments: activity limit and cooldown. It will always be active, but turn off (for 5 seconds) once the happen is active for long enough (25 seconds).</item>
/// </list>
/// These objects' collective response to <see cref="ShouldRunUpdates"/> will be checked up to once each frame. Expressions follow normal boolean logic stucture, operator precedence is NOT > AND > XOR > OR.
/// </para>
/// <para>
/// Note that <see cref="PredicateInlay.Eval"/> can short-circuit, so some of the triggers may not receive a call to their <see cref="ShouldRunUpdates"/> every frame. In this example, evaluation starts with Maybe, then goes on to Karma. If at least one of them is true, Fry gets evaluated; if they are both false, expression short-circuits, and Fry is not evaluated.
/// </para>
/// </summary>
public abstract partial class HappenTrigger
{
	#region fields
	/// <summary>
	/// Happen that owns this instance. Some triggers, such as <see cref="AfterOther"/>, need this to check state of other triggers, the owning happen itself, or other happens in the current set.
	/// </summary>
	protected Happen? owner;
	#endregion
	/// <summary>
	/// Provide an owning happen if you need to. You can provide null or omit the parameter if you do not require it.
	/// </summary>
	/// <param name="ow">The happen instance that owns this trigger. Can be null.</param>
	public HappenTrigger(Happen? ow = null) { owner = ow; }
	/// <summary>
	/// Answers if a trigger is currently ready. This can be called up to once per frame.
	/// </summary>
	/// <returns></returns>
	public abstract bool ShouldRunUpdates();
	/// <summary>
	/// Called every <see cref="Happen.CoreUpdate(RainWorldGame)"/>, *once* every frame. Override this to do your frame-persistent logic.
	/// </summary>
	public virtual void Update() { }
	/// <summary>
	/// Called after every eval to signal expression's final result for current frame. For example, <see cref="Fry"/> uses this to track how long has the happen been active.
	/// </summary>
	/// <param name="res"></param>
	public virtual void EvalResults(bool res) { }
	#region builtins
#pragma warning disable CS1591
#warning contributor notice: triggers
	//Place your trigger classes here.
	//Don't forget to register them in HappenBuilding.RegisterDefaultTriggers as well.
	//Do not remove the warning directive.
	/// <summary>
	/// Intermediary abstract class for triggers that require RainWorldGame state access (such as <see cref="OnKarma"/> or <see cref="AfterRain"/>). Make sure to provide non-null <see cref="RainWorldGame"/> instance to its constructor.
	/// </summary>
	public abstract class NeedsRWG : HappenTrigger
	{
		/// <summary>
		/// Intermediary constructor. Make sure you provide non-null RainWorldGame instance. Owner may be null.
		/// </summary>
		/// <param name="game">Current game instance for state access.</param>
		/// <param name="ow"></param>
		public NeedsRWG(RainWorldGame game!!, Happen? ow = null) : base(ow) { this.game = game; }
		/// <summary>
		/// The required rain world instance.
		/// </summary>
		protected readonly RainWorldGame game;
	}
	/// <summary>
	/// Sample trigger, Always true.
	/// </summary>
	public sealed class Always : HappenTrigger
	{
		public Always() : base(null)
		{
		}
		public override bool ShouldRunUpdates()
		{
			return true;
		}
	}
	/// <summary>
	/// Sample trigger, works after rain starts. Supports an optional delay (in seconds.)
	/// <para>
	/// Example use: <code></code>
	/// </para>
	/// </summary>
	public sealed class AfterRain : NeedsRWG
	{
		public AfterRain(RainWorldGame rwg, Happen ow, Arg delay = null) : base(rwg, ow)
		{
			this.delay = (int?)(delay?.F32 * 40) ?? 0;
		}
		private readonly int delay;
		public override bool ShouldRunUpdates()
		{
			return game.world.rainCycle.TimeUntilRain + delay <= 0;
		}
	}
	/// <summary>
	/// Sample trigger, true until rain starts. Supports an optional delay.
	/// </summary>
	public sealed class BeforeRain : NeedsRWG
	{
		public BeforeRain(RainWorldGame rwg, Happen ow, Arg delay = null) : base(rwg, ow)
		{
			this.delay = (int?)(delay?.F32 * 40) ?? 0;
		}
		private readonly int delay;
		public override bool ShouldRunUpdates()
		{
			return game.world.rainCycle.TimeUntilRain + delay >= 0;
		}
	}
	/// <summary>
	/// Sample trigger, fires every X frames.
	/// </summary>
	public sealed class EveryX : HappenTrigger
	{
		public EveryX(Arg x, Happen ow) : base(ow)
		{
			period = (int?)(x?.F32 * 40) ?? 30;
		}

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
		public Maybe(Arg chance)
		{
			yes = RND.value < chance.F32;
		}
		private readonly bool yes;
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

		public Flicker(Arg minOn, Arg maxOn, Arg minOff, Arg maxOff, bool startOn = true)
		{
			this.minOn = (int?)(minOn?.F32 * 40) ?? 200;
			this.maxOn = (int?)(maxOn?.F32 * 40) ?? 200;
			this.minOff = (int?)(minOff?.F32 * 40) ?? 400;
			this.maxOff = (int?)(maxOff?.F32 * 40) ?? 400;
			ResetCounter(startOn);
		}
		private void ResetCounter(bool next)
		{
			on = next;
			counter = on switch
			{
				true => RND.Range(minOn, maxOn),
				false => RND.Range(minOff, maxOff),
			};
		}
		public override bool ShouldRunUpdates()
		{
			return on;
		}

		public override void Update() { if (counter-- < 0) ResetCounter(!on); }
	}
	/// <summary>
	/// Requires specific karma levels
	/// </summary>
	public sealed class OnKarma : NeedsRWG
	{
		private readonly List<int> levels = new();
		//private readonly List<>;
		public OnKarma(RainWorldGame rwg, ArgSet options, Happen? ow = null) : base(rwg, ow)
		{
			foreach (Arg op in options)
			{
				if (int.TryParse(op.Str, out var r)) levels.Add(r);
				var spl = TXT.Regex.Split(op.Str, "\\s*-\\s*");
				if (spl.Length == 2)
				{
					int.TryParse(spl[0], out var min);
					int.TryParse(spl[1], out var max);
					for (var i = min; i <= max; i++) if (!levels.Contains(i)) levels.Add(i);
				}
			}
		}
		public override bool ShouldRunUpdates()
		{
			return levels.Contains((game.Players[0].realizedCreature as Player)?.Karma ?? 0);
		}
	}
	/// <summary>
	/// Activates after any player visits a specific set of rooms.
	/// </summary>
	public sealed class AfterVisit : NeedsRWG
	{
		private string[] rooms;
		private bool visit = false;
		public AfterVisit(RainWorldGame rwg, ArgSet roomnames) : base(rwg)
		{
			rooms = roomnames.Select(x => x.Str).ToArray();//roomnamesseWhere;
		}
		public override void Update()
		{
			if (visit) return;
			foreach (var player in game.Players) if (rooms.Contains(player.Room.name)) visit = true;
		}
		public override bool ShouldRunUpdates()
		{
			return visit;
		}
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
		public Fry(Arg limit, Arg cd)
		{
			this.limit = (int)(limit.F32 * 40f);
			this.cd = (int)(cd.F32 * 40f);
			counter = 0;
			active = true;
		}
		public override bool ShouldRunUpdates() => active;
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
		internal Happen tar;
		//internal readonly System.Collections.BitArray inertia;
		internal readonly string tarname;
		internal readonly int delay;
		internal bool tarWasOn;
		internal bool amActive;
		internal readonly List<int> switchOn = new();
		internal readonly List<int> switchOff = new();
		public AfterOther(Happen owner, Arg tarname, Arg delay) : base(owner)
		{
			this.delay = (int?)(delay.F32 * 40) ?? 40;
			this.tarname = (string)tarname;
			//tar = owner.set.AllHappens.FirstOrDefault(x => x.name == tarname.Str);
		}
		public override void Update()
		{
			tar ??= owner?.set.AllHappens.FirstOrDefault(x => x.name == tarname);
			if (tar is null) return;
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
			for (var i = 0; i < switchOn.Count; i++)
			{
				switchOn[i]--;
			}
			if (switchOn.FirstOrDefault() < 0)
			{
				switchOn.RemoveAt(0);
				amActive = true;
			}
			for (var i = 0; i < switchOff.Count; i++)
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
		public override bool ShouldRunUpdates() => amActive;
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
		public AfterDelay(Arg dmin, Arg dmax, RainWorldGame rwg) : base(rwg)
		{
			delay = RND.Range((int?)(dmin?.F32 * 40f) ?? 0, (int?)(dmax?.F32 * 40f) ?? 2400);
		}
		/// <summary>
		/// Creates an instance with static delay.
		/// </summary>
		/// <param name="d">Delay</param>
		/// <param name="rwg">RWG instance to check the clock.</param>
		public AfterDelay(Arg d, RainWorldGame rwg) : base(rwg)
		{
			delay = (int?)(d?.F32 * 40f) ?? 2400;
		}
		public override bool ShouldRunUpdates()
		{
			return game.world.rainCycle.timer > delay;
		}
	}
#pragma warning restore CS1591
	#endregion
}
