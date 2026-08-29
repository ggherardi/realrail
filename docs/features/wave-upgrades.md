# Wave System and First Weapon Upgrade

## Goal

The run consists of three escalating enemy waves. The first two waves also create a parallel, optional weapon-upgrade objective during the active horde.

## Wave structure

- The run contains exactly three waves.
- Each wave has a `KillGoal`, `SpawnInterval`, Grunt `MoveSpeed`, explicit `HeavySpawnChance`, and optional `UpgradeTriggerKillCount`.
- `EnemySpawner` continues spawning Grunts and configured Heavy variants at the configured interval until that wave's `KillGoal` has been reached.
- Only enemies killed by player projectile damage count toward `KillGoal`. Enemies that cross the Defense Line are resolved but do not count as kills.
- Once `KillGoal` is reached, enemy spawning stops immediately. Existing enemies remain until killed or removed at the Defense Line.
- A wave completes when its `KillGoal` is reached and no enemies spawned for that wave remain alive.
- Upgrade Targets are not wave enemies for progression: they do not count toward kills or remaining-enemy checks.
- On completion, a wave immediately starts the next wave. Completing Wave 3 produces Victory.

## Upgrade Targets

- Wave 1 triggers one target on KillCount `8` of `20`; Wave 2 triggers one on KillCount `16` of `40`; Wave 3 has no target.
- Trigger points are configured as integer kill counts. A configured trigger is consumed exactly once after the required player kill is registered.
- The target spawns in a randomly selected one of the two lanes, centered on that lane's X coordinate.
- Normal enemies continue spawning and advancing without interruption while a target is present.
- Once spawned, a target owns its own independent lifecycle. Its origin wave does not own, wait for, remove, or otherwise alter it.
- A target remains available across subsequent wave transitions. Targets from Waves 1 and 2 may coexist.
- A target is missed only when it reaches or passes the player; it does not damage the player.
- Player loss and Victory stop gameplay through `GameSession`; target callbacks ignore non-playing sessions safely.

## Double Shot

Destroying an Upgrade Target grants Double Shot:

- Before the upgrade, each auto-fire cycle creates one projectile.
- After it, each cycle creates two straight, parallel projectiles with a small horizontal separation.
- Double Shot lasts for the rest of the run.
- It is idempotent and non-stackable. Collecting another target after obtaining it leaves firing at two projectiles per cycle.

## Initial balance

| Wave | KillGoal | SpawnInterval | Grunt speed | Heavy chance | Upgrade trigger |
| --- | ---: | ---: | ---: | ---: | ---: |
| 1 | 20 | 0.35s | 3.6 | 0% | 8 |
| 2 | 40 | 0.22s | 4.0 | 10% | 16 |
| 3 | 70 | 0.14s | 4.4 | 15% | none |

## Victory and Game Over

- Clearing Wave 3 after its KillGoal ends the run in Victory.
- Player death ends the run in Game Over.
- Both terminal states use the existing session-state mechanism; `Time.timeScale` is not changed.

## Out of scope

- Additional upgrade types, selection UI, rarity, and stacking beyond Double Shot.
- Permanent/meta progression, currencies, inventory, shops, bosses, save/load, and procedural level generation.
