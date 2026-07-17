# Paper Football Codex Implementation Report

Date prepared: 2026-07-17

## Purpose

This report summarizes the Unity paper football prototype work implemented by Codex in `G:\Unity\Games\PaperFootball`. It is written so another ChatGPT/Codex session can quickly understand the current architecture, gameplay behavior, generated scenes, validation status, and remaining limitations.

## High-Level Result

A new tabletop paper football prototype track was added without deleting the existing scenes. The new workflow uses:

- `Assets/Scenes/PaperFootballLauncher.unity` as the entry scene.
- `Assets/Scenes/PaperFootballGame.unity` as the generated playable prototype scene.
- Existing `MainMenu.unity` and `TableScene.unity` retained as legacy scenes.

Build settings place the launcher first, then the prototype scene, then the older scenes.

## Gameplay Implemented

- Tabletop playing surface with floor, scoring edges, goalposts, and placeholder materials.
- Triangular paper football mesh with collider, Rigidbody, damping, friction material, and simple spin.
- Mouse drag/release flick input.
- Flick strength calculation with minimum force, maximum force, minimum drag distance, and maximum drag distance.
- Turn management between Player One and Player Two.
- Possession counter.
- Out-of-bounds/fall detection.
- Football rest detection before resolving a flick.
- Touchdown detection based on overhang at the opponent's table edge.
- Field-goal setup and attempt after a touchdown.
- Field-goal trigger detection between uprights and above the crossbar.
- Duplicate touchdown and field-goal scoring prevention.
- HUD for score, player, phase, flick strength, field-goal mode, last result, possession, and controls.
- Reset ball and reset match controls.

## Current Controls

- Left mouse button on the football: start a flick.
- Drag away from the intended travel direction: slingshot-style aim.
- Release left mouse: apply flick.
- Longer drag: stronger flick, up to configured maximum.
- After a touchdown, the same drag/release action performs the field-goal attempt with an added upward impulse.
- `R`: reset ball.
- `N`: new match.
- `Esc`: cancel current drag.

The flick only starts when the initial click hits the football collider.

## Touchdown Rule Correction

The touchdown rule was corrected to match the user's stated paper football rule:

> Any part of the football hovering over the opponent's table edge is a touchdown.

Implementation details:

- `requiredOverhangPercent` default is now `0`.
- `DefaultPaperFootballConfig.asset` sets `requiredOverhangPercent: 0`.
- The scaffolder generates new configs with `requiredOverhangPercent = 0f`.
- A touchdown still requires the football to be at least partly supported by the table and not fallen off.
- Alternate house rules can still set a higher `requiredOverhangPercent` if desired.

Relevant files:

- `Assets/Scripts/Tabletop/Scoring/EdgeOverhangCalculator.cs`
- `Assets/Scripts/Tabletop/Rules/PaperFootballRuleSet.cs`
- `Assets/Materials/PaperFootballPrototype/DefaultPaperFootballConfig.asset`
- `Assets/Tests/EditMode/EdgeOverhangCalculatorTests.cs`

## Camera/View Fix

The prototype camera was updated after the football was not visible in the Game view.

Current generated camera settings in `PaperFootballGame.unity`:

- Orthographic: enabled.
- Orthographic size: `6.8`.
- Position: `(0, 9.4, -7.4)`.
- Near clip plane: `0.03`.
- The camera looks at the table center.

The bottom controls text was moved upward to avoid clipping in the Game view.

`PaperFootballMesh` was also fixed so it preserves the scaffolded paper material instead of replacing it at runtime.

Relevant files:

- `Assets/Editor/PaperFootballScaffolder.cs`
- `Assets/Scripts/Ball/PaperFootballMesh.cs`
- `Assets/Tests/PlayMode/SceneBootstrapPlayModeTests.cs`

## Architecture Added

Runtime scripts were placed under `Assets/Scripts/Tabletop`, with namespaces beginning with `PaperFootball.Tabletop`.

### Rules

Plain C# rule/state classes:

- `PaperFootballRuleSet`
- `PaperFootballConfig`
- `PaperFootballMatch`
- `MatchStateMachine`
- `MatchPhase`
- `PaperFootballPlayer`
- `PaperFootballRules`
- `FlickResolution`
- `FlickResolutionType`

Rules are kept independent from scene objects.

### Input

- `FlickInputReader`
- `FlickCommand`
- `FlickForceCalculator`

The input layer tracks drag start, current drag point, duration, force, direction, and validity.

### Physics

- `FootballPhysicsController`
- `FootballRestDetector`
- `TableBoundaryDetector`

Physics components handle Rigidbody movement, rest detection, and table/fall checks. They do not directly calculate scores.

### Scoring

- `EdgeOverhangCalculator`
- `EdgeOverhangResult`
- `ScoringEdge`

Touchdown scoring is resolved from football bounds, table bounds, attacking player, and rule config.

### Field Goals

- `FieldGoalController`
- `GoalPostTrigger`

The field-goal flow prevents duplicate scores and reports results back to the match controller.

### Presentation

- `GameHudController`
- `FlickAimIndicator`
- `PrototypeMenuController`

UI observes match/input state and does not own scoring rules.

### Match Orchestration

- `MatchController`

Wires input, physics, scoring, field goals, HUD, and match rules together.

## Scene Generation

Editor automation was added at:

- `Assets/Editor/PaperFootballScaffolder.cs`

Menu command:

- `Paper Football/Build Prototype Scene`

Batch/automation method:

- `PaperFootball.Editor.PaperFootballScaffolder.BuildOrRepairSceneAndExit`

The scaffolder idempotently creates or repairs:

- `Assets/Scenes/PaperFootballGame.unity`
- `Assets/Scenes/PaperFootballLauncher.unity`
- prototype materials under `Assets/Materials/PaperFootballPrototype`
- default rules config
- table, floor, football, goalposts, scoring edges, start spots, field-goal spots, detectors, input reader, HUD, camera, lighting, and event system
- build settings scene order

Existing scenes are retained.

## Validation Support

Editor validation helper:

- `Assets/Editor/PaperFootballValidationRunner.cs`

Methods:

- `PaperFootball.Editor.PaperFootballValidationRunner.RunEditModeTests`
- `PaperFootball.Editor.PaperFootballValidationRunner.RunPlayModeTests`

## Tests Added

Edit Mode tests:

- `EdgeOverhangCalculatorTests`
- `FlickForceCalculatorTests`
- `MatchStateMachineTests`
- `PaperFootballMatchTests`

Coverage includes:

- match-state transitions
- turn changes
- scoring
- target-score win conditions
- flick-force calculations
- flick-force clamping
- overhang calculations
- any-positive-overhang touchdown behavior
- unsupported-football no-score behavior
- out-of-bounds/fall resolution
- field-goal scoring
- duplicate-score prevention
- match reset

Play Mode tests:

- `FootballPhysicsPlayModeTests`
- `SceneBootstrapPlayModeTests`

Coverage includes:

- flick applies velocity
- rest detector reports rest
- generated scene has required prototype references
- kickoff football is inside camera view

## Latest Validation Results

The last validation runs completed successfully:

- Unity batch mode: completed successfully.
- Edit Mode tests: 18 passed, 0 failed.
- Play Mode tests: 4 passed, 0 failed.

Recent known compile warnings from older existing scripts were previously observed:

- `Assets/Scripts/Ball/PaperFootballPhysics.cs`: unused `dragForce`.
- `Assets/Scripts/Input/InputManager.cs`: unused `enableKeyboardInput`.

No new compiler errors were reported in the validation runs.

## Current Default Rule Values

From `Assets/Materials/PaperFootballPrototype/DefaultPaperFootballConfig.asset`:

- Touchdown points: `6`
- Successful kick points: `3`
- Target score: `21`
- Touchdown requires overhang: `true`
- Required overhang percent: `0`
- Minimum supported percent: `0.25`
- Falling from table changes possession: `true`
- Maximum flick force: `18`
- Minimum flick force: `1.5`
- Maximum drag distance: `2.5`
- Football stopping threshold: `0.08`
- Fall height: `-1.2`
- Kickoff offset from center: `3.8`

## Known Limitations

- Visuals are placeholder geometry and materials.
- Field-goal aiming uses the same drag input plus a fixed upward impulse, not a dedicated kicking UI.
- Goal-mouth detection is a simple trigger between uprights/above crossbar, not a full flight-path review.
- Touchdown detection uses collider bounds, not an exact triangle mesh footprint.
- Scene validation checks required references and camera framing, not a full end-to-end gameplay simulation.
- No dedicated camera controls or replay/debug overlay yet.
- No AI or online/multiplayer flow; this is a local two-player prototype.

## Suggested Next Steps

- Add a small debug overlay showing overhang distance/support percent when the ball stops.
- Add Play Mode tests that simulate a ball stopping with tiny overhang and confirm the full match controller awards a touchdown.
- Improve field-goal aiming with a visible arc/target indicator.
- Tune camera framing and football scale after more playtesting.
- Replace placeholder materials/models with final art once gameplay feel is stable.
- Address older unused-field compiler warnings in existing non-tabletop scripts when convenient.
