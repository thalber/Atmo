# Atmo
 
Atmo is a regpack-centric extension API for Rain World. It uses plaintext config files for easily coupling together composable conditions and custom code.

## Requirements

- Custom Regions Support

## Usage

Atmo searches regionpack subfolders `[regpack]/World/[region]/[region-acronym].atmo` for specialized config files. For example, if Better Shelters had a .atmo file for Outskirts, it would be put into `Rain World/Mods/CustomResources/BetterShelters/World/SU/SU.atmo`.

`.atmo` files should be plaintext files encoded in UTF-8.

File format is as follows:

```
// comments are supported

// define room group. each group consists of a list of rooms
// you can separate room names with commas and whitespaces
GROUP:first
SU_C04
SU_S03
SU_S01
SU_S04
END GROUP

// Define an event
HAPPEN: AlarmShake
//groups + include rooms - exclude rooms
WHERE: first + SU_A41 - SU_S03
// Actions. Each action is a word, followed by optional literals that act as parameters for the action / word.
// String literals are enclosed in -> ' <- single quotes.
WHAT: sound 'HUD_Karma_Reinforce_Bump' 'cd=40' 'pitch=2.0' rumble 'cooldown=200' 'duration=100' 'intensity=0' 'shake=0.8'
// Activation conditions. You can connect them with () parens,
// !&^| boolean math operators or NOT AND XOR OR word equivalents
WHEN: AfterRain -80 AND ( Karma 0 '3-9' OR Maybe 0.7)
END HAPPEN
```
