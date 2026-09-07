# Wave Director, Simulation, and Balance Search

## Status

Wave Director, bot profiles, seeded real-gameplay simulation, and a bounded balance-search foundation are implemented. The authored three-wave SampleScene configuration remains the protected control; no experiment writes it back.

## Authoritative runs and deterministic seeds

`SimulationRunner` accepts a base seed and derives one stable seed per run. `RunResult.RunSeed` records it. `RunRandomContext` creates independent deterministic streams for enemy lane/position/composition, upgrade-target lane/position, and reward offers; changing reward draws therefore does not perturb enemy draws. Before each simulated run the runner resets the player to its scene-authored position, bot decision state, weapon firing cadence, transient actors, selection, upgrades, telemetry, session, and wave/spawner state. Normal human gameplay continues to use Unity's automatic randomness.

The promise is scoped: same configuration, bot profile, seed, Unity version, platform, scene state, and accelerated main-thread execution route the same gameplay RNG decisions identically. `RunResult` also records ordered upgrade offer sets, authoritative selections, and final build to diagnose a divergence. Physics stepping, frame timing, and floating-point behavior mean this is not a cross-platform/network replay guarantee. Bot targeting uses scene-object discovery and can also be affected by externally introduced actors. Seeds are recorded so a divergent run can be investigated rather than hidden.

## Execution, workers, and throughput

Every `SimulationJob` has an ID, seed, execution mode, timeout, lifecycle state, result/failure, and wall-clock measurement. `SimulationJobScheduler` runs Unity scene work on the main thread and detects failure, cancellation, and timeouts. It records a requested worker count through `SimulationWorkerPlan`, but honestly reports one in-process worker: Unity GameObjects are not thread-safe.

The Simulation Lab has two execution modes. **Debug / Visible Play Mode** preserves the existing single-process Editor Play Mode route for behavioural inspection. **Batch / Headless Workers** creates isolated worker project copies containing only `Assets`, `Packages`, and `ProjectSettings` (never a shared `Library`), then starts bounded external Unity processes with `-batchmode -nographics -projectPath ... -executeMethod RealRail.Editor.SimulationBatchWorker.Execute`. Each worker opens SampleScene and runs the real WaveDirector, movement, combat, projectiles, PlayerBot, upgrades, GameSession, and telemetry on Unity's main thread; it is not an abstract simulator.

The immutable JSON request includes experiment/job/candidate IDs, configuration, profile, seed, speed, and timeout. The JSON result includes matching IDs/profile/seed, terminal state or failure, `RunResult` diagnostics, worker PID, and timestamps. The coordinator accepts only matching successful output, detects launch errors, non-zero exit, timeouts, missing/malformed output, and supports cancellation. It launches at most the requested 1–8 worker processes. `BalanceExperimentJsonReport` records the actual batch execution mode, worker count, job count, wall-clock duration, simulated time, and throughput. Startup/import overhead is machine-specific and can dominate tiny workloads.

`SimulationJsonReport.Serialize` returns compact job JSON and `BalanceExperimentJsonReport.Serialize` returns formatted experiment JSON (objective, seeds, baseline/candidate configurations, profile aggregates, score components, jobs, execution mode, workers, simulated time, and wall-clock metrics). Completed Lab experiments persist one JSON file under `Application.persistentDataPath/RealRail/SimulationResults`, outside the repository and source control. The Lab displays the saved path.

## Balance experiments

`BalanceObjective` contains separate `BalanceProfileObjective` ranges for each bot profile. It scores win-rate and average-duration range deviations independently per profile before summing weighted components; reports retain each profile's statistics. Average, Strong, and Perfect-ish are never reduced to one opaque survivor metric. Bot performance is not human difficulty, and Play Mode acceptance remains necessary.

`BalanceCandidateGenerator` produces copied, constrained `RunConfiguration` instances. Current legitimate dimensions are kill goal, spawn interval, speed, Heavy probability, and concurrent-enemy cap. Values are clamped to explicit bounds; upgrade trigger arrays are copied. Authored Story content is not rewritten: it may be tested as the baseline, while generated/directed configurations stay transient.

`BalanceSearch` is a bounded, deterministic guided-mutation loop: evaluate the baseline, generate only the requested candidate count × iteration count, retain the better in-memory source, rank stable ties by candidate ID, and stop. It requires explicit seeds, candidate count, iterations, constraints, and objective. It never touches a `RunDefinition` asset or SampleScene wave array.

In **RealRail > Simulation Lab**, the existing simple batch workflow remains. Set a base seed and run the same batch twice to inspect recorded seeds/results. The **Bounded Balance Experiment** section exposes the execution mode, a conservative configurable Batch Worker Count, editable per-profile win-rate ranges, and a shared duration range. Its non-vacuous defaults are development experiment targets only, not approved production balance. It runs copied baseline/candidate configurations through all three profiles and explicit seeds, displays ranking plus actual wall-clock duration, throughput, worker/mode, simulated game time, and the saved JSON path. Production waves remain unchanged.

## Future work

The worker path preserves explicit RNG seeds and stream separation, but it is not a cross-platform/network replay guarantee: Unity physics/frame timing and floating point can still diverge. On the development machine, repeated same-seed Batch runs preserved the terminal loss/leaks/damage in a small check but differed in simulated duration; two concurrent same-seed workers also diverged in detailed progression. Treat a recorded seed as a diagnostic/reproduction input, not a promise of byte-identical results. The current Perfect-ish bot is not an expert-human proxy: future Bot Calibration should separately model execution skill and build/drafting skill. No LLM controls individual gameplay frames, and no generated result establishes fun or replaces human playtesting.
