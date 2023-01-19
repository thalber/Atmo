# Atmo

[![build](https://github.com/thalber/Atmo/actions/workflows/build.yml/badge.svg?branch=main)](https://github.com/thalber/Atmo/actions/workflows/build.yml)

Atmo is a Rain World mod that acts as a regpack-centric scripting addon for [RegionKit](https://github.com/DryCryCrystal/Region-Kit). It allows a region maker to add world events, easily bundling behaviour with custom trigger conditions.

## Dependencies

- Custom Regions Support.

## Usage

Atmo uses `world/[region-name].atmo` resource path to obtain script files: this means that if, for example, if your mod wants to add a .atmo file to SU, you will place your .atmo file in `[your-mod-folder]/world/su.atmo`. Use merge files feature of Remix if you need to combine several mods.

`.atmo` files should be plaintext files encoded in UTF-8.

A shorter example of how an atmo script file could look:

```
//note that, despite room filenames being lowercase after DP
//they should still be written as uppercase in script files

GROUP: group1
SU_S01 SU_S03 SU_S04
END GROUP

HAPPEN: ReduceGravity
WHAT: fling '0;0.5'
WHERE: group1
WHEN: always
```

This would reduce gravity in all shelters in Outskirts.

Full file format example (with comments) can be found [here](syntax.txt). If you are using Notepad++, there is a User-Defined Language config for it in this repo, which gives you syntax highlighting (for [dark mode](../extras/atmoscript.udl.xml) and [light mode](../extras/atmoscript.lightmode.udl.xml)).

A happen block can have multiple `WHAT:` and `WHERE:` clauses, but *only one* `WHEN:` clause.
Inside each `WHERE:` clause line, parsing starts with reading *group names*. After you've switched to included or excluded rooms by using a `+`/`-` separator, you can switch back to groups by using a `=` separator (`WHERE: g1 + r1 r2 - r3 = g2` includes groups `g1` and `g2` and rooms `r1` and `r2`, and excludes room `r3`).

## Actions

Actions are the effects your happens (events) will produce when activated. A happen can have multiple actions. Default action names are case insensitive. Actions can receive parameters.

For a list of actions you can use out of the box, see [this reference](builtins.md).

## Triggers

Triggers are conditions that determine *when* a happen, with all its actions, is activated. Triggers can be grouped together using boolean logic operators (details in syntax example).

For a list of triggers you can use out of the box, see [this reference](builtins.md).

## Custom behaviours

You can register your own triggers and behaviour from your code mod via Atmo's [API](../src/API/V0.cs). API documentation can be found [here](API.md).

## Additional notes

In several places, Atmo uses regular expressions, or **regex**, to select text items based on user input. If you are not familiar with regex, [this site](https://www.regular-expressions.info/tutorialcnt.html) contains a tutorial about them, and [regex101](https://regex101.com/) or [regexr](https://regexr.com/) can be used to test and debug your regex in browser.

## Thanks to
- @DryCryCrystal - commissioning the project.
- @Slime_Cubed - advice and code reviews.
