﻿using Atmo.Gen;

namespace Atmo.Body;
/// <summary>
/// A "World event": sealed class that carries custom code in form of callbacks. Every happen is read from within a <c>HAPPEN:...END HAPPEN</c> block in an .ATMO file. The following example block:
/// <para>
/// <code>
/// HAPPEN: test
/// WHAT: palette 15
/// WHERE: first + SU_A41 SU_A42 - SU_A22
/// WHERE: SU_C04
/// WHEN: karma 1 2 10
/// END HAPPEN
/// </code>
/// Will result in a happen that has the following properties:
/// <list type="table">
/// <listheader><term>Property</term> <description>Contents and meaning</description></listheader>
/// <item>
///		<term><see cref="name"/></term> 
///		<description>
///			Unique string that identifies an instance. 
///			This happen will be called <c>test</c>.
///		</description>
///	</item>
/// <item>
///		<term>Behaviour</term> 
///		<description>
///			The instance's lifetime events are populated with callbacks taken from <see cref="__EV_MakeNewHappen"/>.
///			For registering your behaviours, see <seealso cref="Atmo.API"/>.
///			To see examples of how some of the builtin behaviours work, see <seealso cref="HappenBuilding.__InitBuiltins"/>.
///			This happen will change main palette of affected rooms to 15.
///		</description>
///	</item>
/// <item>
///		<term>Grouping</term> 
///		<description>
///			The set of rooms an instance is active in.
///			This Happen will activate in group called <c>first</c>, and additionally in rooms <c>SU_A41</c> and <c>SU_A42</c>, 
///			but will not be activated in <c>SU_A22</c> if <c>SU_A22</c> is present in the group. 
///			See <seealso cref="HappenSet"/> to see how Happens and Rooms are grouped together.
///		</description>
///	</item>
/// <item>
///		<term><see cref="conditions"/></term> 
///		<description>
///			A <seealso cref="HappenTrigger"/> created from the WHEN expression,
///			which determines when the Happen should be active or not. 
///			This happen will be active when player's karma level is 1, 2 or 10. 
///			See code of <seealso cref="PredicateInlay"/> if you want to know how the expression is parsed.
///		</description>
///	</item>
/// </list>
/// </para>
/// </summary>
public sealed class Happen : IEquatable<Happen>, IComparable<Happen> {
	internal const int PROFILER_CYCLE_COREUP = 200;
	internal const int PROFILER_CYCLE_REALUP = 400;
	internal const int STORE_CYCLES = 12;
	#region fields/props
	#region perfrec
	internal readonly LinkedList<double> realup_readings = new();
	internal readonly List<TimeSpan> realup_times = new(PROFILER_CYCLE_REALUP);
	internal readonly LinkedList<double> haeval_readings = new();
	internal readonly List<TimeSpan> haeval_times = new(PROFILER_CYCLE_COREUP);
	#endregion perfrec
	/// <summary>
	/// Displays whether a happen is active during the current frame. Updated on <see cref="Atmod.DoBodyUpdates(On.RainWorldGame.orig_Update, RainWorldGame)"/>.
	/// </summary>
	public bool Active { get; private set; }
	/// <summary>
	/// Whether the init callbacks have been invoked or not.
	/// </summary>
	public bool InitRan { get; internal set; }
	/// <summary>
	/// HappenSet this Happen is associated with.
	/// Ownership may change when merging atmo files from different regpacks.
	/// </summary>
	public HappenSet Set { get; internal set; }
	/// <summary>
	/// Used internally for sorting.
	/// </summary>
	internal readonly Guid _guid = Guid.NewGuid();
	/// <summary>
	/// Used for frame time profiling.
	/// </summary>
	internal readonly DBG.Stopwatch _sw = new();
	#region fromcfg
	/// <summary>
	/// Activation expression. Populated by <see cref="HappenTrigger.ShouldRunUpdates"/> callbacks of items in <see cref="triggers"/>.
	/// </summary>
	public readonly PredicateInlay? conditions;
	/// <summary>
	/// All triggers associated with the happen.
	/// </summary>
	public readonly List<HappenTrigger> triggers;
	/// <summary>
	/// name of the happen.
	/// </summary>
	public readonly string name;
	/// <summary>
	/// A set of actions with their parameters.
	/// </summary>
	public readonly Dictionary<string, string[]> actions;
	/// <summary>
	/// Current game instance.
	/// </summary>
	public readonly RainWorldGame game;
	#endregion fromcfg
	#endregion fields/props
	/// <summary>
	/// Creates a new instance from given config, set and game reference.
	/// </summary>
	/// <param name="cfg">A config containing basic setup info.
	/// Make sure it is properly instantiated, and none of the fields are unexpectedly null.</param>
	/// <param name="owner">HappenSet this happen will belong to. Must not be null.</param>
	/// <param name="game">Current game instance. Must not be null.</param>
	public Happen(
		HappenConfig cfg,
		HappenSet owner,
		RainWorldGame game) {
		BangBang(owner, nameof(owner));
		BangBang(game, nameof(game));
		Set = owner;
		name = cfg.name;
		this.game = game;
		actions = cfg.actions;
		conditions = cfg.conditions;
		List<HappenTrigger> list_triggers = new();
		conditions?.Populate((id, args) => {
			HappenTrigger? nt = HappenBuilding.__CreateTrigger(id, args, game, this);
			list_triggers.Add(nt);
			return nt.ShouldRunUpdates;
		});
		triggers = list_triggers;
		HappenBuilding.__NewHappen(this);

		if (actions.Count is 0) __logger.LogWarning($"Happen {this}: no actions! Possible missing 'WHAT:' clause");
		if (conditions is null) __logger.LogWarning($"Happen {this}: did not receive conditions! Possible missing 'WHEN:' clause");
	}
	#region lifecycle cbs
	internal void AbstUpdate(
		AbstractRoom absroom,
		int time) {
		if (On_AbstUpdate is null) return;
		foreach (V0_lc_AbstractUpdate cb in On_AbstUpdate.GetInvocationList().Cast<V0_lc_AbstractUpdate>()) {
			try {
				cb?.Invoke(absroom, time);
			}
			catch (Exception ex) {
				__logger.LogError(ErrorMessage(Site.abstup, cb, ex));
				On_AbstUpdate -= cb;
			}
		}
	}
	/// <summary>
	/// Attach to this to receive a call once per abstract update, for every affected room.
	/// </summary>
	public event V0_lc_AbstractUpdate? On_AbstUpdate;
	internal void RealUpdate(Room room) {
		_sw.Start();
		if (On_RealUpdate is null) return;
		foreach (V0_lc_RealizedUpdate cb in On_RealUpdate.GetInvocationList().Cast<V0_lc_RealizedUpdate>()) {
			try {
				cb?.Invoke(room);
			}
			catch (Exception ex) {
				__logger.LogError(ErrorMessage(Site.realup, cb, ex));
				On_RealUpdate -= cb;
			}
		}
		LogFrameTime(realup_times, _sw.Elapsed, realup_readings, STORE_CYCLES);
		_sw.Reset();
	}
	/// <summary>
	/// Attach to this to receive a call once per realized update, for every affected room.
	/// </summary>
	public event V0_lc_RealizedUpdate? On_RealUpdate;
	internal void Init(World world) {
		InitRan = true;
		if (On_Init is null) return;
		foreach (V0_lc_Init cb in On_Init.GetInvocationList().Cast<V0_lc_Init>()) {
			try {
				cb?.Invoke(world);
			}
			catch (Exception ex) {
				__logger.LogError(ErrorMessage(
					where: Site.init,
					cb: cb,
					ex: ex,
					resp: Response.none
					));
			}
		}
	}
	/// <summary>
	/// Subscribe to this to receive one call before abstract or realized update is first ran.
	/// </summary>
	public event V0_lc_Init? On_Init;
	internal void CoreUpdate() {
		_sw.Start();
		for (int tin = triggers.Count - 1; tin > -1; tin--) {
			try {
				triggers[tin].Update();
			}
			catch (Exception ex) {
				HappenTrigger tr = triggers[tin];
				triggers.RemoveAt(tin);
				__logger.LogError(ErrorMessage(
					where: Site.triggerupdate,
					cb: tr.Update,
					ex: ex,
					resp: Response.void_trigger));
			}
		}
		try {
			Active = conditions?.Eval() ?? true;
		}
		catch (Exception ex) {
#pragma warning disable IDE0031 // Use null propagation
			__logger.LogError(ErrorMessage(
				where: Site.eval,
				cb: conditions is null ? null : conditions.Eval,
				ex: ex,
				resp: Response.none));
#pragma warning restore IDE0031 // Use null propagation
		}
		for (int tin = triggers.Count - 1; tin > -1; tin--) {
			try {
				triggers[tin]?.EvalResults(Active);
			}
			catch (Exception ex) {
				triggers.RemoveAt(tin);

				__logger.LogError(ErrorMessage(
				where: Site.eval_res,
				cb: conditions is null ? null : conditions.Eval,
				ex: ex,
				resp: Response.void_trigger));
			}
		}
		if (On_CoreUpdate is null) return;

		//todo: cast cost?
		foreach (V0_lc_CoreUpdate cb in On_CoreUpdate.GetInvocationList()) {
			try {
				cb(game);
			}
			catch (Exception ex) {
				__logger.LogError(ErrorMessage(Site.coreup, cb, ex));
				On_CoreUpdate -= cb;
			}
		}
		LogFrameTime(haeval_times, _sw.Elapsed, haeval_readings, STORE_CYCLES);
		_sw.Reset();
	}
	/// <summary>
	/// Subscribe to this to receive an update once per frame.
	/// </summary>
	public event V0_lc_CoreUpdate? On_CoreUpdate;
	#endregion
	/// <summary>
	/// Returns a performance report struct.
	/// </summary>
	/// <returns></returns>
	public Perf PerfRecord() {
		Perf perf = new() {
			name = name,
			samples_eval = haeval_readings.Count,
			samples_realup = realup_readings.Count
		};
		double
			realuptotal = 0d,
			evaltotal = 0d;
		if (perf.samples_realup is not 0) {
			foreach (double rec in realup_readings) realuptotal += rec;
			perf.avg_realup = realuptotal / realup_readings.Count;
		}
		else {
			perf.avg_realup = double.NaN;
		}
		if (perf.samples_eval is not 0) {
			foreach (double rec in haeval_readings) evaltotal += rec;
			perf.avg_eval = evaltotal / haeval_readings.Count;
		}
		else {
			perf.avg_eval = double.NaN;
		}
		return perf;
	}
	#region general
	/// <summary>
	/// Compares to another happen using GUIDs.
	/// </summary>
	/// <param name="other"></param>
	/// <returns></returns>
	public int CompareTo(Happen other) {
		return _guid.CompareTo(other._guid);
	}
	/// <summary>
	/// Compares to another happen using GUIDs.
	/// </summary>
	/// <param name="other"></param>
	/// <returns></returns>
	public bool Equals(Happen other) {
		return _guid.Equals(other._guid);
	}
	/// <summary>
	/// Returns a string representation of the happen.
	/// </summary>
	/// <returns></returns>
	public override string ToString() {
		return $"{name}" +
			$"[{(actions.Count == 0 ? string.Empty : actions.Select(x => $"{x.Key}").Aggregate(JoinWithComma))}]" +
			$"({triggers.Count} triggers)";
	}
	#endregion
	#region nested
	/// <summary>
	/// Carries performance report from the happen.
	/// </summary>
	public record struct Perf {
		/// <summary>
		/// Happen name
		/// </summary>
		public string name;
		/// <summary>
		/// Average real update frame time
		/// </summary>
		public double avg_realup;
		/// <summary>
		/// Number of recorded real update frame time samples
		/// </summary>
		public int samples_realup;
		/// <summary>
		/// Average eval invocation time
		/// </summary>
		public double avg_eval;
		/// <summary>
		/// Number of recorded eval frame time samples
		/// </summary>
		public int samples_eval;
	}
	private enum Site {
		abstup,
		realup,
		coreup,
		init,
		eval,
		eval_res,
		triggerupdate,
	}
	private enum Response {
		none,
		remove_cb,
		void_trigger
	}
	#endregion
	private string ErrorMessage(
		Site where,
		Delegate? cb,
		Exception ex,
		Response resp = Response.remove_cb) {
		return $"Happen {this}: {where}: " +
			$"Error on invoke {cb}//{cb?.Method}:" +
			$"\n{ex}" +
			$"\nAction taken: " + resp switch {
				Response.none => "none.",
				Response.remove_cb => "removing problematic callback.",
				Response.void_trigger => "voiding trigger.",
				_ => "???",
			};
	}
}
