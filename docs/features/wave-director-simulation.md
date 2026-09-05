# Wave Director and Player-Bot Foundation

## Status

Wave Director, bot skill profiles, and opt-in batch simulation are implemented. Headless/parallel execution, deterministic replay, automated balance search, and procedural encounter generation remain future work.

## Run and wave execution

`WaveDirector` executes a `RunConfiguration`: a defensive runtime plan containing an arbitrary number of `WaveConfig` entries. The current authored scene retains the accepted three-wave configuration as its baseline fallback. An authored `RunDefinition` asset can create the same runtime plan, while a future directed encounter generator can construct a `RunConfiguration` directly and call `StartRun`.

Wave execution remains authoritative and unchanged in its important semantics: only projectile kills advance a kill goal; Defense Line removals do not; spawning stops as soon as the goal is reached; and a wave completes only after its spawned enemies have resolved. Upgrade Targets remain independent from this accounting. `MaxConcurrentEnemies` is an optional per-wave cap (`0` means unlimited), providing a small extension seam without treating enemy count as the sole difficulty dimension.

Difficulty is deliberately multidimensional. Composition, durability, movement speed, spawn timing, concurrency, lane distribution, and future enemy behavior may all vary independently. This foundation does not add a fixed difficulty curve or a procedural generator.

## Simulation and batch runs

`PlayerBot` observes normal scene gameplay actors (`UpgradeTarget` and `EnemyMover`), drives the normal `PlayerMotor`, and chooses rewards through `UpgradeRewardSelection.Select`, the same authoritative path used by the human UI. It has no reference to `WaveDirector`, `RunConfiguration`, or wave parameters. It is disabled by default, so human input and selection UI remain the normal experience.

The bot exposes three algorithmic decision profiles: `Average` observes less often and uses simple nearby-target/upgrades preferences; `Strong` reacts faster, prioritizes imminent threats, and avoids repeatedly developing the same offered upgrade; and `Perfect-ish` reacts fastest with the strongest currently visible-choice heuristic. These profiles never change player health, weapon damage, fire rate, enemy stats, waves, or upgrade caps. They model bot decision quality only, not human difficulty.

`GameSession` reports generic gameplay facts to `RunTelemetry`, which builds a compact `RunResult`: outcome, duration, highest wave started, kills, leaks, player damage, and acquired upgrades. Telemetry records normal session facts rather than exposing Wave Director internals to the bot or analytics consumer.

`SimulationRunner` is opt-in development tooling on the scene's `Systems` root. It runs one real gameplay session at a time at a configurable `Time.timeScale` multiplier (default `4x`), records one result per terminal session, and restarts automatically. It deliberately uses the normal `Update`/scaled-time gameplay paths rather than a mathematical combat replacement. Reward selection retains its existing pause behavior: the selection restores the prior simulation scale when the bot selects through the normal authoritative path.

Before a fresh run the runner resets the session health/state/time, telemetry, upgrades, reward selection, wave/spawner state, bot input, and removes transient enemies, projectiles, and upgrade targets. This is an explicit lifecycle seam rather than scene reload. Normal gameplay is unaffected because the runner and bot are disabled by default.

Finite batches retain all `RunResult` instances and produce a `RunStatistics` summary with profile, run count, wins/losses, win rate, mean/median duration and wave, average kills/leaks/damage, defeat/wave distributions, and aggregate upgrade/build distributions. `ToReport()` is a compact human-readable rendering; its properties are the programmatic interface for later tooling.

For the normal development workflow, open `SampleScene`, then **RealRail > Simulation Lab**. Choose a profile, run count, and speed, then click **Run Batch**. The Lab enters Play Mode when needed, configures the actual `PlayerBot` and `SimulationRunner`, and renders the runner's completed `RunStatistics` directly. A second batch with another profile can start in the same Play Mode session after the first completes. **Stop Batch** returns control and restores normal time/input. The `Tools/RealRail/Configure Batch Simulation` command repairs or adds the serialized development setup without enabling it.

## Determinism and next steps

Current enemy lanes/heavy selection, upgrade offers, and upgrade-target lanes use `UnityEngine.Random`; M2 does not seed or replay this shared source. The batch APIs avoid hiding that limitation, so M3 can introduce a narrow seeded-random seam without replacing real gameplay. M3 can also add headless/parallel workers and autonomous candidate-wave search; none are implemented here.

## Future work

Future work is headless/parallel execution, deterministic seeds/replay, and candidate-wave search/tuning. Bot results must not be presented as proof of human difficulty.
