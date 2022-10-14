# Atmo API overview

This file contains instructions on how to interface with Atmo API from other code mods.

## Naming

"Happens" are like *world events*. This name was picked to avoid confusion with vanilla's Events/Triggers.

## Classes
### [API](src/API.cs)
This static class is your primary entrypoint for interacting with Atmo. Relevant members:
#### Delegate types
Various delegates used by the API. See [their docstrings](src/API.cs#L17) for purpose and parameter descriptions.

#### Events

You can directly use `EV_MakeNewTrigger` and `EV_MakeNewHappen` to add your custom code for creating HappenTriggers and filling Happens respectively.

`EV_MakeNewTrigger` is invoked every time a condition expression in a `WHEN:` block has been parsed, and a Happen needs to turn a trigger name (such as `Always`, `OnKarma`, etc) into a trigger object. If any callback attached to it returns non-null, *invocation short-circuits, and all consequent callbacks are ignored*. If you are subscribing to this manually, make sure to check for trigger name, and return null if it is not yours.

`EV_MakeNewHappen` is invoked every time a happen has finished parsing, and needs to receive behaviour callbacks. You will typically be checking contents of `Happen.actions` (parsed from `WHERE:` blocks in a `.atmo` file) or `Happen.name` to see if your code should accept this specific callback.

Alternatively, use the following methods to register actions and trigger by preset name:

#### `AddNamedAction()` overloads
- `AddNamedAction(string, lc_AbstractUpdate?, lc_RealizedUpdate?, lc_Init?, lc_CoreUpdate?, bool)` registers up to one action for every lifecycle event. This shorthand does not support action parameters.
- `AddNamedAction(string, Create_NamedHappenBuilder, bool)` wraps your builder on a matching name. This shorthand supports action parameters.
You can remove wrapped named-builders added by this method *by name you provided* using `RemoveNamedAction(string)`.

#### `AddNamedTrigger(string, Create_NamedTriggerFactory, bool)`
This method registers a named trigger factory. You can remove trigger-factory *by name you provided* using `RemoveNamedTrigger(string)`.

### [Happen](src/Happen.cs)
This is the core class which the behaviours revolve around. Its relevant members:

- Lifecycle events: These are invoked on various stages of a Happen's lifetime.
	- `On_AbstUpdate`: as long as the Happen is *activated*, invoked for every affected room on each Abstract update. Receives abstract update step as parameter.
	- `On_RealUpdate`: as long as the Happen is *activated*, invoked for every affected room on each Realized update, that is: every frame.
	- `On_Init`: invoked *once* per cycle, right before the first time `On_AbstUpdate` or `On_RealUpdate` would be invoked. Receives `World` as parameter.
	- `On_CoreUpdate`: invoked *once* per frame *every frame*, no matter if the Happen is active or not. Useful if your happen needs to have a persistent frame counter of something.
- `name`: name of your Happen, as defined in `.atmo` file.
- `triggers`: an array of HappenTrigger objects for the current happen.
- `actions`: a dictionary of added actions with parameters.

You access `Happen`s from builders attached to `Atmo.API.EV_MakeNewHappen`, wrapped or not.

***Performance notice***: it is entirely on you to minimize time consumption for `On_RealUpdate`. Cache results when possible, avoid running LINQ chains in method bodies. You can use `Atmo.Atmod.inst.HappenSet.GetPerfRecords()` to collect some frame time data from current session. Note that this method is a yielder, and return should be consumed and discarded before the beginning of the next frame to avoid `InvalidOperationException`s.

### [HappenTrigger](src/HappenTrigger.cs)
This is an abstract class, representing a single activation condition from a `WHEN:` block inside a `.atmo` file. `HappenTrigger`'s children are to be instantiated by `Atmo.API.EV_MakeTrigger` subscribers.
HappenTrigger has a child class called `NeedsRWG`: derive from it if your trigger needs access to game state (such as checking whether a specific creature is spawned in the current region).
Relevant members:

- abstract `ShouldRunUpdates()`: Called *once, for every trigger instance, each frame*. Returns true if associated Happen should run its lifecycle events on current frame (except `On_CoreUpdate`, which will be ran regardless). Override this with your custom activation logic.
- virtual `Update()`: called *once, for every trigger instance, each frame*. Override this if your trigger should change with time (such as turning on and off in consistent intervals).

***Performance notice***: it is entirely on you to minimize time consumption for either of these overrides. Cache results when possible, avoid running LINQ chains in method bodies. You can use `Atmo.Atmod.inst.HappenSet.GetPerfRecords()` to collect some frame time data from current session. Note that this method is a yielder, and return should be consumed and discarded before the beginning of the next frame to avoid `InvalidOperationException`s.

### [Atmod](src/Atmod.cs)
Main plugin class for Atmo. You can access its static singlet, as well as some other members.

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
