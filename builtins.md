# Builtin list

This document describes all actions and triggers you can use out of the box. Most of them can receive arguments, and to indicate how they are expected to be used, I mark them in tables as following:

| Syntax	| Description	|
| ---		| ---			|
| `1:duration` | the argument `duration` is positional (as in, expected by the action/trigger to always be the first, second, third etc in line)|
| `d:duration` | the argument `duration` is *named*, and is expected to be used like this: `'d=value'`, with `d` being the label and `value` being the value you can put in the atmo file. |
| `d/dur:duration`	| the argument `duration` is *named*, and can go under one of several names. Here, both `'d=value'` and `'dur=value'` would work.
| `1:duration?` | any argument where name is postfixed with question mark is *optional* and can be omitted.|
| `1:duration?=15`	| argument `duration` is *optional*, and, if you omit it, will have value `15`. |
| `1:duration$`| if the argument is postfixed with the dollar sign, *you can use a variable for one of the arguments, and it will handle value changes appropriately*. For all other arguments, you can still use a variable, but its value will only be read once and will not be updated in real time. For example, you can use  `log` action like this: `log 'abst=$time'`, and it will print current value of `time` variable to the console every abstract update. More info about variables in [this reference](variables.md). |
| `3..:options...` | Indicates that `options` is actually multiple arguments, and any arguments at position 3 and further will be treated as one of them. Consider this example action, that has `3..:options...`, as shown in the cell on the left: `action 'arg1' 'arg2' 10 15 999 'extra'` - here 10, 15, 999 and `'extra'` are all part of `options`. If an argument like this is marked as optional, it is legal to leave it empty. |

If an action or a trigger can receive several mutually exclusive sets of arguments, they are listed under separate rows in the table.

It is encouraged that whoever adds new behaviours, documents argument use in the same way.

## List of default triggers

| Action's names	| Arguments	| Desription 	| Example use	|
| --- 				| --- 		| ---			| --- 			|
| always			| none		| Always active | `always`		|
| untilrain, beforerain | `1:delay?=0` | Works until the rain hits, with optional additional `delay` in seconds. (can be negative to stop earlier) | `untilrain` - starts right after rain, `untilrain 10` - starts 10 seconds after rain |
| afterrain | `1:delay?=0` | Works after the rain hits, with optional additional `delay` in seconds (can be negative to start earlier) | `afterrain`, `afterrain -20` |
| everyx, every | `1:x` | Activates for one frame every `x` seconds. | `every 5` - once every five seconds | 
| maybe, chance | `1:chance?=0.5` | Activates for a cycle with a `chance` (0 for 0% chance, 1 for 100%). | `maybe 0.7` - 70% chance |
| flicker | `1:minOn?=5`, `2:maxOn?=5`, `3:minOff?=5`, `4:maxOff?=5`, `5:startEnabled?=true` | Flickers on and off periodically. Each time, stays on for a random number of seconds betweem `minOn` and `maxOn`, then turns off for a random number of seconds between `minOff` and `maxOff`, and repeats this endlessly. `startEnabled` sets whether the trigger should start active. | `flicker 5 7 20 25 yes` - uptime 5-7 seconds, downtime 20-25 seconds, starts enabled |
| karma, onkarma | `1..:levels...` | Active when player is on one of provided karma `levels`. Levels can be given as numbers (1, 2, 10), and optionally in form of `3-7` (accepts all values between 3 and 7, both ends included. this syntax is action-specific) | `karma 10` - activates on max karma, `karma 1-5 8` - activates on karma 1, 2, 3, 4, 5 and 8 |
| visit, playervisit, playervisited | `1..:rooms...` | Activates once any player enters one of the provided rooms. | `visit 'SU_C04' 'GATE_SU_HU'` - activates after player visits `SU_C04` or the SU-HI gate |
| fry, fryafter | `1:limit?=10`, `2:cooldown?=15` | On by default, but if the happen this trigger is attached to stays active for `limit` consecutive seconds, disables itself for `cooldown` seconds. | `fry 5 25` - can tolerate 5 seconds of uptime, disables itself for 25 seconds |
| after, afterother | `1:name`, `2:delay` | Looks up a given happen by `name`, and repeats its state, with a set `delay` in seconds. | `after happen2 10` |
| delay, ondelay | `1:delay` | Activates after a specified `delay` in seconds. | `delay 60` - starts after a minute |
| delay, ondelay | `1:minDelay`, `2:maxDelay` | Activates after a delay randomly picked between `minDelay` and `maxDelay`. | `delay 60 '$cycletime'` (this picks a delay somewhere between 1 minute after starting and the end of the cycle.) |
| playercount | `1..:counts...` | Active if player `counts` contains current player count. | `playercount 1 2 4` - active if session has 1, 2 or 4 players. |
| difficulty, ondifficulty, campaign | `1..:difficulties...` | Active on given `difficulties`. | `difficulty 'Survivor'` - active for survivor, `difficulty 0 1` - active for survivor and monk |
| vareq, varequal, variableeq | `1:varname`, `2:value` | Active if variable `varname`'s string value is equal to `value`. NOTE: make sure you write your target variable's name without a dollar sign! | `vareq 'OS' 'Unix'` - active if user is on a Unix machine |
| varne, varnot, varnotequal | `1:varname`, `2:value` | Active if variable `varname`'s string value is not equal to `value`. NOTE: make sure you write your target variable's name without a dollar sign! | `varne 'OS' 'Unix'` - active if user is not on Unix |
| varmatch, variablematch, varregex | `1:varname`, `2:pattern` | Active if regular expression `pattern` matches `varname`. NOTE: using this directly from atmo files may occasionally be problematic, since string literals in .atmo files can't directly contain singlequotes. You can use `\q` to escape a singlequote, but escapes are currently unfinished. | `varmatch 'OS' 'Win.+'` - active if user is on any Windows platform |

## List of default actions

| Names	| Arguments	| Description	| Example use	|
| ---	| ---		| --- 			| --- 			|
| fling | `1:velocity$`, `filter/select:filter?=.*$`, `var/variance:variance?=0$`, `spread/dev/deviation:deviation?=0$` | Applies `velocity` to all objects selected by `filter` (filter is treated as a regular expression, defaults to `.*`, which matches everything). `velocity` is treated as a vector: first component is horizontal force (positive is right), second component is vertical force (positive is up). `velocity` can additionally be modified by `variance` (randomizes pull force per object, 0 means acceleration stays unmodified, 1 means acceleration is spread from 0% to 200% compared to the original) and `deviation` (randomizes pull angle, in degrees, both directions) | `fling '0.1;0.8' 'filter=Creature'` - Accelerates all creatures slightly to the right and noticeably upwards, `fling '-0.3;0' 'filter=Swarmer'` - drags all neurons (moon's, pebbles' and hunter's) to the left, `fling '0; 0.8' 'filter=rock\|spear' 'var=0.5' 'dev=15'` - selects rocks and spears, accelerates them upwards (with 15 degree spread left and right, acceleration ranges from 0.4 to 1.2) |
| sound, playsound | `1:soundid$`, `cd/cooldown:cooldown?=2`, `lim/limit:limit?=2147483647`, `vol/volume:volume?=0.5$`, `pitch:pitch?=1$` | Plays a sound under ID `soundid` repeatedly while active. `cooldown` is the delay between individual plays, `limit` is how many times the sound is allowed to play, `vol` is volume (1 is normal). | `sound 'HUD_Karma_Reinforce_Bump' 'vol=10f' 'pitch=0.3f'` - permanently damages the user's hearing |
| soundloop | `1:soundid$`, `lim/limit:limit?=2147483647`, `vol/volume:volume?=0.5$`, `pitch:pitch?=1$`, `pan:pan?=0$`, `lim/limit:limit?=[infinity]` | plays `soundid` as a loop in all affected rooms while active. `limit` is how many total seconds the sound is allowed to play, `vol` is volume (1 is normal), `pan` is panning (-1 for left ear, 1 for right, 0 for centered). | `soundloop 'Zapper_LOOP' 'pitch=2' 'pan=1'` - plays an SS zapper loop on high pitch in the user's right ear. |
| rumble, screenshake | `int/intensity:intensity?=1$`, `shake:shake?=0.5$` | Shakes the screen. | `rumble 'shake=0.9'` - severe screen shake |
| karma, setkarma | `1:level` | Sets player's current karma to `level`. `level` can also be in format `add=value` (works with `add`,`+`) or `substract=value` (works with `sub`, `substract`, `-`), to increase or decrease karma level relative to current level. | `karma 3` - sets to 3, `karma 'add=1'` - adds 1 |
| karmacap, maxkarma, setmaxkarma | `1:cap` | Sets player's karma cap to `cap`. `cap` can also be in format `add=value` (works with `add`,`+`) or `substract=value` (works with `sub`, `substract`, `-`), to increase or decrease max karma level relative to current level. | `maxkarma=9` - sets to 9, `maxkarma '-=2'` - reduces by 2 |
| log, message | `sev/severity:severity?=Debug$`, `init/oninit:init?$`, `abst/abstup:abst?$` | Writes a message to bepInEx log, in format of `name:"message"` for message provided by `init` (written once, when the happen is first activated), and `name:"message":frames` for message provided by `abst` (written every few frames, for every affected room). `severity` is the message's importance; it can be "Debug", "Info", "Message", "Warning", "Error" and "Fatal" (debug/info may not be written to console or log file by default, depending on user settings!) | `log 'sev=Fatal' 'abst=skill issue'` - writes "skill issue" to console. |
| mark, themark, setmark | `1:value?=true` | sets the player's communication mark state to `value`. | `setmark` - giveth, `setmark 0`/`setmark false` - taketh away |
| glow, theglow, setglow | `1:value?=true` | sets the player's glow state to `value`. | `glow` - giveth, `glow 0` - taketh away. |
| raintimer, cycleclock, setclock | `1:value` | sets current cycle timer when activated. | `setclock '$cycletime'` - starts the rain, `setclock 0` - resets the clock |
| palette, changepalette | `1:palette$` | Sets affected rooms' main palette to `palette`. | `palette 15` |
| setvar, setvariable | `1:varname`, `2:value` | Sets variable `varname` to `value`. NOTE: make sure you write your target variable's name without a dollar sign! | `setvar 'myThing' '$utctime'` - sets `myThing` variable to current UTC time, `setvar '$myThing' '$utctime'` - gets variable `myThing`, *takes its string value, uses that value as a string to look up another variable, and sets that variable to current UTC time*.
