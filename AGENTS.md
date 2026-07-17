# Paper Football Codex Instructions

## Project overview

This is a Unity tabletop paper football game.

The player flicks a triangular paper football across a tabletop. The game should simulate believable sliding, rotation, collisions, falling from the table, scoring, possession changes, and field-goal attempts.

Read these files before making changes:

* `ProjectSettings/ProjectVersion.txt`
* `Packages/manifest.json`
* Existing files under `Assets`
* Existing tests
* Current Git status

Do not upgrade Unity, replace the render pipeline, or install additional packages unless explicitly requested.

## Initial gameplay goal

Create a playable local prototype with:

* A tabletop playing surface.
* A triangular paper football.
* Two opposite player sides.
* Click-and-drag flick controls.
* Physics-based ball movement.
* Turn management.
* Out-of-bounds detection.
* Edge-overhang detection.
* Touchdown detection.
* A field-goal attempt mode.
* Configurable scoring.
* Score and turn UI.
* Match reset and ball reset controls.

Use placeholder models and materials. Focus on gameplay before visual polish.

## Architecture

Separate the game into the following areas:

### Rules

Use plain C# classes for:

* Scoring
* Turn transitions
* Possession
* Match state
* Win conditions
* Valid touchdown conditions
* Field-goal results

Rules should not depend directly on Unity scene objects.

### Physics

Unity components may handle:

* Rigidbody movement
* Surface friction
* Collision detection
* Table boundaries
* Falling from the table
* Football rotation
* Stopping detection

Do not calculate scores directly inside collision callbacks. Collision components should report gameplay events to the match controller.

### Input

Keep player input separate from ball physics.

The flick system should track:

* Drag start position
* Current drag position
* Drag distance
* Drag direction
* Drag duration
* Maximum permitted force
* Minimum permitted force
* Release position

Convert the final input into a validated flick command.

### Presentation

UI and visual effects should observe game state rather than control it.

The initial UI should display:

* Current player
* Current phase
* Player one score
* Player two score
* Flick strength
* Field-goal mode
* Round or possession number
* Reset instructions

## Suggested components

Use focused classes such as:

* `PaperFootballController`
* `FootballPhysicsController`
* `FlickInputReader`
* `FlickCommand`
* `FlickForceCalculator`
* `TableBoundaryDetector`
* `FootballRestDetector`
* `EdgeOverhangDetector`
* `GoalPostTrigger`
* `MatchStateMachine`
* `MatchController`
* `PaperFootballRules`
* `ScoreManager`
* `TurnManager`
* `FieldGoalController`
* `GameCameraController`
* `GameHudController`
* `PaperFootballBootstrap`

Do not place the entire game inside one MonoBehaviour.

## Match phases

Use an explicit state machine with phases such as:

* Initializing
* WaitingForFlick
* FootballMoving
* ResolvingFlick
* TouchdownScored
* FieldGoalSetup
* FieldGoalAttempt
* ChangingPossession
* MatchComplete
* Paused

State transitions must be explicit and testable.

## Configurable rules

Paper football rules vary. Store gameplay settings in a ScriptableObject or serializable configuration.

Configurable values should include:

* Touchdown points
* Successful kick points
* Number of possessions or target score
* Whether a touchdown requires an overhang
* Required overhang percentage
* Whether falling from the table changes possession
* Maximum flick force
* Minimum flick force
* Maximum drag distance
* Football stopping threshold
* Field-goal time limit
* Turn time limit

Do not scatter rule values throughout MonoBehaviours.

## Touchdown detection

A touchdown should not be based only on a trigger zone.

Determine whether:

* The football has stopped.
* The football remains supported by the table.
* Part of the football crosses the opponent’s scoring edge.
* The football has not completely fallen from the table.
* The required percentage or distance extends beyond the edge.

Create a dedicated edge-overhang calculation that can be tested independently.

## Field goals

Create a separate field-goal phase.

The field-goal system should:

* Place or orient the football at a defined kick position.
* Allow the player to aim.
* Accept a flick or kick input.
* Detect whether the football passes between the uprights.
* Detect whether it passes above the crossbar.
* Prevent the same attempt from scoring multiple times.
* Return control to the match state machine.

## Scene generation

Create a repeatable editor command under:

`Assets/Editor/PaperFootballScaffolder.cs`

The command should create or update:

`Assets/Scenes/PaperFootballGame.unity`

It should create:

* Table
* Table collider
* Floor
* Paper football
* Football Rigidbody
* Goalposts
* Player-one scoring edge
* Player-two scoring edge
* Boundary detectors
* Main camera
* Lighting
* EventSystem
* UI canvas
* Match controller
* Required serialized references

The scaffolding operation must be safe to run more than once without creating duplicate objects.

Avoid manually editing large Unity YAML scene files.

## Repository rules

* Never modify `Library`, `Temp`, `Logs`, `obj`, or generated solution files.
* Do not add paid assets.
* Do not add external assets without permission.
* Do not overwrite existing functional systems without inspecting them first.
* Avoid global mutable singletons.
* Avoid `FindObjectOfType` for routine object references.
* Prefer serialized references, dependency injection, or bootstrap wiring.
* Use namespaces beginning with `PaperFootball`.
* Keep runtime scripts outside editor-only folders.
* Keep editor automation inside `Assets/Editor`.
* Use `FixedUpdate` for Rigidbody physics operations.
* Do not perform per-frame object searches.
* Validate null references and invalid configuration values.
* Keep tuning values configurable.
* Prefer small, focused classes.

## Testing

Add Edit Mode tests for:

* Match-state transitions
* Turn changes
* Scoring
* Target-score win conditions
* Flick-force calculations
* Flick-force clamping
* Overhang calculations
* Out-of-bounds resolution
* Field-goal scoring
* Duplicate-score prevention
* Match reset

Add Play Mode tests for:

* Football settling after movement
* Football falling from the table
* Scoring trigger integration
* Scene bootstrap references

## Validation requirements

After changing the project:

1. Run Unity in batch mode.
2. Verify that all scripts compile.
3. Run Edit Mode tests.
4. Run relevant Play Mode tests.
5. Review Unity logs.
6. Review Git status.
7. Review the complete diff.

Do not claim compilation or tests passed unless Unity actually completed the commands successfully.

## Development approach

For larger requests:

1. Inspect the repository.
2. Explain the current implementation.
3. Produce a concise plan.
4. List the files that will change.
5. Implement one playable vertical slice.
6. Run validation.
7. Summarize results and known limitations.

Do not build unrelated features during a focused task.
