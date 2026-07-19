# Roguelike Flick System Handoff Report

Date prepared: 2026-07-18
Project path: `G:\Unity\Games\PaperFootball`
Unity version: `6000.0.68f1`

## Purpose

This report summarizes the first roguelike-facing gameplay expansion work. The implemented scope is Phase 1: a two-stage flick interaction that preserves the existing tabletop match rules while making the player choose a physical contact point before choosing flick direction and power.

## Implemented Scope

- Added a `FlickInteractionState` enum and `FlickInteractionStateMachine`.
- Added local-space contact storage through `SelectedContactPoint`.
- Added `ContactPointSelector` to raycast against the active football collider and confirm a surface hit.
- Added `ContactPointIndicator` to show the selected hit point, contact quality labels, normal readout, and yaw tendency when a flick direction is known.
- Added `FootballCameraController` with tabletop, contact-selection, and resolution views.
- Added `FlickInteractionController` as the coordinator between contact selection, drag input, camera state, indicator state, and match input events.
- Extended `FlickInputReader` with a contact-point override so the second-stage drag keeps the previously selected contact point instead of replacing it with a fresh raycast hit.
- Updated `MatchController` to subscribe to `FlickInteractionController` when present, with fallback to direct `FlickInputReader` for older scenes.
- Updated `PaperFootballScaffolder` so regenerated prototype scenes include the new interaction objects and camera/marker/UI wiring.
- Updated HUD controls text for the new two-step input flow.

## Final Interaction Flow

1. Match enters `WaitingForFlick` or `FieldGoalSetup`.
2. `FlickInteractionController` switches to `WaitingForContact`.
3. Normal drag flick input is disabled.
4. `FootballCameraController` moves to a close-up football view.
5. `ContactPointSelector` raycasts against the active football collider.
6. `ContactPointIndicator` follows the hovered or confirmed surface point.
7. Player confirms a contact point with left mouse.
8. The selected point is stored in collider local space.
9. Camera returns to tabletop view.
10. `FlickInputReader` is enabled only after the transition finishes.
11. Player uses the existing drag/release interaction to choose direction and power.
12. The final `FlickCommand` uses the resolved world-space point from the stored local contact.
13. `FootballPhysicsController` continues applying physics through `AddForceAtPosition`.
14. During resolution, contact selection and flick input are disabled.

## Architecture Decisions

- The physics controller still only performs Rigidbody physics.
- Camera, selection, and marker behavior live in presentation/input components.
- Match rules remain in the existing `PaperFootballMatch` and `MatchController` flow.
- The new coordinator proxies existing `DragChanged`, `FlickReleased`, `ResetBallRequested`, `NewMatchRequested`, and `CancelRequested` events so match resolution code needed minimal change.
- Contact points are stored in local space to remain correct through camera transitions and any future football movement or rotation.
- `TryConfirmContactPoint` exists on `FlickInteractionController` so future AI or alternate input can choose contact points without faking mouse input.

## Files Added

- `Assets/Scripts/Tabletop/Input/FlickInteractionState.cs`
- `Assets/Scripts/Tabletop/Input/FlickInteractionStateMachine.cs`
- `Assets/Scripts/Tabletop/Input/SelectedContactPoint.cs`
- `Assets/Scripts/Tabletop/Input/ContactPointSelector.cs`
- `Assets/Scripts/Tabletop/Input/FlickInteractionController.cs`
- `Assets/Scripts/Tabletop/Presentation/ContactPointIndicator.cs`
- `Assets/Scripts/Tabletop/Presentation/FootballCameraController.cs`
- `Assets/Tests/EditMode/FlickInteractionStateMachineTests.cs`
- `Assets/Tests/EditMode/SelectedContactPointTests.cs`
- `Assets/Tests/PlayMode/FlickInteractionPlayModeTests.cs`
- `Assets/Docs/RoguelikeFlickSystemHandoffReport.md`

## Files Changed

- `Assets/Editor/PaperFootballScaffolder.cs`
- `Assets/Scripts/Tabletop/Input/FlickInputReader.cs`
- `Assets/Scripts/Tabletop/Match/MatchController.cs`
- `Assets/Scripts/Tabletop/Presentation/GameHudController.cs`
- `Assets/Tests/PlayMode/SceneBootstrapPlayModeTests.cs`

This work builds on the uncommitted spin/force changes already described in:

- `Assets/Docs/SpinForcePhysicsHandoffReport.md`

## Defaults

- Contact camera transition duration: `0.35` seconds.
- Contact close-up orthographic size: `0.95`.
- Contact close-up offset: `(0, 2.15, -1.85)` from the football.
- Contact marker scale from scaffolder: `0.075`.
- Contact marker surface offset: `0.025`.
- Contact selection is currently required for field-goal setup as well as normal flicks.

## Tests Added

Edit Mode:

- Local contact point converts back to expected world point.
- Contact point remains attached when the football rotates.
- Flick interaction state machine allows the expected two-stage flow.
- Flick interaction state machine rejects invalid jumps into flick drag.

Play Mode:

- Contact marker follows the football transform.
- Waiting-for-flick starts contact selection and disables drag input.
- Contact selection is disabled during physics resolution.
- Confirmed contact point is preserved as the `FlickInputReader` override for drag.
- Clearing selection removes stale contact override.

Scene bootstrap was also updated to expect:

- `FlickInteractionController`
- `ContactPointSelector`
- `ContactPointIndicator`
- `FootballCameraController`

## Validation Status

Validation was completed after the Unity editor was closed.

- Unity batch compile: exit code `0`.
- Compile log contained `Tundra build success` and `Batchmode quit successfully`.
- No `error CS` or `warning CS` entries were found in the targeted compile log scan.
- Scaffolder: `PaperFootball.Editor.PaperFootballScaffolder.BuildOrRepairSceneAndExit` completed with exit code `0` and rebuilt `Assets/Scenes/PaperFootballGame.unity` plus `Assets/Scenes/PaperFootballLauncher.unity`.
- Edit Mode tests: `Passed`, 40 passed, 0 failed, 0 skipped, 0 inconclusive, duration `0.145s`.
- Play Mode tests: Unity XML result `Passed`, 14 passed, 0 failed, 0 skipped, 0 inconclusive, duration `3.3128202s`.
- `git diff --check` reports no whitespace errors after cleanup; it still prints repository line-ending normalization warnings.

Log note: Unity emitted `[Licensing::Module] Error: Access token is unavailable; failed to update` during batch runs. This did not block compilation, scaffolding, or tests.

Process note: the Play Mode run saved a passing `TestResults.xml` but did not exit cleanly before the shell timeout, so the leftover hidden Unity batch process was stopped after results were verified.

## Known Limitations

- Shot variance, seeded randomness, encounters, upgrades, opponents, and run progression are not implemented yet.
- The current contact marker is simple: a small marker plus text/arc feedback, not a polished decal.
- The active football still uses a box collider around the triangular mesh, so contact selection is collider-accurate rather than exact triangle-surface accurate.
- No AI contact selection or roguelike run UI exists yet.

## Recommended Next Step

Start Phase 2 seeded shot variance now that the two-stage contact-then-flick core is compiled, scaffolded, and covered by Edit Mode and Play Mode tests.
