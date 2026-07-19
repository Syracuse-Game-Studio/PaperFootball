# Paper Football Spin And Force Handoff Report

Date prepared: 2026-07-18
Project path: `G:\Unity\Games\PaperFootball`
Unity version: `6000.0.68f1`

## Purpose

This report summarizes the recent paper football physics tuning work for another ChatGPT/Codex session to intake quickly. The goal of the change was to make the physical paper football spin based on where the user flicks it, and to reduce normal flick force because even low-strength flicks were sending the football over the table edge.

## User Request

The user wanted:

- The paper football to spin based on where it is flicked from.
- Less force for normal flicks because anything above about 15% strength went over the table edge.
- Physics-based behavior, not a predetermined or canned spin amount.

## High-Level Result

Normal flicks now carry the actual world-space contact point from the initial collider raycast. The physics controller queues the impulse and applies it in `FixedUpdate` using `Rigidbody.AddForceAtPosition`. This means the Rigidbody receives torque from the real lever arm between the hit point and the center of mass. A centered flick mostly slides, while off-center flicks naturally yaw/spin in opposite directions depending on which side is hit.

The normal flick force tuning was also reduced and made less aggressive at low drag strengths. The default rule values are now:

- `minimumFlickForce = 0.35`
- `maximumFlickForce = 4`
- `flickForceResponseExponent = 1.6`
- `maximumDragDistance = 2.5`
- `minimumDragDistance = 0.05`
- `footballAngularDamping = 0.8`
- `contactYawTorqueMultiplier = 2.5`
- `maximumFootballAngularVelocity = 24`

The response exponent means displayed flick strength can still span 0-100%, but early drag values produce softer force. For example, a low-percent flick should now behave more like a controlled tabletop nudge instead of a launch.

## Visible Spin Update

A later visible-spin pass expanded this implementation so the spin is clear to the player, not only present in Rigidbody data.

- `FootballPhysicsController` now exposes the last applied contact point, center of mass, applied impulse, contact lever-arm distance, and angular damping.
- The controller draws a temporary debug line from center of mass to the applied contact point after a flick.
- `footballAngularDamping` is now part of `PaperFootballRuleSet` and defaults to `0.8`, preserving visible yaw while still allowing damping to slow the spin.
- `contactYawTorqueMultiplier` adds a physics-based yaw torque impulse derived from `cross(contact lever arm, shot impulse)`. This amplifies the real off-center contact torque without assigning canned angular velocity.
- `maximumFootballAngularVelocity` raises the Rigidbody cap to `24` rad/s so strong off-center hits can visibly spin.
- `FootballPhysicsController.IsMoving` now considers angular velocity as well as linear velocity.
- A new `FootballSpinDebugOverlay` shows linear velocity, angular velocity, yaw angular velocity, current Y rotation, applied contact point, center of mass, contact distance, and angular damping.
- The scaffolder now adds `FootballFoldLine` and `FootballCornerMark` child meshes under the Rigidbody football so orientation is readable from the tabletop camera. These are parented to the actual football object and are not separate world-space markers.
- The actual Rigidbody root now stays aligned to the table/world axes; the generated `PaperFootballVisual` child is rotated flat. This keeps `RigidbodyConstraints.FreezeRotationX | FreezeRotationZ` from blocking real tabletop Y spin.
- The scaffolder continues freezing Rigidbody rotation X/Z while leaving Y rotation unfrozen.

## Important Implementation Details

### Input

`Assets/Scripts/Tabletop/Input/FlickInputReader.cs`

- `TryStartDrag` already raycasted against the football collider.
- The implementation now stores `hit.point` in `dragContactWorld`.
- Drag preview and release calculations pass that contact point into `FlickForceCalculator`.
- Invalid previews also preserve the contact point so state remains coherent through cancel/release.

`Assets/Scripts/Tabletop/Input/FlickCommand.cs`

- Added `ContactPointWorld`.
- Added `HasContactPoint`.
- Kept the old constructor for compatibility with existing tests and code.
- Added a contact-point constructor for real input and focused tests.

`Assets/Scripts/Tabletop/Input/FlickForceCalculator.cs`

- Added overload accepting `contactPointWorld`.
- Preserves the previous public `Calculate(...)` signature by forwarding to the new overload.
- Replaced direct linear force interpolation with `Mathf.Pow(strength01, rules.flickForceResponseExponent)` before force interpolation.
- `Strength01` remains based on drag distance for HUD/aim display.

### Physics

`Assets/Scripts/Tabletop/Physics/FootballPhysicsController.cs`

- Removed the old serialized `spinImpulse` field and the manual yaw torque logic.
- Added a pending impulse struct and applies queued impulses in `FixedUpdate`.
- Normal flicks call `QueueImpulse(command.Direction * command.Force, command.HasContactPoint, command.ContactPointWorld)`.
- Field goal kicks also preserve and pass through their contact point.
- If a contact point is available, the controller applies force via `body.AddForceAtPosition`.
- If no contact point is available, it falls back to `body.AddForce`.
- `ResolveApplicationPoint` uses `Collider.ClosestPoint` to keep the hit point on the football collider.
- When `constrainFlipping` is true, the application point's Y is flattened to `body.worldCenterOfMass.y` to allow yaw spin without introducing unwanted pitch/roll flipping.
- Rigidbody operations stay in the physics step.
- Applies rule-driven angular damping through `rules.footballAngularDamping`.
- Applies rule-driven maximum angular velocity through `rules.maximumFootballAngularVelocity`.
- Applies supplemental yaw torque from the real contact lever arm and impulse through `rules.contactYawTorqueMultiplier`.
- Records spin debug data without applying canned rotation or arbitrary post-shot angular velocity.

### Field Goals

`Assets/Scripts/Tabletop/FieldGoals/FieldGoalKickResult.cs`

- Added `ContactPointWorld` and `HasContactPoint`.
- Kept the original constructor for compatibility.

`Assets/Scripts/Tabletop/FieldGoals/FieldGoalKickCalculator.cs`

- Carries the flick command contact point into the field goal kick result.
- This keeps field-goal kicks consistent with normal flicks if the player hits the ball off-center.

### Rules And Defaults

`Assets/Scripts/Tabletop/Rules/PaperFootballRuleSet.cs`

- Changed default normal flick force from `1.5-18` to `0.35-4`.
- Added `flickForceResponseExponent = 1.6f`.
- Added `footballAngularDamping = 0.8f`.
- Added `contactYawTorqueMultiplier = 2.5f`.
- Added `maximumFootballAngularVelocity = 24f`.
- Sanitizes exponent to at least `0.1`.

`Assets/Materials/PaperFootballPrototype/DefaultPaperFootballConfig.asset`

- Updated existing runtime config asset with the new force values and exponent.

`Assets/Editor/PaperFootballScaffolder.cs`

- Updated generated/default config values so future scaffolding creates the softer force settings.

## Tests Added Or Updated

`Assets/Tests/EditMode/FlickForceCalculatorTests.cs`

- Added a test proving the response exponent softens low-strength force.
- Added a test proving the contact point is preserved in `FlickCommand`.

`Assets/Tests/PlayMode/FootballPhysicsPlayModeTests.cs`

- Added/expanded tests for:
  - centered flicks producing less yaw angular velocity than off-center flicks,
  - left/right contact points producing opposite yaw directions,
  - off-center flicks changing the transform Y rotation,
  - root-aligned tabletop footballs producing visible Y yaw from off-center flicks,
  - the football continuing to rotate beyond the first physics step,
  - yaw slowing because of angular damping,
  - rest detection refusing to complete while angular velocity is above threshold,
  - manual reset restoring the expected rotation.

`Assets/Tests/PlayMode/SceneBootstrapPlayModeTests.cs`

- Verifies the generated scene contains `FootballSpinDebugOverlay`.
- Verifies `FootballFoldLine` and `FootballCornerMark` exist under the Rigidbody football.
- Verifies `PaperFootballVisual` is the rotated flat child while the Rigidbody root has no X/Z tilt.
- Verifies the football Rigidbody does not freeze `RigidbodyConstraints.FreezeRotationY`.

## Files Changed In This Work

- `Assets/Docs/CodexImplementationReport.md`
- `Assets/Docs/PaperFootballPrototypeNotes.md`
- `Assets/Editor/PaperFootballScaffolder.cs`
- `Assets/Materials/PaperFootballPrototype/DefaultPaperFootballConfig.asset`
- `Assets/Scripts/Tabletop/FieldGoals/FieldGoalKickCalculator.cs`
- `Assets/Scripts/Tabletop/FieldGoals/FieldGoalKickResult.cs`
- `Assets/Scripts/Tabletop/Input/FlickCommand.cs`
- `Assets/Scripts/Tabletop/Input/FlickForceCalculator.cs`
- `Assets/Scripts/Tabletop/Input/FlickInputReader.cs`
- `Assets/Scripts/Tabletop/Physics/FootballPhysicsController.cs`
- `Assets/Scripts/Tabletop/Presentation/FootballSpinDebugOverlay.cs`
- `Assets/Scripts/Tabletop/Rules/PaperFootballRuleSet.cs`
- `Assets/Tests/EditMode/FlickForceCalculatorTests.cs`
- `Assets/Tests/PlayMode/FootballPhysicsPlayModeTests.cs`
- `Assets/Tests/PlayMode/SceneBootstrapPlayModeTests.cs`

This report file was added after the implementation:

- `Assets/Docs/SpinForcePhysicsHandoffReport.md`

## Validation Status

Validation was rerun after the visible-spin update:

- Unity batch compile completed with exit code `0`.
- Compile log contained `Tundra build success` and `Batchmode quit successfully`.
- No `error CS` or `warning CS` entries were found in the targeted compile log scan.
- Scaffolder completed with exit code `0` and rebuilt `Assets/Scenes/PaperFootballGame.unity` plus `Assets/Scenes/PaperFootballLauncher.unity`.
- Edit Mode tests passed: `40 passed, 0 failed`.
- Play Mode tests passed via Unity XML: `21 passed, 0 failed`, duration `8.7292562s`.
- `git diff --check` reported no whitespace errors. It only emitted line-ending normalization warnings.

Log note: Unity emitted `[Licensing::Module] Error: Access token is unavailable; failed to update` during batch runs. This did not block compilation, scaffolding, or tests.

Process note: During Play Mode validation, Unity wrote a passing XML result file but did not exit on its own. The batch-mode Unity process was closed manually after confirming the XML showed `21 passed, 0 failed`.

## Current Worktree Notes

At the time this report was prepared, the worktree contains uncommitted changes from this implementation. Do not assume these changes are committed.

The active tabletop prototype uses the newer `PaperFootball.Tabletop.Physics.FootballPhysicsController`. There is also an older `Assets/Scripts/Ball/PaperFootballPhysics.cs` script in the repo; it was not changed for this task because the generated prototype scene is wired through the tabletop controller.

## Suggested Next Steps

- Playtest the new `0.35-4` force range in the Unity editor and tune from feel.
- If 15% is still too strong, raise `flickForceResponseExponent` or lower `maximumFlickForce`.
- If full-strength flicks become too weak, raise `maximumFlickForce` slightly while keeping the exponent.
- Consider exposing the current force curve in a debug overlay so tuning can happen while watching actual impulse values.
- Eventually consider a more accurate triangle collider or compound collider. Touchdown and contact behavior currently still rely on a box collider around the triangular mesh.
