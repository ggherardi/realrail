# Wave System and Upgrade System V2

## Goal

The run has three escalating enemy waves and five optional Upgrade Target opportunities. Targets remain independent of normal enemies and wave progression.

## Wave structure

- The run contains exactly three waves.
- Each wave has a `KillGoal`, `SpawnInterval`, Grunt `MoveSpeed`, explicit `HeavySpawnChance`, and zero or more upgrade trigger kill counts.
- `EnemySpawner` continues spawning Grunts and configured Heavy variants at the configured interval until that wave's `KillGoal` has been reached.
- Only enemies killed by player projectile damage count toward `KillGoal`. Enemies that cross the Defense Line are resolved but do not count as kills.
- Once `KillGoal` is reached, enemy spawning stops immediately. Existing enemies remain until killed or removed at the Defense Line.
- A wave completes when its `KillGoal` is reached and no enemies spawned for that wave remain alive.
- Upgrade Targets are not wave enemies for progression: they do not count toward kills or remaining-enemy checks.
- On completion, a wave immediately starts the next wave. Completing Wave 3 produces Victory.

## Upgrade Targets

- Wave 1 triggers one target on KillCount `8` of `20`; Wave 2 triggers targets on `14` and `28` of `40`; Wave 3 triggers targets on `21` and `46` of `70`.
- Trigger points are configured as integer kill counts. A configured trigger is consumed exactly once after the required player kill is registered.
- The target spawns in a randomly selected one of the two lanes, centered on that lane's X coordinate.
- Normal enemies continue spawning and advancing without interruption while a target is present.
- Once spawned, a target owns its own independent lifecycle. Its origin wave does not own, wait for, remove, or otherwise alter it.
- A target remains available across subsequent wave transitions. Targets from Waves 1 and 2 may coexist.
- A target is missed only when it reaches or passes the player; it does not damage the player.
- Player loss and Victory stop gameplay through `GameSession`; target callbacks ignore non-playing sessions safely.

## Upgrade System V2

Destroying an Upgrade Target temporarily selects one eligible reward at random and applies one level. Runtime upgrade state, reward generation, automatic selection, and application are separate: targets do not contain upgrade-effect logic. This preserves a clean seam for the planned 1-of-3 selection UI.

| Upgrade | Cap | Level behavior |
| --- | ---: | --- |
| Double Shot | 1 | Level 0 fires one projectile; Level 1 fires exactly two parallel projectiles. |
| Rapid Fire | 3 | Fire interval: 0.35s, 0.30s, 0.25s, 0.20s. |
| Piercing Shot | 2 | A projectile damages 1, 2, then 3 distinct valid targets. Duplicate callbacks cannot damage a target twice. |
| Power Shot | 2 | Projectile damage is 1, 2, then 3. Enemy Health remains authoritative. |

All effects derive from one acquired-upgrade state. At each firing cycle it produces projectile count, future fire interval, damage, and distinct-hit capacity. Projectiles receive immutable damage and capacity when fired, so later rewards do not change a shot already in flight. Capped upgrades are excluded from candidates; if all upgrades are capped, the target resolves safely with no reward. Successful rewards show brief compact HUD feedback.

Automatic random selection is temporary V2 behavior. A future roguelite milestone can generate up to three eligible candidates, show a player choice, and apply the selected one without rewriting upgrade application or weapon effects.

## Development and testing tools

Gameplay Debug Tools V1 is development-only tooling, available in the Unity Editor and development builds. `F1` toggles a compact corner debug HUD which displays the effective shot configuration from the same `UpgradeSystem` configuration used by `AutoFire`, the acquired levels, and God Mode status. `F2` toggles God Mode; it intercepts player damage only, so waves, enemies, kills, and Upgrade Targets continue normally.

Keys `1` through `4` apply exactly one level of Double Shot, Rapid Fire, Piercing Shot, and Power Shot respectively through the normal runtime upgrade application API. Caps are respected and reported. `R` resets acquired upgrade levels to baseline without resetting the wave, enemies, player position, health, kill count, or session state. These controls exist to make deterministic gameplay verification possible; they are not player-facing UI.

## Initial balance

| Wave | KillGoal | SpawnInterval | Grunt speed | Heavy chance | Upgrade triggers |
| --- | ---: | ---: | ---: | ---: | --- |
| 1 | 20 | 0.35s | 3.6 | 0% | 8 |
| 2 | 40 | 0.22s | 4.0 | 10% | 14, 28 |
| 3 | 70 | 0.14s | 4.4 | 15% | 21, 46 |

## Victory and Game Over

- Clearing Wave 3 after its KillGoal ends the run in Victory.
- Player death ends the run in Game Over.
- Both terminal states use the existing session-state mechanism; `Time.timeScale` is not changed.

## Out of scope

- Selection UI, cards, rarity, rerolls, and permanent/meta progression.
- Permanent/meta progression, currencies, inventory, shops, bosses, save/load, and procedural level generation.
