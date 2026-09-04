# Wave Director and Player-Bot Foundation

## Status

Foundation implemented; batch simulation, balance search, skill-profile tuning, and procedural encounter generation remain future work.

## Run and wave execution

`WaveDirector` executes a `RunConfiguration`: a defensive runtime plan containing an arbitrary number of `WaveConfig` entries. The current authored scene retains the accepted three-wave configuration as its baseline fallback. An authored `RunDefinition` asset can create the same runtime plan, while a future directed encounter generator can construct a `RunConfiguration` directly and call `StartRun`.

Wave execution remains authoritative and unchanged in its important semantics: only projectile kills advance a kill goal; Defense Line removals do not; spawning stops as soon as the goal is reached; and a wave completes only after its spawned enemies have resolved. Upgrade Targets remain independent from this accounting. `MaxConcurrentEnemies` is an optional per-wave cap (`0` means unlimited), providing a small extension seam without treating enemy count as the sole difficulty dimension.

Difficulty is deliberately multidimensional. Composition, durability, movement speed, spawn timing, concurrency, lane distribution, and future enemy behavior may all vary independently. This foundation does not add a fixed difficulty curve or a procedural generator.

## Simulation seam

`PlayerBot` observes normal scene gameplay actors (`UpgradeTarget` and `EnemyMover`), drives the normal `PlayerMotor`, and chooses rewards through `UpgradeRewardSelection.Select`, the same authoritative path used by the human UI. It has no reference to `WaveDirector`, `RunConfiguration`, or wave parameters. It is disabled by default, so human input and selection UI remain the normal experience.

The initial policy prioritizes Upgrade Targets and otherwise positions for the nearest approaching threat. Its enablement and decision logic form the seam for future weak-to-strong skill profiles; such profiles are not implemented here.

`GameSession` reports generic gameplay facts to `RunTelemetry`, which builds a compact `RunResult`: outcome, duration, highest wave started, kills, leaks, player damage, and acquired upgrades. Telemetry records normal session facts rather than exposing Wave Director internals to the bot or analytics consumer.

## Future work

The next simulation milestone can provide headless runners, repeated independent runs, deterministic seeds, result aggregation, bot skill profiles, and candidate-wave search/tuning. Those systems are intentionally not claimed as implemented by this foundation.
