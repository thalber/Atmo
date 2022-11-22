using Atmo.Body;
using Atmo.Data;

namespace Atmo;
#pragma warning disable CS0419 // Ambiguous reference in cref attribute
/// <summary>
/// Static class for user API. You will likely be interacting with the mod from here.
/// There are several ways you may interact with it:
/// <list type="bullet">
/// <item><see cref="AddNamedAction"/> overloads: attach behaviour to happens that have a specific action name in their WHAT clause;</item>
/// <item><see cref="AddNamedTrigger"/> overloads: attach a factory callback that creates a trigger that matches specified name(s);</item>
/// <item><see cref="EV_MakeNewHappen"/> and <see cref="EV_MakeNewTrigger"/> events: directly attach callbacks without builtin name checking.</item>
/// </list>
/// See also: <seealso cref="Happen"/> for core lifecycle logic, <seealso cref="HappenTrigger"/> for how conditions work, <seealso cref="HappenSet"/> for additional info on how happens are composed.
/// </summary>
public static class API
{
	#region fields
	internal static readonly Dictionary<string, Create_RawHappenBuilder> namedActions = new();
	internal static readonly Dictionary<string, Create_RawTriggerFactory> namedTriggers = new();
	internal static readonly Dictionary<string, Create_RawMacroHandler> namedMacros = new();
	#endregion
	#region dels
	/// <summary>
	/// Delegates for happens' abstract updates.
	/// </summary>
	/// <param name="absroom">Abstract room the update is happening in.</param>
	/// <param name="time">Abstract update step, in frames.</param>
	public delegate void lc_AbstractUpdate(AbstractRoom absroom, int time);
	/// <summary>
	/// Delegate for happens' realized updates.
	/// </summary>
	/// <param name="room">The room update is happening in.</param>
	public delegate void lc_RealizedUpdate(Room room);
	/// <summary>
	/// Delegate for being called on first abstract update
	/// </summary>
	/// <param name="world"></param>
	public delegate void lc_Init(World world);
	/// <summary>
	/// Delegate for happens' init call.
	/// </summary>
	/// <param name="rwg">Current instance of <see cref="RainWorldGame"/>. Always a Story session.</param>
	public delegate void lc_CoreUpdate(RainWorldGame rwg);
	/// <summary>
	/// Callback for attaching custom behaviour to happens. Can be directly attached to <see cref="EV_MakeNewHappen"/>
	/// </summary>
	/// <param name="happen">Happen that needs lifetime callbacks attached. Check its instance members to see if your code should affect it, and use its instance events to attach behaviour.</param>
	public delegate void Create_RawHappenBuilder(Happen happen);
	/// <summary>
	/// Delegate for registering named actions.
	/// Used by <see cref="AddNamedAction(string, Create_NamedHappenBuilder, bool)"/>.
	/// </summary>
	/// <param name="happen">Happen that needs lifetime callbacks attached. One of the its <see cref="Happen.actions"/> has a name you selected. Use its instance events to attach behaviour.</param>
	/// <param name="args">The event's arguments, taking from a WHAT: clause.</param>
	public delegate void Create_NamedHappenBuilder(Happen happen, ArgSet args);
	/// <summary>
	/// Delegate for including custom triggers. Can be directly attached to <see cref="EV_MakeNewTrigger"/>. Make sure to check the first parameter (name) and see if it is fitting.
	/// </summary>
	/// <param name="name">Trigger name (id).</param>
	/// <param name="args">A set of (usually optional) arguments.</param>
	/// <param name="game">Current game instance.</param>
	/// <param name="happen">Happen to attach things to.</param>
	/// <returns>Child of <see cref="HappenTrigger"/> if subscriber wishes to claim the trigger; null if not.</returns>
	public delegate HappenTrigger? Create_RawTriggerFactory(string name, ArgSet args, RainWorldGame game, Happen happen);
	/// <summary>
	/// Delegate for registering named triggers. Used by <see cref="AddNamedTrigger"/> overloads.
	/// </summary>
	/// <param name="args">Trigger arguments.</param>
	/// <param name="game">Current RainWorldGame instance.</param>
	/// <param name="happen">Happen the trigger is to be attached to.</param>
	public delegate HappenTrigger? Create_NamedTriggerFactory(ArgSet args, RainWorldGame game, Happen happen);
	/// <summary>
	/// Delegate for registering macro-variables, for use in <see cref="VarRegistry.GetVar(string, int, int)"/>. Can be directly attached to <see cref="EV_ApplyMacro"/>. Make sure to check the first parameter (name) and see if it is fitting.
	/// </summary>
	/// <param name="name">Supposed name of the macro.</param>
	/// <param name="value">Body text passed to the macro.</param>
	/// <param name="saveslot">Current saveslot number.</param>
	/// <param name="character">Current character number.</param>
	/// <returns><see cref="IArgPayload"/> object linking to macro's output; null if name does not fit or there was an error.</returns>
	public delegate IArgPayload? Create_RawMacroHandler(string name, string value, int saveslot, int character);
	/// <summary>
	/// Delegate for registering named macros. Used by <see cref="AddNamedMacro"/> overloads.
	/// </summary>
	/// <param name="value">Body text passed to the macro.</param>
	/// <param name="saveslot">Current saveslot number.</param>
	/// <param name="character">Current character number.</param>
	/// <returns><see cref="IArgPayload"/> object linking to macro's output; null if there was an error.</returns>
	public delegate IArgPayload? Create_NamedMacroHandler(string value, int saveslot, int character);
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
			foreach (string? ac in ha.actions.Keys)
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
		if (TXT.Regex.Match(name, "\\w+").Length != name.Length)
		{
			plog.LogWarning($"Invalid action name: {name}");
			return false;
		}
		if (namedTriggers.ContainsKey(name)) { return false; }
		StringComparer? comp = ignoreCase ? StringComparer.CurrentCultureIgnoreCase : StringComparer.CurrentCulture;
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
		if (!namedActions.TryGetValue(action, out Create_RawHappenBuilder? builder)) return;
		EV_MakeNewHappen -= builder;
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
				.Select((name) => AddNamedTrigger(name, fac, ignoreCase) ? 1 : 0)
				.Aggregate((x, y) => x + y);
	}
	/// <summary>
	/// Registers a named trigger. Single name.
	/// </summary>
	/// <param name="name">Name of the trigger.</param>
	/// <param name="fac">User factory callback.</param>
	/// <param name="ignoreCase">Whether name matching should be case insensitive.</param>
	/// <returns></returns>
	public static bool AddNamedTrigger(
		string name,
		Create_NamedTriggerFactory fac,
		bool ignoreCase = true)
	{
		if (TXT.Regex.Match(name, "\\w+").Length != name.Length)
		{
			plog.LogWarning($"Invalid trigger name: {name}");
			return false;
		}
		if (namedTriggers.ContainsKey(name)) { return false; }
		StringComparer? comp = ignoreCase ? StringComparer.CurrentCultureIgnoreCase : StringComparer.CurrentCulture;

		HappenTrigger? newCb(string n, ArgSet args, RainWorldGame rwg, Happen ha)
		{
			if (comp.Compare(n, name) == 0) return fac(args, rwg, ha);
			return null;
		}
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
		if (!namedTriggers.TryGetValue(name, out Create_RawTriggerFactory? fac)) return;
		EV_MakeNewTrigger -= fac;
		namedTriggers.Remove(name);
	}
	/// <summary>
	/// Registers a macro with a given set of names.
	/// </summary>
	/// <param name="names">Array of names for the macro.</param>
	/// <param name="handler">User handler callback.</param>
	/// <param name="ignoreCase">Whether name matching should be case insensitive.</param>
	/// <returns>Number of errors and name collisions encountered.</returns>
	public static int AddNamedMacro(
		string[] names,
		Create_NamedMacroHandler handler,
		bool ignoreCase = true)
		=> names.Select((name) => AddNamedMacro(name, handler, ignoreCase) ? 0 : 1).Aggregate((x, y) => x + y);
	/// <summary>
	/// Registers a macro with a given single name.
	/// </summary>
	/// <param name="name">Name of the macro.</param>
	/// <param name="handler">User handler callback.</param>
	/// <param name="ignoreCase">Whether macro name matching should be case insensitive.</param>
	/// <returns>True if successfully attached; false otherwise.</returns>
	public static bool AddNamedMacro(
		string name,
		Create_NamedMacroHandler handler,
		bool ignoreCase = true)
	{
		if (TXT.Regex.Match(name, "\\w+").Length != name.Length)
		{
			plog.LogWarning($"Invalid macro name: {name}");
			return false;
		}
		if (namedMacros.ContainsKey(name)) return false;
		StringComparer comp = ignoreCase ? StringComparer.CurrentCultureIgnoreCase : StringComparer.CurrentCulture;
		IArgPayload? newCb(string n, string val, int ss, int ch)
		{
			if (comp.Compare(n, name) == 0) return handler(val, ss, ch);
			return null;
		}
		EV_ApplyMacro += newCb;
		namedMacros.Add(name, newCb);
		return true;
	}
	/// <summary>
	/// Clears a macro name binding.
	/// </summary>
	/// <param name="name"></param>
	public static void RemoveNamedMacro(string name)
	{
		if (!namedMacros.TryGetValue(name, out Create_RawMacroHandler? handler)) return;
		EV_ApplyMacro -= handler;
		namedMacros.Remove(name);
	}
	/// <summary>
	/// Subscribe to this to attach your custom callbacks to newly created happen objects.
	/// You can also use <see cref="AddNamedAction"/> overloads as name-safe wrappers.
	/// </summary>
	public static event Create_RawHappenBuilder? EV_MakeNewHappen;
	internal static IEnumerable<Create_RawHappenBuilder?>? MNH_invl
		=> EV_MakeNewHappen?.GetInvocationList().Cast<Create_RawHappenBuilder?>();
	/// <summary>
	/// Subscribe to this to dispense your custom triggers.
	/// You can also use <see cref="AddNamedTrigger"/> overloads as a name-safe wrappers.
	/// </summary>
	public static event Create_RawTriggerFactory? EV_MakeNewTrigger;
	internal static IEnumerable<Create_RawTriggerFactory?>? MNT_invl
		=> EV_MakeNewTrigger?.GetInvocationList()?.Cast<Create_RawTriggerFactory?>();
	/// <summary>
	/// Subscribe to this to register custom variables-macros. You can also use <see cref="AddNamedMacro"/> overloads as name-safe wrappers.
	/// </summary>
	public static event Create_RawMacroHandler? EV_ApplyMacro;
	internal static IEnumerable<Create_RawMacroHandler?>? AM_invl
		=> EV_ApplyMacro?.GetInvocationList()?.Cast<Create_RawMacroHandler?>();
#pragma warning restore CS0419 // Ambiguous reference in cref attribute
	#endregion
}
