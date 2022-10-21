using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static Atmo.Atmod;
using static Atmo.HappenBuilding;
using static Atmo.HappenTrigger;

namespace Atmo;
/// <summary>
/// Static class for user API. You will likely be interacting with the mod from here.
/// </summary>
public static class API
{
	#region fields
	internal static readonly Dictionary<string, Create_RawHappenBuilder> namedActions = new();
	internal static readonly Dictionary<string, Create_RawTriggerFactory> namedTriggers = new();
	#endregion
	#region dels
	/// <summary>
	/// delegate for calling by happens on abstract updates
	/// </summary>
	/// <param name="absroom">absroom</param>
	/// <param name="time">absupdate step</param>
	public delegate void lc_AbstractUpdate(AbstractRoom absroom, int time);
	/// <summary>
	/// delegate for being called by happens on realized updates
	/// </summary>
	/// <param name="room">room</param>
	public delegate void lc_RealizedUpdate(Room room);
	/// <summary>
	/// Delegate for being called on first abstract update
	/// </summary>
	/// <param name="world"></param>
	public delegate void lc_Init(World world);
	/// <summary>
	/// Delegate for being called on core update
	/// </summary>
	/// <param name="rwg"></param>
	public delegate void lc_CoreUpdate(RainWorldGame rwg);
	/// <summary>
	/// callback for attaching custom behaviour to happens. Can be directly attached to <see cref="EV_MakeNewHappen"/>
	/// </summary>
	/// <param name="ha"></param>
	public delegate void Create_RawHappenBuilder(Happen ha);
	/// <summary>
	/// Delegate for registering named callbacks.
	/// Used by <see cref="AddNamedAction(string, Create_NamedHappenBuilder, bool)"/>.
	/// </summary>
	/// <param name="ha"></param>
	/// <param name="args"></param>
	public delegate void Create_NamedHappenBuilder(Happen ha, string[] args);
	/// <summary>
	/// Delegate for including custom triggers. Can be directly attached to <see cref="EV_MakeNewTrigger"/>.
	/// </summary>
	/// <param name="name">Trigger name (id)</param>
	/// <param name="args">optional arguments.</param>
	/// <param name="rwg">Game instance.</param>
	/// <param name="ha">Happen to attach things to.</param>
	/// <returns>Child of <see cref="HappenTrigger"/> if subscriber wishes to claim the trigger; null if not.</returns>
	public delegate HappenTrigger? Create_RawTriggerFactory(string name, string[] args, RainWorldGame rwg, Happen ha);
	/// <summary>
	/// Delegate for registering named triggers.
	/// </summary>
	/// <param name="args">Trigger arguments.</param>
	/// <param name="rwg">Current RainWorldGame instance.</param>
	/// <param name="ha">Happen the trigger is to be attached to.</param>
	public delegate HappenTrigger? Create_NamedTriggerFactory(string[] args, RainWorldGame rwg, Happen ha);
	#endregion
	#region API proper
	/// <summary>
	/// Registers a named action. Multiple names. Up to one callback for every lifecycle event. No args support.
	/// </summary>
	/// <param name="names">Action's names. Case insensitive.</param>
	/// <param name="au">Abstract update callback.</param>
	/// <param name="ru">Realized update callback.</param>
	/// <param name="oi">Init callback.</param>
	/// <param name="cu">Core update callback.</param>
	/// <param name="ignoreCase">Whether action name matching should be case sensitive.</param>
	/// <returns>The number of name collisions encountered.</returns>
	public static int AddNamedAction(
		string[] names,
		lc_AbstractUpdate? au = null,
		lc_RealizedUpdate? ru = null,
		lc_Init? oi = null,
		lc_CoreUpdate? cu = null,
		bool ignoreCase = true)
	{
		return names
				.Select((name) => AddNamedAction(name, au, ru, oi, cu, ignoreCase) ? 0 : 1)
				.Aggregate((x, y) => x + y);
	}

	/// <summary>
	/// Registers a named action. One name. Up to one callback for every lifecycle event. No args support.
	/// </summary>
	/// <param name="name">Trigger name.</param>
	/// <param name="au">Abstract update callback.</param>
	/// <param name="ru">Realized update callback.</param>
	/// <param name="oi">Init callback.</param>
	/// <param name="cu">Core update callback.</param>
	/// <param name="ignoreCase">Whether action name matching should be case insensitive.</param>
	/// <returns>True if successfully registered; false if name already taken.</returns>
	public static bool AddNamedAction(
		string name,
		lc_AbstractUpdate? au = null,
		lc_RealizedUpdate? ru = null,
		lc_Init? oi = null,
		lc_CoreUpdate? cu = null,
		bool ignoreCase = true)
	{
		StringComparer? comp = ignoreCase ? StringComparer.CurrentCultureIgnoreCase : StringComparer.CurrentCulture;
		if (namedActions.ContainsKey(name)) { return false; }
		void newCb(Happen ha)
		{
			foreach (var ac in ha.actions.Keys)
			{
				if (comp.Compare(ac, name) == 0)
				{
					ha.On_AbstUpdate += au;
					ha.On_RealUpdate += ru;
					ha.On_Init += oi;
					ha.On_CoreUpdate += cu;
					return;
				}
			}
		}
		namedActions.Add(name, newCb);
		EV_MakeNewHappen += newCb;
		return true;
	}
	/// <summary>
	/// Registers a named action. Many names. Arbitrary Happen manipulation. Args support. 
	/// </summary>
	/// <param name="names">Action name(s). Case insensitive.</param>
	/// <param name="builder">User builder callback.</param>
	/// <param name="ignoreCase">Whether action name matching should be case sensitive.</param>
	/// <returns>Number of name collisions encountered.</returns>
	public static int AddNamedAction(
		string[] names,
		Create_NamedHappenBuilder builder,
		bool ignoreCase = true)
	{
		return names
				.Select((name) => AddNamedAction(name, builder, ignoreCase) ? 0 : 1)
				.Aggregate((x, y) => x + y);
	}

	/// <summary>
	/// Registers a named action. One name. Arbitrary Happen manipulation. Args support.
	/// </summary>
	/// <param name="name">Name of the action.</param>
	/// <param name="builder">User builder callback.</param>
	/// <param name="ignoreCase"></param>
	/// <returns>True if successfully added; false if already taken.</returns>
	public static bool AddNamedAction(
		string name,
		Create_NamedHappenBuilder builder,
		bool ignoreCase = true)
	{
		StringComparer? comp = ignoreCase ? StringComparer.CurrentCultureIgnoreCase : StringComparer.CurrentCulture;
		if (namedTriggers.ContainsKey(name)) { return false; }
		void newCb(Happen ha)
		{
			foreach (KeyValuePair<string, string[]> ac in ha.actions)
			{
				if (comp.Compare(ac.Key, name) == 0)
				{
					builder?.Invoke(ha, ac.Value);
				}
			}
		}
		namedActions.Add(name, newCb);
		EV_MakeNewHappen += newCb;
		return true;
	}
	/// <summary>
	/// Removes a named callback.
	/// </summary>
	/// <param name="action"></param>
	public static void RemoveNamedAction(string action)
	{
		if (!namedActions.ContainsKey(action)) return;
		EV_MakeNewHappen -= namedActions[action];
		namedActions.Remove(action);
	}
	/// <summary>
	/// Registers a named trigger. Multiple names.
	/// </summary>
	/// <param name="names">Trigger's name(s).</param>
	/// <param name="fac">User trigger factory callback.</param>
	/// <param name="ignoreCase">Whether trigger name should be case sensitive.</param>
	/// <returns>Number of name collisions encountered.</returns>
	public static int AddNamedTrigger(
		string[] names,
		Create_NamedTriggerFactory fac,
		bool ignoreCase = true)
	{
		return names
				.Select((name) => AddNamedTrigger(name, fac, ignoreCase) ? 0 : 1)
				.Aggregate((x, y) => x + y);
	}

	/// <summary>
	/// Registers a named trigger. Single name.
	/// </summary>
	/// <param name="name"></param>
	/// <param name="fac"></param>
	/// <param name="ignoreCase"></param>
	/// <returns></returns>
	public static bool AddNamedTrigger(
		string name,
		Create_NamedTriggerFactory fac,
		bool ignoreCase = true)
	{
		StringComparer? comp = ignoreCase ? StringComparer.CurrentCultureIgnoreCase : StringComparer.CurrentCulture;

		if (namedTriggers.ContainsKey(name)) { return false; }
		Create_RawTriggerFactory newCb = (n, args, rwg, ha) =>
		{
			if (comp.Compare(n, name) == 0) return fac(args, rwg, ha);
			return null;
		};
		namedTriggers.Add(name, newCb);
		EV_MakeNewTrigger += newCb;
		return true;
	}
	/// <summary>
	/// Removes a registered trigger by name.
	/// </summary>
	/// <param name="name"></param>
	public static void RemoveNamedTrigger(string name)
	{
		if (!namedTriggers.ContainsKey(name)) return;
		EV_MakeNewTrigger -= namedTriggers[name];
		namedTriggers.Remove(name);
	}
	/// <summary>
	/// Subscribe to this to attach your custom callbacks to newly created happen objects. You can also use <see cref="AddNamedAction(string, Create_NamedHappenBuilder, bool)"/> and <see cref="AddNamedAction(string, lc_AbstractUpdate?, lc_RealizedUpdate?, lc_Init?, lc_CoreUpdate?, bool)"/> as name-safe shorthands.
	/// </summary>
	public static event Create_RawHappenBuilder? EV_MakeNewHappen;
	internal static IEnumerable<Create_RawHappenBuilder?>? MNH_invl
		=> EV_MakeNewHappen?.GetInvocationList().Cast<Create_RawHappenBuilder?>();//.ToArray();
	/// <summary>
	/// Subscribe to this to dispense your custom triggers. You can also use <see cref="AddNamedTrigger(string, Create_NamedTriggerFactory, bool)"/> as a name-safe shorthand.
	/// </summary>
	public static event Create_RawTriggerFactory? EV_MakeNewTrigger;
	internal static IEnumerable<Create_RawTriggerFactory?>? MNT_invl
		=> EV_MakeNewTrigger?.GetInvocationList()?.Cast<Create_RawTriggerFactory?>();//.ToArray();
	#endregion
}
