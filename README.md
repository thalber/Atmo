# Atmo
 
Atmo is a Rain World mod that acts as a regpack-centric scripting addon for [RegionKit](https://github.com/DryCryCrystal/Region-Kit). It allows a region maker to add world events, easily bundling behaviour with custom trigger conditions.

## Dependencies

- Custom Regions Support.

## Usage

Atmo searches regionpack subfolders `[regpack]/World/[region]/[region-acronym].atmo` for specialized config files. For example, if Better Shelters had a .atmo file for Outskirts, it would be put into `Rain World/Mods/CustomResources/BetterShelters/World/SU/SU.atmo`.

`.atmo` files should be plaintext files encoded in UTF-8.

A shorter example of how an atmo script file could look:

```
GROUP: group1
SU_S01 SU_S03 SU_S04
END GROUP

HAPPEN: ReduceGravity
WHAT: fling '0;0.5'
WHERE: group1
WHEN: always
```

This would reduce gravity in all shelters in Outskirts.

Full file format example (with comments) can be found [here](syntax.txt). If you are using Notepad++, there is a User-Defined Language config for it in this repo, which gives you syntax highlighting (for [dark mode](extras/atmoscript.udl.xml) and [light mode](extras/atmoscript.lightmode.udl.xml)).

A happen block can have multiple `WHAT:` and `WHERE:` clauses, but *only one* `WHEN:` clause.
Inside each `WHERE:` clause line, parsing starts with reading *group names*. After you've switched to included or excluded rooms by using a `+`/`-` separator, you can switch back to groups by using a `=` separator.

## Actions

Actions are the effects your happens (events) will produce when activated. A happen can have multiple actions. Default action names are case insensitive. Actions can receive parameters.

For a list of actions you can use out of the box, see [this reference](builtins.md).

## Triggers

Triggers are conditions that determine *when* a happen, with all its actions, is activated. Triggers can be grouped together using boolean logic operators (details in syntax example).

For a list of triggers you can use out of the box, see [this reference](builtins.md).

## Custom behaviours

You can register your own triggers and behaviour from your code mod via Atmo's [API](src/API.cs). API documentation can be found [here](API.md).

## Additional notes

In several places, Atmo uses regular expressions, or **regex**, to select text items based on user input. If you are not familiar with regex, [this site](https://www.regular-expressions.info/tutorialcnt.html) contains a tutorial about them, and [regex101](https://regex101.com/) can be used to test and debug your regex.

## Thanks to
- @DryCryCrystal - commissioning the project.
- @Slime_Cubed - advice and code reviews.
