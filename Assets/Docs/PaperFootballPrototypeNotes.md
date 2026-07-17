# Paper Football Prototype Notes

## Primary Workflow

Use `Assets/Scenes/PaperFootballLauncher.unity` as the prototype entry scene. It opens the new tabletop prototype while keeping the existing `MainMenu.unity` and `TableScene.unity` available as legacy scenes.

## Implemented In This Prototype Track

- Local two-player tabletop flick flow.
- Touchdown detection from stopped geometric overhang.
- Field-goal setup and attempt after a touchdown.
- Goal-mouth trigger detection between uprights and above the crossbar.
- Duplicate touchdown and field-goal scoring prevention.
- Reset-ball and reset-match controls.
- Edit Mode coverage for rules, scoring, turns, overhang, and field goals.
- Play Mode coverage for physics smoke tests and scaffolded scene references.

## Current Limitations

- Visuals are still placeholder geometry and materials.
- Field-goal aiming uses the same drag input plus a fixed upward impulse, not a dedicated kicking/aiming UI.
- The goal-mouth trigger is intentionally simple and does not yet model a full flight path review.
- Scene validation is focused on required references, not full end-to-end gameplay simulation.
- Automated CI validation is not wired yet; verification currently uses local Unity editor and batch-mode commands.
