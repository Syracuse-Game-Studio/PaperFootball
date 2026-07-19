# Paper Football Roguelike Phase 2 Handoff Report

Date prepared: 2026-07-19
Project path: `G:\Unity\Games\PaperFootball`
Unity version: `6000.0.68f1`

## Purpose

This report summarizes the Phase 2 roguelike foundation implementation. Read this with `Assets/Docs/PaperFootballConsolidatedHandoffReport.md`.

## High-Level Result

Phase 2 adds a playable roguelike foundation on top of the existing tabletop match and physics systems. It does not replace local match mode. Human and AI shots still flow through `FlickCommand`, `MatchController`, and `FootballPhysicsController`.

Implemented systems:

- seeded run random streams
- one-shot seeded force, direction, and contact-point variance
- uncertainty preview UI
- upgrade and modifier framework
- five initial upgrades
- three opponent profiles
- deterministic six-encounter generation
- table surface and obstacle foundations
- precision target zone
- boss desk-shake and full-spin touchdown bonus hooks
- run state, rewards, victory/defeat flow, summary, and JSON snapshot
- launcher entry for Local Match, Roguelike Run, and Quit
- scaffolder integration for default assets, controllers, UI, and scene references
- Edit Mode and Play Mode tests for Phase 2 foundations

## Seeded Random Architecture

Files:

- `Assets/Scripts/Tabletop/Roguelike/Random/RunRandom.cs`

Key types:

- `IRunRandom`
- `DeterministicRunRandom`
- `SequenceRunRandom`
- `RunRandomStream`
- `StableSeedUtility`

Random streams:

- `RunGeneration`
- `EncounterGeneration`
- `RewardGeneration`
- `OpponentDecisions`
- `ShotVariance`
- `Cosmetic`

Child seeds are derived from stable text and numeric inputs: run seed, stream name, encounter index, player, possession number, flick sequence number, and a stable identifier. The implementation uses a stable FNV-style hash and does not rely on object hash codes.

## Shot Variance

Files:

- `Assets/Scripts/Tabletop/Roguelike/Variance/ShotVariance.cs`
- `Assets/Scripts/Tabletop/Roguelike/Variance/ShotVarianceController.cs`
- `Assets/Scripts/Tabletop/Presentation/ShotUncertaintyPreview.cs`

Default settings:

- force variance: `0.03`
- direction variance: `1.5` degrees
- contact-point variance radius: `0.0075` Unity units
- reveal sampled result: `false`
- accuracy rating: `Stable`

Resolution order:

1. Start from the validated `FlickCommand`.
2. Derive a shot-variance seed from run context.
3. Sample force multiplier once.
4. Clamp final force to current rules.
5. Sample yaw direction offset once.
6. Sample local contact jitter once.
7. Resolve contact back onto the football collider.
8. Store all base and final values in `ResolvedFlickParameters`.
9. Convert to a final `FlickCommand`.
10. Queue the physics impulse through `FootballPhysicsController`.

No final-position variance, mesh-only animation, or per-frame randomness was added.

## Modifier Composition

Files:

- `Assets/Scripts/Tabletop/Roguelike/Modifiers/FootballModifiers.cs`

Key types:

- `FootballModifier`
- `ModifierPipeline`
- `FootballUpgradeDefinition`
- `FootballBuild`
- `UpgradeCatalog`
- `FootballBuildEvaluation`

Composition order:

1. base value
2. additive modifiers
3. multiplicative modifiers
4. minimum clamps
5. maximum clamps
6. overrides
7. final safety clamp

Modifiers are sorted by priority, then stable modifier ID.

## Initial Upgrades

Created by `PaperFootballScaffolder` under `Assets/Materials/PaperFootballPrototype/Roguelike`.

- `Tight Fold`: contact variance x`0.65`, direction variance x`0.75`, spin torque x`0.9`
- `Weighted Center`: spin torque x`0.75`, direction variance x`0.85`, angular damping x`1.1`, center of mass Y `-0.01`
- `Loose Fold`: spin torque x`1.3`, contact variance x`1.35`, max angular velocity x`1.15`
- `Waxed Paper`: friction x`0.75`, flick force x`1.08`, force variance x`1.15`, linear damping x`0.85`
- `Reinforced Tip`: field-goal force x`1.08`, field-goal direction variance x`0.75`, preview accuracy +`0.15`

Reward choices are deterministic from run seed and reward index, avoid duplicates, skip max-stack upgrades, and respect mutual exclusion tags.

## Opponent Behavior

Files:

- `Assets/Scripts/Tabletop/Roguelike/Opponents/OpponentFramework.cs`

Profiles:

- `Power Flicker`: high power, higher force variance, moderate accuracy, high risk
- `Spinner`: off-center contact, higher yaw spin, medium power
- `Calculator`: centered contact, low force variance, high accuracy, conservative

AI decision flow:

1. Build deterministic opponent random stream.
2. Choose candidate contact points on the football collider.
3. Generate candidate directions and powers.
4. Score candidates for forward progress, table safety, profile preference, and spin fit.
5. Submit the selected normal `FlickCommand` through `MatchController.TrySubmitFlick`.

## Encounter Generation

Files:

- `Assets/Scripts/Tabletop/Roguelike/Encounters/EncounterFramework.cs`

Generated run sequence:

1. Standard Match: Power Flicker, Normal Desk, No Obstacles
2. Precision Drill: Calculator, Rough Desk, Pencil Lane
3. Standard Match: Calculator, Normal Desk, Book Bank
4. Standard Match: Power Flicker, Slippery Desk, Eraser Midfield
5. Elite Match: Spinner, Slippery Desk, Mixed Office
6. Boss Match: Spinner, Science Lab Table, Mixed Office

Surfaces:

- `Normal Desk`: dynamic friction `0.55`, static friction `0.65`
- `Slippery Desk`: dynamic friction `0.25`, static friction `0.32`
- `Rough Desk`: dynamic friction `0.95`, static friction `1.05`
- `Science Lab Table`: dynamic friction `0.34`, static friction `0.44`

Obstacle layouts:

- no obstacles
- pencil lane
- eraser midfield
- book bank
- mixed office

## Run Progression

Files:

- `Assets/Scripts/Tabletop/Roguelike/Run/RunProgression.cs`
- `Assets/Scripts/Tabletop/Roguelike/Presentation/RunProgressionUiController.cs`
- `Assets/Scripts/Tabletop/Roguelike/Presentation/RoguelikeDebugOverlay.cs`
- `Assets/Scripts/Tabletop/Roguelike/Run/RunDevelopmentCommands.cs`

Run state tracks:

- run seed
- current encounter index
- generated encounters
- player build
- encounter results
- run status
- run statistics

Snapshot JSON contains stable data only: version, seed, current index, status, upgrade IDs/stacks, encounter results, and statistics.

## Consumables

Files:

- `Assets/Scripts/Tabletop/Roguelike/Consumables/ConsumableFramework.cs`

Foundations added:

- `Tape Friction Patch`
- `Eraser Blocker`
- `ConsumableInventory`
- `TemporaryPlacementController`

Temporary placements are cleared between encounters. Full player-facing placement UI is not complete yet.

## Scaffolder Integration

Updated:

- `Assets/Editor/PaperFootballScaffolder.cs`

The scaffolder now creates or repairs:

- default shot variance settings
- upgrade assets and catalog
- opponent assets and catalog
- table surface assets and catalog
- obstacle layout assets and catalog
- `ShotVarianceController`
- `ShotUncertaintyPreview`
- `TableSurfaceApplier`
- `ObstacleLayoutController`
- `TemporaryPlacementController`
- `PrecisionTargetZone`
- `OpponentTurnController`
- `RunController`
- run UI panels
- roguelike debug overlay
- development command object
- launcher buttons for Local Match, Roguelike Run, Quit, and legacy scenes

## Controls

Launcher:

- `Local Match`
- `Roguelike Run`
- `Quit`
- legacy scene buttons

Run UI:

- enter numeric seed
- random seed
- start run
- continue encounter intro
- select one reward
- restart same seed
- new seed
- return to local match

Development commands:

- `F5`: restart current seed
- `F6`: start new random seed
- `F9`: toggle shot variance

## Tests Added

Edit Mode:

- `Assets/Tests/EditMode/RoguelikePhase2FoundationTests.cs`

Play Mode:

- `Assets/Tests/PlayMode/RoguelikePhase2PlayModeTests.cs`

Coverage includes:

- seeded random reproducibility
- stable child seed derivation
- disabled variance preserving baseline values
- variance bounds and reproducibility
- deterministic modifier composition
- reward uniqueness and stack filtering
- upgrade evaluation effects
- distinct opponent commands
- generated encounter sequence stability
- run snapshot JSON
- variance-driven physics input
- zero-variance physics baseline
- AI command through shared physics controller
- surface runtime material swapping
- obstacle cleanup
- generated scene roguelike references

## Validation Status

Completed:

- `git diff --check`: passed, with LF-to-CRLF normalization warnings only.

Attempted but blocked:

- Unity batch compile
- scaffolder batch run
- Edit Mode tests
- Play Mode tests

Unity batch mode repeatedly reached domain reload and package registration, then stalled in licensing/package entitlement reconnect loops. The logs showed no `error CS` compiler diagnostics, but Unity never reached `Tundra build success`, the scaffolder method, or test execution before timeout. Do not treat Unity validation as passed.

Log paths used:

- `C:\Users\baley\AppData\Local\Temp\PaperFootball_Phase2_Compile.log`
- `C:\Users\baley\AppData\Local\Temp\PaperFootball_Phase2_Compile2.log`
- `C:\Users\baley\AppData\Local\Temp\PaperFootball_Phase2_Scaffolder.log`

## Known Limitations

- Unity validation is blocked by licensing/package entitlement reconnect issues.
- The roguelike systems have not been manually playtested in the editor after this change.
- Consumables have runtime placement foundations but not full player-facing placement UI.
- Surface friction modifiers from upgrades are evaluated, but deeper per-encounter friction tuning may need more playtesting.
- Boss desk shake is implemented as a deterministic physics impulse, but the exact strength needs tuning.
- Precision drill uses a simple target zone and normal rest detection.
- Run layout is linear; branching is still a future extension.
- UI is functional prototype UI, not final presentation.

## Worktree Status

The worktree is uncommitted. Do not assume these changes have been committed.
