# Wave Director, Simulation, and Balance Search

## Status

Wave Director, bot profiles, seeded real-gameplay simulation, and a bounded balance-search foundation are implemented. The authored three-wave SampleScene configuration remains the protected control; no experiment writes it back.

## Authoritative runs and deterministic seeds

`SimulationRunner` accepts a base seed and derives one stable seed per run. `RunResult.RunSeed` records it. `RunRandomContext` creates independent deterministic streams for enemy lane/position/composition, upgrade-target lane/position, and reward offers; changing reward draws therefore does not perturb enemy draws. Before each simulated run the runner resets the player to its scene-authored position, bot decision state, weapon firing cadence, transient actors, selection, upgrades, telemetry, session, and wave/spawner state. Normal human gameplay continues to use Unity's automatic randomness.

The promise is scoped: same configuration, bot profile, seed, Unity version, platform, scene state, and accelerated main-thread execution route the same gameplay RNG decisions identically. `RunResult` also records ordered upgrade offer sets, authoritative selections, and final build to diagnose a divergence. Physics stepping, frame timing, and floating-point behavior mean this is not a cross-platform/network replay guarantee. Bot targeting uses scene-object discovery and can also be affected by externally introduced actors. Seeds are recorded so a divergent run can be investigated rather than hidden.

## Execution, workers, and throughput

Every `SimulationJob` has an ID, seed, execution mode, timeout, lifecycle state, result/failure, and wall-clock measurement. `SimulationJobScheduler` runs Unity scene work on the main thread and detects failure, cancellation, and timeouts. It records a requested worker count through `SimulationWorkerPlan`, but honestly reports one in-process worker: Unity GameObjects are not thread-safe. More than one worker requires isolated Unity batch/headless processes, each with a separate scene and result file; M3 does not fake that by putting multiple scene runs on threads.

The reliable fast path is accelerated, non-rendered batchmode execution of the same WaveDirector, movement, combat, projectiles, PlayerBot, upgrades, GameSession, and telemetry. Batchmode removes presentation from the automation path but remains real Unity simulation, not an abstract combat model. `SimulationJobExecution.WallClockMilliseconds` and `BalanceExperimentJsonReport` expose job count and wall-clock duration; tooling can calculate runs-per-second or simulated-time-per-wall-second. Measurements are machine/environment specific.

`SimulationJsonReport.Serialize` returns compact job JSON and `BalanceExperimentJsonReport.Serialize` returns formatted experiment JSON (baseline, ranked candidates, profile aggregates, scores, jobs, and wall-clock milliseconds). Tooling may persist these strings under an ignored development-results directory; the game does not automatically add result dumps to source control.

## Balance experiments

`BalanceObjective` contains separate `BalanceProfileObjective` ranges for each bot profile. It scores win-rate and average-duration range deviations independently per profile before summing weighted components; reports retain each profile's statistics. Average, Strong, and Perfect-ish are never reduced to one opaque survivor metric. Bot performance is not human difficulty, and Play Mode acceptance remains necessary.

`BalanceCandidateGenerator` produces copied, constrained `RunConfiguration` instances. Current legitimate dimensions are kill goal, spawn interval, speed, Heavy probability, and concurrent-enemy cap. Values are clamped to explicit bounds; upgrade trigger arrays are copied. Authored Story content is not rewritten: it may be tested as the baseline, while generated/directed configurations stay transient.

`BalanceSearch` is a bounded, deterministic guided-mutation loop: evaluate the baseline, generate only the requested candidate count × iteration count, retain the better in-memory source, rank stable ties by candidate ID, and stop. It requires explicit seeds, candidate count, iterations, constraints, and objective. It never touches a `RunDefinition` asset or SampleScene wave array.

In **RealRail > Simulation Lab**, the existing simple batch workflow remains. Set a base seed and run the same batch twice to inspect recorded seeds/results. The **Bounded Balance Experiment** section runs a deliberately small set of copied baseline/candidate configurations through all three profiles and its explicit seed set, displays baseline/candidate per-profile aggregates and scores, and keeps production waves unchanged. Its default broad objective is intentionally exploratory; choose approved targets before interpreting any winner as a design recommendation.

## Future work

Process-launching batch workers, a guarded worker-pool/result-file protocol, real-machine throughput benchmarks across one and multiple processes, additional encounter dimensions, and human-approved production tuning remain future work. No LLM controls individual gameplay frames, and no generated result establishes fun or replaces human playtesting.
