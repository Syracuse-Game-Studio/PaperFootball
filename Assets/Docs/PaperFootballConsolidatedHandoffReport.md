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

## Ongoing Update Policy

Every implementation change made after this consolidation should be recorded in this file before handoff. Use the "Post-Consolidation Updates" section for dated entries, and keep validation notes honest about what actually ran.

## Current Prototype

- Entry scene: `Assets/Scenes/PaperFootballLauncher.unity`
- Playable scene: `Assets/Scenes/PaperFootballGame.unity`
- Active namespace: `PaperFootball.Tabletop`
- Active physics controller: `Assets/Scripts/Tabletop/Physics/FootballPhysicsController.cs`
- Scene generation: `Assets/Editor/PaperFootballScaffolder.cs`
- Validation runner: `Assets/Editor/PaperFootballValidationRunner.cs`

Existing legacy scenes such as `MainMenu.unity` and `TableScene.unity` are still retained.

## Post-Consolidation Updates

### 2026-07-19 - Goalpost Contact Selection Fix

Problem: when the football was in front of Player One's field goal, the player could not reliably choose a flick contact point because goalpost/upright colliders could win the raycast.

Implemented:

- Contact selection now resolves the intended football contact through the football-specific raycast path instead of accepting the first world collider hit.
- `ContactPointSelector` and `FlickInputReader` now preserve football contact selection even when goalpost colliders sit between the camera and football.
- Added Play Mode coverage for selecting the football contact point behind goalpost colliders.
- Fixed the Play Mode test compile reference by using `UnityEngine.Physics.SyncTransforms()` instead of a nonexistent `PaperFootball.Tabletop.Physics.SyncTransforms` namespace path.

### 2026-07-19 - Missing Script Repair

Problem: Unity reported missing script references on roguelike scene objects and generated roguelike assets after the Phase 2 foundation was split across multi-type files.

Implemented:

- Split roguelike MonoBehaviours into Unity-friendly files whose filenames match their primary component types:
  - `TemporaryPlacementController.cs`
  - `TableSurfaceApplier.cs`
  - `ObstacleLayoutController.cs`
  - `PrecisionTargetZone.cs`
  - `OpponentTurnController.cs`
  - `RunController.cs`
- Converted the remaining shared framework declarations to partial classes so the original grouped files can keep shared logic without owning Unity's serialized script identity.
- Added Unity-friendly ScriptableObject files and stable `.meta` GUIDs for roguelike definitions/catalogs/settings:
  - `ConsumableDefinition.cs`
  - `FootballUpgradeDefinition.cs`
  - `UpgradeCatalog.cs`
  - `TableSurfaceDefinition.cs`
  - `TableSurfaceCatalog.cs`
  - `ObstacleLayoutDefinition.cs`
  - `ObstacleLayoutCatalog.cs`
  - `OpponentProfile.cs`
  - `OpponentCatalog.cs`
  - `ShotVarianceSettings.cs`
- Repaired roguelike generated `.asset` files so `m_Script` points to the correct MonoScript GUID instead of `{fileID: 0}`.
- Repaired `Assets/Scenes/PaperFootballGame.unity` to use real script GUID references for the roguelike scene components.
- Updated `PaperFootballScaffolder.EnsureComponent<T>()` so generated scene objects remove missing MonoBehaviours before adding required components.

### 2026-07-19 - Roguelike Player Two Handoff Fix

Problem: after Player Two's roguelike shot resolved, the camera sometimes stayed zoomed or failed to return to the zoomed-out Player One contact-selection state. The yellow contact hover could also stop appearing.

Root cause: the opponent controller repeatedly called `MatchController.SetInputSuppressed(false)` while control had already returned to Player One. That caused repeated `Render()` calls, which repeatedly reapplied the waiting-for-flick state and restarted contact selection/camera side effects before hover selection could stabilize.

Implemented:

- `MatchController.SetInputSuppressed(bool)` is now idempotent and returns early when suppression already matches the requested value.
- `FlickInteractionController.SetInputSuppressed(bool)` is also idempotent.
- `FlickInteractionController.ApplyMatchState(...)` no longer restarts contact selection every time the same waiting-for-flick state is reapplied. It only begins contact selection when the phase/player actually changes or the interaction is not already waiting/selecting contact.
- Added Play Mode regression coverage with `ReapplyingWaitingForFlickDoesNotRestartContactSelection`.

### 2026-07-19 - Fresh Unity Reopen Check

After the editor was reopened, the current Unity process was checked and the latest Editor log tail showed:

- Unity editor processes were running and responding.
- `CompileScripts` completed.
- `LogAssemblyErrors` reported no compile errors.
- The launcher scene loaded: `Assets/Scenes/PaperFootballLauncher.unity`.
- No fresh missing-script, compiler, or exception entries were found in the recent log tail.
- A serialized scan across `.unity`, `.prefab`, and `.asset` files found no `m_Script: {fileID: 0}`, no local scene-only script references, and no embedded `MonoScript` YAML records.

Validation note: this was an editor reopen/import log check, not a full Unity batch test run.

### 2026-07-19 - Flat Shot and Air Flick Shot Foundation

Problem: normal tabletop play needed two selectable shot types: the existing grounded Flat Table Shot and a new Air Flick Shot that can hop roguelike obstacles, become less predictable after landing, and never score a field goal.

Implemented:

- Added explicit shot-type architecture in `PaperFootball.Tabletop.Shots`:
  - `FootballShotType`
  - `ShotExecutionContext`
  - `AirFlickShotSettings`
  - `AirFlickShotCalculator`
  - `AirFlickShotResult`
  - `LandingVarianceSample`
  - `AirFlickState`
- Extended `FlickCommand`, `FlickForceCalculator`, `FlickInputReader`, and `FlickInteractionController` so shot type is preserved through contact selection, drag preview, release, variance resolution, AI commands, and physics launch.
- Added `ShotSelectionController` with `1` for Flat Shot, `2` for Flick Shot, clickable HUD buttons, selected-shot label, and tradeoff text.
- Added runtime fallback HUD/landing wiring so older saved scenes can still create the shot selector and `AirFlickLandingController` during Play Mode before the scene is rebuilt by the scaffolder.
- Added `AirFlickLandingController` on the football physics path. It tracks `Inactive`, `Launched`, `Airborne`, `Landed`, and `Resolved`, detects actual airborne state from table height/vertical velocity, and consumes a seeded landing variance sample once on the first valid table landing.
- Extended `FootballPhysicsController` with `AirFlick(...)`, queued external yaw torque, and `LastShotType` debug state. Air Flick still uses `AddForceAtPosition` through the selected contact point.
- Extended `TrajectoryPreviewRenderer` with an Air Flick arc preview distinct from field-goal preview usage.
- Hardened `FieldGoalController` with `ShotExecutionContext` eligibility. Field-goal scoring now requires an active attempt, `FootballShotType.FieldGoalKick`, `CanScoreFieldGoal = true`, matching player/football, and no duplicate score.
- Extended opponent decisions so AI candidates can be Flat or Air Flick. AI prefers Air Flick when active obstacle bounds block the useful direct path and prefers Flat Shot when the path is clear or edge risk is high.
- Added Air Flick upgrade/modifier extension keys and runtime modifier plumbing through `FootballBuildEvaluation`, `RunController`, and `MatchController`.
- Updated `PaperFootballScaffolder` to create/repair the Air Flick settings asset, landing controller, shot selector UI, Air Flick buttons/label/description, and AI obstacle reference when it can run.

Validation notes:

- Live Unity editor import log showed `LogAssemblyErrors (0ms)` and no recent compiler errors after the Air Flick changes.
- `git diff --check` reported no whitespace errors; it only emitted existing LF-to-CRLF normalization warnings.
- Serialized missing-script scan found no `m_Script: {fileID: 0}`, no local scene-only script references, and no embedded `MonoScript` YAML records in `.unity`, `.prefab`, or `.asset` files.
- After the Unity-generated `.csproj` files were regenerated, `dotnet build PaperFootball.Tabletop.csproj --no-restore` succeeded with `0` warnings and `0` errors.
- After the Unity-generated `.csproj` files were regenerated, `dotnet build PaperFootball.Tabletop.EditModeTests.csproj --no-restore` succeeded with `0` warnings and `0` errors.
- After the Unity-generated `.csproj` files were regenerated, `dotnet build PaperFootball.Tabletop.PlayModeTests.csproj --no-restore` succeeded with `0` warnings and `0` errors.
- Fresh Unity editor log after regeneration showed `CompileScripts: 5147.871ms` and `LogAssemblyErrors (0ms)`.
- Unity batch scaffolder was attempted but blocked because another Unity editor instance already had `G:\Unity\Games\PaperFootball` open. The log at `Temp/CodexAirFlickScaffolder.log` says multiple Unity instances cannot open the same project, so the saved scene was not rebuilt by batch mode.
- Full Edit Mode and Play Mode test runs were not completed in this pass because batch Unity could not open the project while the editor was already open.

Follow-up: run `Paper Football/Build Prototype Scene` from the open Unity editor, or close the editor and rerun `PaperFootball.Editor.PaperFootballScaffolder.BuildOrRepairSceneAndExit`, to save the new shot selector and Air Flick settings into `Assets/Scenes/PaperFootballGame.unity`. Runtime fallback wiring should still make the selector and landing controller appear during Play Mode before that saved-scene rebuild.

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
- Selectable normal shot modes: Flat Table Shot and Air Flick Shot.
- Contact-point physics using `Rigidbody.AddForceAtPosition`.
- Visible tabletop spin from off-center flicks.
- Air Flick launch arc, first-landing variance, and obstacle-hop foundation.
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
- `1`: select Flat Shot during normal turns.
- `2`: select Flick Shot during normal turns.
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
- explicit football shot-type preservation
- Air Flick launch calculation, bounded variance, seeded landing samples, field-goal eligibility context, shot-selection rejection rules, AI shot type choice, and Air Flick modifier composition

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
- contact selection targets the football even when goalpost colliders are in front
- reapplying the same waiting-for-flick state does not restart contact selection or camera hover setup
- touchdown integration for tiny positive overhang
- field-goal trigger duplicate-score prevention
- field-goal trajectory preview behavior
- Flat Shot no-upward-launch behavior
- Air Flick upward launch through the shared physics controller
- normal shot types cannot score field goals
- legitimate FieldGoalKick scoring still works

## Latest Validation

Latest validation after the Flat Shot / Air Flick implementation and after closing the Unity editor:

- Unity batch scaffolder: exit code `0`; rebuilt `PaperFootballGame.unity` and `PaperFootballLauncher.unity`.
- Saved scene now contains `ShotSelectionController`, `FlatShotButton`, `AirFlickShotButton`, and the football-side `AirFlickLandingController`.
- New Air Flick settings asset exists at `Assets/Materials/PaperFootballPrototype/AirFlickShotSettings.asset`.
- Edit Mode tests: `60 passed`, `0 failed` in `Temp/AirFlickEditModeTests.log`.
- Play Mode tests: `33 passed`, `0 failed` in `C:\Users\baley\AppData\LocalLow\DefaultCompany\PaperFootball\TestResults.xml`.
- Serialized missing-script scan: no `m_Script: {fileID: 0}`, scene-local script refs, or embedded `MonoScript` YAML records found in `Assets`.
- `git diff --check`: no whitespace errors; line-ending normalization warnings only.
- No Unity editor process was left running after validation; the leftover hidden Play Mode batch process was stopped after the passing XML was verified.

Known validation notes:

- Unity sometimes logs `[Licensing::Module] Error: Access token is unavailable; failed to update`; this has not blocked compile, scaffolding, or tests.
- Play Mode writes a passing Unity `TestResults.xml` but may not exit cleanly after the result is saved. Stop only the hidden batch PID after verifying the XML if this recurs.

## Known Limitations

- Visuals are placeholder geometry and materials.
- Fold line and corner mark are functional readability placeholders, not final paper art.
- Contact selection uses a box collider around the triangular mesh.
- Touchdown detection uses collider bounds, not an exact triangle footprint.
- Field-goal aiming still uses drag input plus computed upward impulse, not a dedicated kick UI.
- Goal-mouth detection is a simple trigger between uprights/above crossbar.
- Scene validation checks required references and camera framing, not full end-to-end gameplay simulation.
- Roguelike Phase 2 is implemented as a prototype foundation and is covered by the current Edit Mode and Play Mode validation passes.
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
5. Tune the roguelike run loop and Air Flick feel in editor playtests now that the saved scene wiring is current.
