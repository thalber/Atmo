# Atmo API overview

This file contains instructions on how to interface with Atmo API from other code mods, and description of all central classes.

## Naming

"Happens" are like *world events*. This name was picked to avoid confusion with vanilla's Events/Triggers.


## [Versioned API classes](../src/API)

`Atmo.API` namespace is your primary way of interacting with the mod.

The API may be segregated by versions: Each version has its own class (Atmo.API.V0.cs, Atmo.API.V1.cs and so on). API versions may be modified incrementally post release (adding new members), but any breaking changes will instead be forwarded to a new version. Any API classes will link to the same backend and, ideally, work together in arbitrary combinations.

### Delegate types
Various general purpose delegates used by the API are in [this file](../src/API/Delegates.cs). See their docstrings for purpose and parameter descriptions. Their names are prefixed with API version they were made for.

### [V0](../src/API/V0.cs)

This is the initial release (1.0) version.

#### Events

You can directly use `EV_MakeNewTrigger` and `EV_MakeNewHappen` to add your custom code for creating HappenTriggers and filling Happens respectively.

`EV_MakeNewTrigger` is invoked every time a condition expression in a `WHEN:` block has been parsed, and a Happen needs to turn a trigger name (such as `Always`, `OnKarma`, etc) into a trigger object. If any callback attached to it returns non-null, *invocation short-circuits, and all consequent callbacks are ignored*. If you are subscribing to this manually, make sure to check for trigger name, and return null if it is not yours.

`EV_MakeNewHappen` is invoked every time a happen has finished parsing, and needs to receive behaviour callbacks. You will typically be checking contents of `Happen.actions` (parsed from `WHAT:` blocks in a `.atmo` file) or `Happen.name` to see if your code should accept this specific callback.

Alternatively, use the following methods to register actions and trigger by preset name:

#### `AddNamedAction()` overloads

| Header	| Function	| 
| ---		| ---		|
|  `AddNamedAction(string, V0_lc_AbstractUpdate?, V0_lc_RealizedUpdate?, V0_lc_Init?, V0_lc_CoreUpdate?, bool)` | One action name. Up to one action for every lifecycle event. Does not support action parameters. |
| `AddNamedAction(string[], V0_lc_AbstractUpdate?, V0_lc_RealizedUpdate?, V0_lc_Init?, V0_lc_CoreUpdate?, bool)` | Multiple action names. Up to one action for every lifecycle event. Does not support action parameters. |
| `AddNamedAction(string, Create_NamedHappenBuilder, bool)` | One action name. This shorthand supports action arguments (passed as the second parameter to `builder`). |
| `AddNamedAction(string[], Create_NamedHappenBuilder, bool)` | Multiple action names. Supports action arguments (passed as the second parameter to `builder`) |

You can remove wrapped named-builders added by this method *by name you provided* using `RemoveNamedAction(string)`.
Arguments are passed as ArgSet objects. For more information on this class, see [its docstrings](../src/Data/ArgSet.cs).

#### `AddNamedTrigger` overloads
| Header	| Function	|
| ---		| --- 		|
| `AddNamedTrigger(string, Create_NamedTriggerFactory, bool)` | One name. Trigger supports arguments. |
| `AddNamedTrigger(string[], Create_NamedTriggerFactory, bool)` | Multiple names. Trigger supports arguments. |

You can remove trigger-factories *by name you provided* using `RemoveNamedTrigger(string)`.

## Persistent classes

These are classes that are accessible but not a part of the versioned API. Their contents have a slightly weaker guarantee of persistence (we will try our best to not break them).

### [Happen](../src/Body/Happen.cs)
This is the core class which the behaviours revolve around. Happens contain:
- Behaviour, in form of delegates/callbacks attached to the following events:
	- `On_AbstUpdate`: as long as the Happen is *activated*, invoked for every affected room on each Abstract update. Receives abstract update step as parameter.
	- `On_RealUpdate`: as long as the Happen is *activated*, invoked for every affected room on each Realized update, that is: every frame.
	- `On_Init`: invoked *once* per cycle, right before the first time `On_AbstUpdate` or `On_RealUpdate` would be invoked. Receives `World` as parameter.
	- `On_CoreUpdate`: invoked *once* per frame *every frame*, no matter if the Happen is active or not. Useful if your happen needs to have a persistent frame counter of something.
- Activation conditions (in form of one or more [HappenTrigger](../src/Body/HappenTrigger.cs) objects) in `triggers` field: an array of HappenTrigger objects for the current happen. They are evaluated through a [PredicateInlay structure](../modules/PredicateInlay/src/PredicateInlay.cs).
- `name`: name of your Happen, as defined in `.atmo` file.
- `actions`: a dictionary of added actions with parameters.

You access `Happen`s from builder callbacks attached to `Atmo.API.EV_MakeNewHappen`, wrapped or not. You can also use `Atmod.CurrentSet` field to access current instance of [HappenSet](../src/Body/HappenSet.cs), and look up individual happens from there, although this is not recommended.

***Performance notice***: it is entirely on you to minimize time consumption for `On_RealUpdate`. Cache results when possible, avoid running LINQ chains in method bodies. You can use `Atmo.Atmod.inst.HappenSet.GetPerfRecords()` to collect some frame time data from current session. Note that this method is a yielder, and return should be consumed and discarded before the beginning of the next frame to avoid `InvalidOperationException`s.

### [HappenTrigger](../src/Body/HappenTrigger.cs)
This is an abstract class, representing a single activation condition from a `WHEN:` block inside a `.atmo` file. `HappenTrigger`'s children are to be instantiated by `Atmo.API.EV_MakeTrigger` subscribers.
HappenTrigger has a child class called `NeedsRWG`: derive from it if your trigger needs access to game state (such as checking whether a specific creature is spawned in the current region).
Relevant members:

- abstract `ShouldRunUpdates()`: Called *once, for every trigger instance, each frame*. Returns true if associated Happen should run its lifecycle events on current frame (except `On_CoreUpdate`, which will be ran regardless). Override this with your custom activation logic.
- virtual `Update()`: called *once, for every trigger instance, each frame*. Override this if your trigger should change with time (such as turning on and off in consistent intervals).

**Alternative to inheritance**: If you don't like reflection, you may use `HappenTrigger.EventfulTrigger` child. Example use:
```cs
int x = 6;
int c = 0;
return new EventfulTrigger()
{
	// this instance will roll a dice every frame
	On_Update = 
		() => { c = UnityEngine.Random.Range(0, x); },
	// it will be active every frame it rolled 0 (1 in 6 chance)
	On_ShouldRunUpdates =
		() => c is 0,
};

```

***Performance notice***: it is entirely on you to minimize time consumption for either of these overrides. Cache results when possible, avoid running LINQ chains in method bodies. You can use `Atmo.Atmod.inst.HappenSet.GetPerfRecords()` to collect some frame time data from current session. Note that this method is a yielder, and return should be consumed and discarded before the beginning of the next frame to avoid `InvalidOperationException`s.

### [HappenSet](../src/Body/HappenSet.cs)

These represent the link betweem happens and rooms they have been assigned to. Following members are involved in grouping:

| Member	| Description	|
| ---		| ---			|
| `RoomsToGroups` | Links between rooms (left pool) and room groups (right pool). Includes subregions. |
| `GroupsToHappens` | Links between groups (left pool) and happens (right pool). |
| `ExcludeToHappen` | Links between rooms that should be excluded (left pool) and happens (right pool). In excluded rooms, a happen will be ignored even if some of its groups have the room. |
| `IncludeToHappen` | Links between rooms that should be force included (left pool) and happens (right pool). In force included rooms, a happen will be active even if none of its groups have the room. |  
| `AllHappens` | Contains a list of all owned happens. |

While you can manipulate all these manually, it is error prone. Instead, several wrapper methods exist:

| Method	| Description	|
| ---		| ---			|
| `InsertGroup(string, IEnumerable<string>)` | Registers a group, linking given room names to group name. |
| `InsertGroups(IDictionary<string, IEnumerable<string>>)` | Same thing, but for multiple groups at once, in a dictionary format. |
| `InsertHappens` | Adds one or more happens to a set. If for whatever reason you are constructing a new happen yourself, make sure to call this before any following methods: |
| `AddGrouping(Happen, IEnumerable<string>)` | Adds a link between a happen and one or more group. |
| `AddExcludes(Happen, IEnumerable<string>)` | Adds excludes to a happen. |
| `AddIncludes(Happen, IEnumerable<string>)` | Adds includes to a happen. |

You can use these methods to see where your happens are active:

| Method	| Description	|
| ---		| ---			|
| `GetHappensForRoom(string)` | Gets all happens that should be active in a given room. |
| `GetRoomsForHappen(Happen)` | Gets all room names for a given happen. |

Other members:

| Member	| Description	|
| ---		| ---			|
| `game`, `world` | Game state references |
| `GetPerfRecords()` | Fetches a struct carrying frame time reports from last several seconds. |

### [Atmod](../src/Atmod.cs)
Main plugin class for Atmo. You can access its static singlet, as well as some other members.

### [VarRegistry](../src/Helpers/VarRegistry.cs)

This class handles variable access and serialization. To interact with any fields using `Atmo.Helpers.Utils.VT<int, int>` here, use `VarRegistry.MakeSD` to construct valid instances (hash codes for keying won't work otherwise)

| Member	| Description	|
| ---		| ---			|
| `VarsPerSave` | Contains one [VarSet](../src/Helpers/VarSet) for every pair of "saveslot-character" values. These carry normal and persistent variables. |
| `VarsGlobal` | Contains a dictionary, keyed by saveslot index, with values being global variable dictionary for that saveslot. | 
| `VarsVolatile` | A singular set of variables that are reset upon quitting the game, and are shared between all saveslots and all characters. |
| `GetVar(string, int, int)` | Attempts fetching a variable by name with prefixes, for given saveslot and character. |


## Usage tips

For simple behaviours, you can use lambdas/closures for easily sharing data. In the following example, two closures in the same scope capture two local variables (`counter` and `cd`) and freely use them both later.

```cs
API.AddNamedAction("stun", (Happen ha, string[] args) =>
{
    int.TryParse(args.AtOr(0, "200"), out var cd);
    int counter = 0;
    ha.On_CoreUpdate += (RainWorldGame rwg) =>
    {
        if (counter < 0) counter = cd; else counter--;
    };
    ha.On_RealUpdate += (Room rm) =>
    {
        if (counter != 0) return;
        foreach (var uad in rm.updateList) 
            if (uad is Creature c) 
                c.Stun(10);
    };
});
```
If lambdas sound unfamiliar or confusing to you, see [C# docs on them](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/lambda-expressions#capture-of-outer-variables-and-variable-scope-in-lambda-expressions).
