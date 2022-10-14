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
// !&^| boolean math operators or NOT AND XOR OR word equivalents. XOR can also be used as !=
WHEN: AfterRain -80 AND ( Karma 0 '3-9' OR Maybe 0.7)
END HAPPEN
```

## Builtins
### Actions
Action names case insensitive.

- `playergrav`/`playergravity`: applies a custom gravity multiplier to players in the room.
  - First parameter sets the multiplier (default 0.5).
  - Example: `playergrav 0.3`.
- `sound`/`playsound`: Plays a specified sound with a set interval.
  - parameter 1 (required): sound ID.
  - subsequent parameters are received in form of `paramname=value`. they are as follows:
    - `cd`/`cooldown`: delay (frames) between each sound cue. set to negative to make it only play once.
    - `vol`/`volume`: volume of the sound. default 1.0.
    - `pitch`: sound pitch. default 1.0.
  - Example: `sound 'HUD_Karma_Reinforce_Bump' 'cd=40' 'pitch=2.0'`
- `rumble`/`screenshake`: shakes the screen.
  - parameters are received in form of `paramname=value`. they are as follows:
    - `cd`/`cooldown`: time between shakes (frames). Set to negative to make it not reset.
    - `duration`: duration of a shake. Set to negative to make it shake forever after being triggered.
    - `shake`: shake intensity. default 0.5
  - Example: `rumble 'cooldown=200' 'duration=100' 'intensity=0' 'shake=0.8'`
    
### Trigger conditions
Case insensitive.
- `always`: Always active. No arguments.
- `untilrain`/`beforerain`: Active until rain starts.
  - Optional parameter sets an additional delay (in frames), which can be negative.
- `afterrain`: Active after rain.
  - Optional parameter sets an additional delay (in frames), which can be negative.
- `everyx`/`every`: Active for *one* frame every X frames. Default delay is 40 (one second);
  - optional parameter sets a custom delay.
- `maybe`/`chance`: Every cycle, this trigger is either active or inactive.
  - First parameter sets the chance (it should be between 0 and 1, default value 0.5).
- `flicker`: this trigger turns on and off periodically. receives up to 5 parameters:
  - minimum Active time (frames)
  - maximum Active time (frames)
  - minimum Inactive time (frames)
  - maximum Inactive time (frames)
  - start enabled (`1`, `true` or `yes` to enable)
- `karma`: active when a karma requirement is met.
  - receives any number of integer parameters (`karma 1 5 9`) for individual levels, can also receive ranges in string parameters (`karma '1-3'`)
- `visited`/`playervisited`: each cycle, activates *after* the player visits one of the rooms provided as parameters and stays on.
  - string parameters contain room names.
