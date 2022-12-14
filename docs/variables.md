

# Variable system

Atmo features a variable system, which allows you to read and write data from a shared registry.

You can use variables anywhere an action or a trigger expects an argument: for example, `log 'sev=Warning' 'abst=$time'` will repeatedly print current time to the console. Unlike a simple argument, *a variable's value can change at any time*. For example, `$time` is updated every frame.

Variables are separated into several categories, listed below:

| Category		| Prefix	| Description	| Saved to disk	| Can be changed by the user	|
| --- 			| ---		| --- 			| ---			| ---				|
| Normal		| none		| Separate set for each saveslot and each character; progress reset to last successful save on death. | Yes | Yes |
| Persistent	| `p_`		| Separate set for each saveslot and each character; persist between deaths, much like information about ascention or visited echoes. | Yes | Yes |
| Global		| `g_`		| Separate set for each saveslot. Can only be erased by erasing the entire saveslot. | Yes | Yes |
| Volatile 		| `v_`		| Single global set. Not saved to disk and not separated by saveslots. | No | Yes |
| Special		| special names | Built-in variables for various technical purposes. They occupy a few preset names, listed in another table below. They're not specific to any saveslot or character. | No | No |

Set prefix is always required. Prefixes are not considered part of the variable name (variables `var1` and `p_var1` are two distinct variables with the same name, but from different sets).

## Special variables

This is the list of all special variables provided by atmo. Their names are reserved and cannot be assigned to.

| names	| function	|
| --- 	| ---		|
| root, rootfolder | game root folder path |
| now, time | current time. Not precise, since it is only updated once a frame |
| utcnow, utctime | current UTC time. Not precise, since it is only updated once a frame |
| version, atmover | Atmo current version |
| cycletime | current cycle's max length, in seconds. |
| os | current OS. Theoretically possible values: Win32S, Win32Windows, Win32NT, WinCE, Unix, Xbox, MacOSX. |
| realm | Whether the game is running Realm |
| memoryused, memused | Current size of used memory in bytes. Can only be fetched as string |

## Metafunctions

Metafunctions are specialized variables that can receive additional input. They are used in form of `'$$FUNCNAME input'` anywhere a normal argument or variable could. Typically, type coercion is not available for metafunctions. **NOTE:** the first dollar sign is *not* part of a metafunction name. If some metafunction `FUNC` can be assigned to with `setvar` action, you would need to specify it as `setvar '$FUNC input' 'value'`.

You can see full list of provided metafunctions in [builtins list](builtins.md)

## Code reference

For accessing variables from other c# code, see [VarRegistry file and its docstrings](../src/Data/VarRegistry.cs), or [this section for special variables](../src/Data/VarRegistry.Specials.cs).
