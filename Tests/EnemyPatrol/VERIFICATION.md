# Enemy patrol verification — 2026-11-12

Executed using Unity 6000.3.24f1 in batch mode against this project after the Editor was closed.

## Fixed

- Moved `NavMeshPath` construction from a MonoBehaviour field initializer to `Start`. Unity rejected the old initializer during component creation/deserialization.
- Replaced diagonal corner corridor joins with axis-aligned room entrances and open elbow junctions. Routes to room 10 use outer lanes so room 9's walls do not disconnect them.
- Rebuilt and saved the complete test scene and baked NavMesh.

## Results

- `ENEMY_PATROL_VERIFY_PASS checks=111`: all configured ordinary walking connections have complete NavMesh paths, all room centers resolve correctly, all five vent exits are on NavMesh in their destination rooms, waits match the supplied table, and zero-weight visual adjacency is preserved.
- `ENEMY_PLAY_VERIFY_PASS`: actual advancing Play Mode frames verified patrol movement/arrival, visible-player chase, loss of target and last-seen movement, recovery to room point, capture callback and player state, return to spawn, all five vent routes, suspended room/navigation state while inside a vent, no capture during vent transit, and terminal departure. Completed after 77,598 engine frames.
- The Play Mode fixture temporarily forces routes/waits and moves the target to exercise deterministic scenarios; those runtime arrangements are not saved into the scene.

Logs from this run: `outputs/EnemyAICheck/play1.log` (local generated output, ignored by Git).

## Repeat

With this project closed in other Unity instances:

```powershell
& 'C:/Program Files/Unity/Hub/Editor/6000.3.24f1/Editor/Unity.exe' -batchmode -nographics -projectPath 'C:/Users/user/Documents/GitHub/The-Dysfunctional-Asylum' -executeMethod Asylum.Enemies.Editor.EnemyPatrolPlayVerification.Run -logFile 'C:/Users/user/Documents/GitHub/The-Dysfunctional-Asylum/outputs/EnemyAICheck/play-verification.log'
```

Do not add `-quit`: the fixture enters Play Mode and exits Unity itself with success/failure status. It rebuilds the dedicated test scene before exercising it.

## Scope

This verifies the local AI fixture, not Photon Fusion synchronization, headset input, animation, or final-level geometry. XR runtime initialization emitted unavailable-runtime diagnostics on the machine without an active headset; the non-XR AI scenarios completed successfully.

## Grounded-player chase regression (follow-up)

The previous fixture observed Chase only briefly and manually enlarged captureDistance. It did not cover the playable CharacterController's grounded skin-width position.

- Reproduced before the fix: `chase-before.log` failed at stage 2, with the enemy back in Waiting instead of sustained Chase.
- Fixed EnemyTarget.NavigationPosition to use an explicit navigation anchor or the character's feet, while retaining a separate sight point for visibility. Chase destinations and capture distance now use that ground position. Capture origin also accounts for the enemy agent's baseOffset.
- Set the demo's foot-rooted agent baseOffset explicitly to zero; Unity had assigned 1 automatically when the agent was added to the capsule.
- Strengthened the fixture: settle the player through CharacterController.Move, require sustained Chase plus actual movement toward the player, and keep the default captureDistance of 1.
- After the fix: `outputs/EnemyAICheck/chase-after.log` contains `ENEMY_PATROL_VERIFY_PASS checks=111` and `ENEMY_PLAY_VERIFY_PASS ... frames=40021`, including default-distance capture and all five vent routes.
