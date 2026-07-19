# Paper Football Prototype Notes

## Primary Workflow

Use `Assets/Scenes/PaperFootballLauncher.unity` as the prototype entry scene. It opens the new tabletop prototype while keeping the existing `MainMenu.unity` and `TableScene.unity` available as legacy scenes.

## Implemented In This Prototype Track

- Local two-player tabletop flick flow.
- Two-stage contact selection before direction and power drag for the roguelike flick foundation.
- Contact-point flick physics that lets off-center hits create spin.
- Root-aligned Rigidbody football with a rotated flat visual child so table Y spin is not blocked by constraints.
- Visible fold-line and corner-mark references parented to the football so yaw spin is readable.
- Spin debug overlay for velocity, yaw, contact point, center of mass, and lever-arm distance.
- Softer default flick force tuning for better tabletop control at low drag strengths.
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
- Roguelike progression, seeded shot variance, upgrades, opponents, and run UI are not implemented yet.
