# Air Flick and Flat Shot Handoff Report

Date: 2026-07-19
Project path: `G:\Unity\Games\PaperFootball`
Unity version: `6000.0.68f1`

## Summary

Implemented the foundation for two selectable normal-play shot types:

- `FlatTableShot`: preserves the existing grounded tabletop shot behavior.
- `AirFlickShot`: launches the football into the air, can hop roguelike obstacles, applies seeded first-landing variance, and cannot score field goals.

`FieldGoalKick` remains a separate shot type that is forced only during a legitimate field-goal attempt.

## Shot-Type Architecture

Added `PaperFootball.Tabletop.Shots`:

- `FootballShotType`
- `ShotExecutionContext`
- `AirFlickShotSettings`
- `AirFlickShotCalculator`
- `AirFlickShotResult`
- `LandingVarianceSample`
- `AirFlickState`

`FlickCommand` now carries `ShotType`, so shot intent is preserved through input, AI, variance, physics, landing, debug, and scoring paths.

## Player Input Flow

- Normal turns allow Flat Shot and Flick Shot.
- Keyboard:
  - `1`: Flat Shot
  - `2`: Flick Shot
- Mouse users get clickable HUD buttons through `ShotSelectionController`.
- Field-goal setup/attempt displays `SHOT: FIELD GOAL` and rejects normal shot-mode selection.
- If the saved scene has not been rebuilt yet, `MatchController` creates a runtime shot selector under the HUD during Play Mode.

## Flat Shot Behavior

Flat Shot keeps the existing behavior:

- close-up contact selection
- local-space contact preservation
- tabletop drag/power selection
- seeded force/direction/contact variance
- `Rigidbody.AddForceAtPosition`
- visible yaw from off-center hits
- no intentional upward launch impulse

## Air Flick Physics

`AirFlickShotCalculator` produces:

- horizontal direction
- forward impulse
- upward impulse
- selected contact point
- launch angle
- predicted max height
- one `LandingVarianceSample`

`FootballPhysicsController.AirFlick(...)` queues the impulse and applies it through the existing `FixedUpdate` physics path using `AddForceAtPosition`.

## Landing Variance

`AirFlickLandingController` tracks:

- `Inactive`
- `Launched`
- `Airborne`
- `Landed`
- `Resolved`

It marks the football airborne only after height/vertical-velocity checks, then consumes the stored landing sample once on the first valid table landing. The landing effect is bounded and physics-based:

- tangential impulse
- optional bounce correction
- yaw torque impulse

No final position or final rotation is assigned directly.

## Field-Goal Restriction

`FieldGoalController` now requires `ShotExecutionContext` eligibility. Field-goal scoring requires:

- active field-goal attempt
- active `FootballShotType.FieldGoalKick`
- `CanScoreFieldGoal = true`
- matching player
- matching football collider
- no duplicate score for the attempt

Flat Shot and Air Flick Shot use `CanScoreFieldGoal = false`.

## AI Behavior

`OpponentDecisionService` now generates Flat Shot candidates and Air Flick candidates. Air Flick scores better when active obstacle bounds block the useful direct path. Flat Shot scores better when the path is clear or the projected landing is near an edge.

`OpponentTurnController` can receive `ObstacleLayoutController` and passes active obstacle bounds into AI decision context.

## Upgrade Hooks

Added modifier keys:

- `AirFlickForwardImpulseMultiplier`
- `AirFlickUpwardImpulseMultiplier`
- `AirFlickLaunchAngleAdd`
- `AirFlickForceVarianceMultiplier`
- `AirFlickDirectionVarianceMultiplier`
- `AirFlickContactVarianceMultiplier`
- `AirFlickLandingVarianceMultiplier`
- `AirFlickBounceMultiplier`
- `AirFlickLandingYawMultiplier`
- `AirFlickPreviewAccuracy`

`RunController` passes evaluated Air Flick modifiers into `MatchController.SetAirFlickModifierScales(...)`.

## Scaffolder

`Assets/Editor/PaperFootballScaffolder.cs` now creates or repairs:

- Air Flick settings asset path: `Assets/Materials/PaperFootballPrototype/AirFlickShotSettings.asset`
- `AirFlickLandingController`
- `ShotSelectionController`
- Flat Shot button
- Flick Shot button
- selected-shot label
- shot description text
- opponent obstacle reference

After the Unity editor was closed, batch scaffolding completed successfully and rebuilt both generated scenes. The saved game scene now includes the Air Flick shot selector and Air Flick landing controller, so the runtime fallback should only matter for stale or hand-edited scenes.

## Files Added

- `Assets/Scripts/Tabletop/Shots/*`
- `Assets/Scripts/Tabletop/Input/ShotSelectionController.cs`
- `Assets/Scripts/Tabletop/Physics/AirFlickLandingController.cs`
- `Assets/Tests/EditMode/AirFlickShotTests.cs`
- `Assets/Docs/AirFlickAndFlatShotHandoffReport.md`
- `Assets/Materials/PaperFootballPrototype/AirFlickShotSettings.asset`

## Files Modified

- `FlickCommand.cs`
- `FlickForceCalculator.cs`
- `FlickInputReader.cs`
- `FlickInteractionController.cs`
- `FootballPhysicsController.cs`
- `TableBoundaryDetector.cs`
- `FieldGoalController.cs`
- `MatchController.cs`
- `GameHudController.cs`
- `TrajectoryPreviewRenderer.cs`
- `FootballModifiers.cs`
- `OpponentFramework.cs`
- `OpponentTurnController.cs`
- `RunProgression.cs`
- `PaperFootballScaffolder.cs`
- Play Mode scene/physics/field-goal tests
- consolidated and implementation docs

## Tests Added Or Updated

Edit Mode:

- shot type preservation
- Flat Shot no intentional upward direction
- Air Flick upward impulse and launch-angle bounds
- Air Flick variance scaling
- reproducible landing variance
- landing correction impulse cap
- field-goal eligibility context
- shot selection rejection during field-goal/resolution phases
- AI Flat/Air shot selection
- Air Flick modifier composition

Play Mode:

- Flat Shot no upward velocity
- Air Flick upward launch via shared physics controller
- normal shot types cannot score field goals
- legitimate field-goal kick still scores
- scene bootstrap expects Air Flick landing and shot selection references

## Validation Results

- Live Unity editor import log: `LogAssemblyErrors (0ms)` after the Air Flick changes.
- Recent Unity log scan: no `error CS`, missing-script warnings, or fresh exceptions found.
- Serialized scan: no `m_Script: {fileID: 0}`, local scene-only script refs, or embedded `MonoScript` YAML records found in `.unity`, `.prefab`, or `.asset` files.
- `git diff --check`: no whitespace errors; LF-to-CRLF normalization warnings only.
- After regenerating Unity `.csproj` files, `dotnet build PaperFootball.Tabletop.csproj --no-restore` succeeded with `0` warnings and `0` errors.
- After regenerating Unity `.csproj` files, `dotnet build PaperFootball.Tabletop.EditModeTests.csproj --no-restore` succeeded with `0` warnings and `0` errors.
- After regenerating Unity `.csproj` files, `dotnet build PaperFootball.Tabletop.PlayModeTests.csproj --no-restore` succeeded with `0` warnings and `0` errors.
- Fresh Unity editor log after regeneration showed `CompileScripts: 5147.871ms` and `LogAssemblyErrors (0ms)`.
- Unity batch scaffolder after the editor was closed: exit code `0`; rebuilt `Assets/Scenes/PaperFootballGame.unity` and `Assets/Scenes/PaperFootballLauncher.unity`.
- Saved scene verification found `ShotSelectionController`, `FlatShotButton`, `AirFlickShotButton`, and `AirFlickShotSettings`.
- Edit Mode validation runner: `60 passed`, `0 failed` in `Temp/AirFlickEditModeTests.log`. The requested text result file was not emitted, so the log is the source of truth for this count.
- Play Mode validation runner: `33 passed`, `0 failed` in `C:\Users\baley\AppData\LocalLow\DefaultCompany\PaperFootball\TestResults.xml`.
- Play Mode cleanup note: Unity saved the passing XML but left a hidden batch editor process open; it was stopped only after the XML was verified.

## Known Limitations

- Runtime fallback still creates the shot selector and landing controller during Play Mode if the saved scene is stale.
- Air Flick obstacle-clearance preview is an estimate, not exact Rigidbody replay.
- Landing variance affects first valid table landing only and does not yet expose a polished player-facing clearance meter.
- No new Air Flick-specific upgrade assets were added; only modifier extension points were created.

## Worktree Status

The worktree is uncommitted. Do not assume any of this work has been committed.
