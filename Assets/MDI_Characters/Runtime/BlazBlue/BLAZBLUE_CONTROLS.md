# BlazBlue controls

Classic 3-button is the default. Nothing already built changes until the scheme is switched.

## Switch in Unity

Call this from a button, the console, or a temporary debug line:

```csharp
DemonIdol.UI.SettingsPanel.SetControlScheme(DemonIdol.BlazBlue.ControlScheme.BlazBlue4Button);
```

Set it back with `ControlScheme.Classic3Button`. The choice is saved in PlayerPrefs (`mdi_control_scheme`, 0 = Classic, 1 = BlazBlue) and in the settings JSON. Old saves have no field, so they stay Classic.

BlazBlue only takes over a match when **both** fighters have `blazBlueProfile` set. Otherwise the match logs a warning and stays on the Classic sim. Akio, Aika, UnityChan and PicoChan do not have a profile yet, so they still play their 3-button kits.

## Buttons

Keyboard: J A, K B, L C, I D. Semicolon throws (B+C). U is barrier or Crush Trigger (A+B). O is Rapid Cancel (A+B+C). P is Burst or Overdrive (A+B+C+D). Move with WASD or arrows.

Gamepad, Central Fiction face layout: X/Square A, Y/Triangle B, B/Circle C, A/Cross D. LB barrier, RB throw, RT Rapid Cancel, LT Burst/Overdrive.

Touch: A B C across the right thumb, D the larger button at the bottom-right, with THROW, BARRIER, RC and BURST above them. The Classic P/M/S overlay is not edited.

## Slot 9

Izno chose C07 StellarWitch for roster slot 9. Akane and Muryotaisu are not that slot. Her kit is later, after the four built girls.

Astral Heat is off (`enableAstral: 0` on `BBSystemTuning`).
