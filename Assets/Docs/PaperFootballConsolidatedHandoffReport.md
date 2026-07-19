# Paper Football Consolidated Handoff Report

Date consolidated: 2026-07-19
Project path: `G:\Unity\Games\PaperFootball`
Unity version: `6000.0.68f1`

## Purpose

This is the single intake report for the current tabletop paper football prototype. It consolidates:

- `Assets/Docs/CodexImplementationReport.md`
- `Assets/Docs/SpinForcePhysicsHandoffReport.md`
- `Assets/Docs/RoguelikeFlickSystemHandoffReport.md`
- `Assets/Docs/PaperFootballPrototypeNotes.md`
- `Assets/Docs/RoguelikePhase2HandoffReport.md`

Read this file first. The older reports are retained for history, but this is the current combined state.

## Current Prototype

- Entry scene: `Assets/Scenes/PaperFootballLauncher.unity`
- Playable scene: `Assets/Scenes/PaperFootballGame.unity`
- Active namespace: `PaperFootball.Tabletop`
- Active physics controller: `Assets/Scripts/Tabletop/Physics/FootballPhysicsController.cs`
- Scene generation: `Assets/Editor/PaperFootballScaffolder.cs`
- Validation runner: `Assets/Editor/PaperFootballValidationRunner.cs`

Existing legacy scenes such as `MainMenu.unity` and `TableScene.unity` are still retained.

## Phase 2 Roguelike Foundation

Phase 2 adds a roguelike run foundation without replacing local match mode. See `Assets/Docs/RoguelikePhase2HandoffReport.md` for the detailed handoff.

Implemented:

- deterministic run random streams
- seeded force, direction, and contact-point shot variance
- uncertainty preview UI
- upgrade/modifier framework with five starter upgrades
- three opponent profiles using shared `FlickCommand` physics
- deterministic six-encounter run generation
- normal, slippery, rough, and science-lab table surfaces
- obstacle layouts, precision target zone, and boss desk-shake hooks
- run state, rewards, victory/defeat summary, and JSON snapshot foundation
- launcher entry for Local Match, Roguelike Run, and Quit
- scaffolder creation of run controllers, catalogs, assets, and UI

Validation note: Unity batch validation for Phase 2 was attempted but blocked by licensing/package entitlement reconnect loops before compiler/test results were produced. Do not treat Phase 2 Unity validation as passed until rerun successfully.

## Gameplay Implemented

- Local two-player tabletop paper football match.
- Table, floor, scoring edges, goalposts, start spots, field-goal spots, HUD, camera, lighting, and input wiring.
- Two-stage input: select a contact point, then drag/release to choose direction and power.
- Contact-point physics using `Rigidbody.AddForceAtPosition`.
- Visible tabletop spin from off-center flicks.
- Softer flick force tuning for low-strength shots.
- Turn management, possession counter, score display, reset ball, and reset match.
- Out-of-bounds/fall detection.
- Rest detection using linear and angular velocity.
- Touchdown detection from stopped edge overhang.
- Field-goal setup and attempt after touchdown.
- Duplicate touchdown and field-goal score prevention.

## Current Controls

- Left mouse on football: select contact point.
- Left mouse drag/release after contact selection: aim and apply force.
- Drag away from intended travel direction: slingshot aim.
- `R`: reset ball.
- `N`: new match.
- `Esc`: cancel current drag.

## Visible Spin Fix

The previous visible-spin issue was caused by the Rigidbody root being rotated flat at `90` degrees. With `RigidbodyConstraints.FreezeRotationX | FreezeRotationZ`, that meant Unity was preserving the wrong free axis and blocking actual tabletop yaw.

The current fix is:

- `Paper Football` Rigidbody/collider root stays aligned to table/world axes.
- `PaperFootballVisual` is a child rotated flat to display the triangular mesh.
- `FootballFoldLine` and `FootballCornerMark` are child meshes under the Rigidbody root, so they rotate with the real football.
- Rigidbody constraints freeze X/Z but do not freeze Y.
- Off-center contact still applies the shot through `AddForceAtPosition`.
- `contactYawTorqueMultiplier` adds supplemental yaw torque derived from `cross(contact lever arm, shot impulse)`, so spin remains physics-based rather than a canned animation.
- `maximumFootballAngularVelocity` raises the Rigidbody cap so strong off-center hits can visibly rotate.

No direct mesh-only spin animation is used.

## Core Architecture

Rules are plain C# classes and do not depend on scene objects:

- `PaperFootballRuleSet`
- `PaperFootballConfig`
- `PaperFootballMatch`
- `MatchStateMachine`
- `MatchPhase`
- `PaperFootballPlayer`
- `PaperFootballRules`
- `FlickResolution`
- `FlickResolutionType`

Input:

- `FlickInputReader`
- `FlickCommand`
- `FlickForceCalculator`
- `FlickInteractionController`
- `FlickInteractionStateMachine`
- `ContactPointSelector`
- `SelectedContactPoint`

Physics:

- `FootballPhysicsController`
- `FootballRestDetector`
- `TableBoundaryDetector`

Scoring:

- `EdgeOverhangCalculator`
- `EdgeOverhangResult`
- `ScoringEdge`
- `OverhangDebugSnapshot`

Field goals:

- `FieldGoalController`
- `GoalPostTrigger`
- `FieldGoalKickCalculator`
- `FieldGoalKickResult`
- `TrajectoryPredictionService`

Presentation:

- `GameHudController`
- `FlickAimIndicator`
- `ContactPointIndicator`
- `FootballCameraController`
- `FootballSpinDebugOverlay`
- `OverhangDebugOverlay`
- `TrajectoryPreviewRenderer`
- `PrototypeMenuController`

Match orchestration:

- `MatchController`

## Current Rule Defaults

From `Assets/Materials/PaperFootballPrototype/DefaultPaperFootballConfig.asset`:

- Touchdown points: `6`
- Successful kick points: `3`
- Target score: `21`
- Maximum possessions: `0`
- Touchdown requires overhang: `true`
- Required overhang percent: `0`
- Minimum supported percent: `0.25`
- Falling from table changes possession: `true`
- Minimum flick force: `0.35`
- Maximum flick force: `4`
- Flick force response exponent: `1.6`
- Minimum drag distance: `0.05`
- Maximum drag distance: `2.5`
- Football stopping threshold: `0.08`
- Angular stopping threshold: `0.25`
- Football angular damping: `0.8`
- Contact yaw torque multiplier: `2.5`
- Maximum football angular velocity: `24`
- Required still time: `0.35`
- Fall height: `-1.2`
- Field-goal time limit: `6`
- Kickoff offset from center: `3.8`
- Field-goal force range: `2.5` to `9`
- Field-goal launch angle range: `28` to `58`
- Field-goal upward force range: `2` to `7`
- Trajectory point count: `28`
- Trajectory timestep: `0.075`
- Maximum trajectory preview time: `2.1`

## Scene Generation

Use:

```text
Paper Football/Build Prototype Scene
```

or batch method:

```text
PaperFootball.Editor.PaperFootballScaffolder.BuildOrRepairSceneAndExit
```

The scaffolder idempotently creates or repairs:

- `Assets/Scenes/PaperFootballGame.unity`
- `Assets/Scenes/PaperFootballLauncher.unity`
- prototype materials under `Assets/Materials/PaperFootballPrototype`
- default rules config
- build settings scene order
- table, floor, football, visual child, spin marks, goalposts, scoring edges, detectors, input, HUD, camera, and controllers

Avoid hand-editing large Unity scene YAML.

## Tests

Edit Mode coverage includes:

- match-state transitions
- turn changes
- scoring
- target-score win conditions
- flick-force calculations and clamping
- softened force response curve
- contact point preservation in `FlickCommand`
- overhang calculations
- any-positive-overhang touchdown behavior
- unsupported-football no-score behavior
- out-of-bounds/fall resolution
- field-goal scoring
- duplicate-score prevention
- match reset
- selected contact point local/world conversion
- two-stage flick state machine transitions
- trajectory prediction

Play Mode coverage includes:

- flick applies velocity
- centered flick produces less yaw than off-center flick
- left/right contact points create opposite yaw directions
- off-center flick changes transform Y rotation and keeps rotating
- root-aligned tabletop football visibly yaws from off-center flick
- yaw slows through angular damping
- rest detection waits for angular velocity to fall below threshold
- manual reset restores expected rotation
- contact marker follows football transform
- contact selection disables drag input until confirmed
- confirmed contact point is preserved for flick drag
- scene has required prototype references
- football is framed by camera at kickoff
- visible spin references are parented to the Rigidbody football
- touchdown integration for tiny positive overhang
- field-goal trigger duplicate-score prevention
- field-goal trajectory preview behavior

## Latest Validation

Latest validation after the root-aligned visible-spin fix:

- Unity batch compile: exit code `0`.
- Compile log contained `Tundra build success` and `Batchmode quit successfully`.
- Scaffolder: exit code `0`, rebuilt `PaperFootballGame.unity` and `PaperFootballLauncher.unity`.
- Edit Mode tests: `40 passed`, `0 failed`.
- Play Mode tests: `21 passed`, `0 failed`.
- `git diff --check`: no whitespace errors; line-ending normalization warnings only.

Known validation notes:

- Unity sometimes logs `[Licensing::Module] Error: Access token is unavailable; failed to update`; this has not blocked compile, scaffolding, or tests.
- Play Mode writes a passing Unity `TestResults.xml` but often does not exit before the shell timeout. The leftover hidden Unity batch process was stopped after verifying the XML result.

## Known Limitations

- Visuals are placeholder geometry and materials.
- Fold line and corner mark are functional readability placeholders, not final paper art.
- Contact selection uses a box collider around the triangular mesh.
- Touchdown detection uses collider bounds, not an exact triangle footprint.
- Field-goal aiming still uses drag input plus computed upward impulse, not a dedicated kick UI.
- Goal-mouth detection is a simple trigger between uprights/above crossbar.
- Scene validation checks required references and camera framing, not full end-to-end gameplay simulation.
- Roguelike Phase 2 is implemented as a prototype foundation, but Unity validation is currently blocked by licensing/package entitlement reconnect loops.
- Consumable placement has runtime foundations but not a full player-facing placement flow yet.
- No dedicated replay system.

## Current Worktree

The worktree contains uncommitted changes from the prototype, two-stage flick, spin/force, visible-spin, and report consolidation work. Do not assume these changes have been committed.

The active tabletop prototype uses `PaperFootball.Tabletop.Physics.FootballPhysicsController`. The older `Assets/Scripts/Ball/PaperFootballPhysics.cs` remains in the repository but is not the active generated-scene physics path.

## Recommended Next Steps

1. Playtest off-center flicks in the Unity editor and tune `contactYawTorqueMultiplier`, `footballAngularDamping`, and `maximumFootballAngularVelocity` by feel.
2. If flicks still go too far at low strength, increase `flickForceResponseExponent` or lower `maximumFlickForce`.
3. Add a better triangle or compound collider for more accurate contact and overhang.
4. Replace functional placeholder fold/corner marks with final paper art.
5. Rerun Unity validation after the licensing/package entitlement issue is resolved, then tune the roguelike run loop in editor playtests.
