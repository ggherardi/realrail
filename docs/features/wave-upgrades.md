# Wave System and Upgrade System V2

## Scope note

This document records the implemented V3 reward slice and its exact current
runtime behavior. It is not the authoritative future progression or Fusion
design. In particular, its fixed caps and simple reward behavior do not lock
the future Level/rarity model, offer rules, or free-form Fusion direction; see
[Run Upgrades, Progression, and Fusions](run-upgrades-fusions.md).

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

## Upgrade System V3

Each run now has an explicit **Run Upgrade Pool**: the identities allowed to appear as rewards for that run. The current gameplay mode initializes it with Double Shot, Rapid Fire, Piercing Shot, and Power Shot. Eligibility is derived from that immutable pool and the acquired runtime state, so capped upgrades are excluded without removing them from the pool.

Destroying an Upgrade Target asks the reward coordinator to generate up to three distinct eligible choices from the Run Upgrade Pool. The player selects one through the centered overlay; gameplay pauses while the choice is open, and the existing authoritative `UpgradeSystem` applies exactly one level before the run resumes. Two or one eligible upgrade produces that many buttons. Zero eligible upgrades resolves cleanly with no overlay or reward.

Only one selection is displayed at a time. If multiple targets resolve in the same frame, later valid rewards are queued and offered after the active choice completes. Targets still contain no reward-effect or UI logic.

The V2 automatic random application has been replaced by this player choice. The same minimal pool seam can later be populated by Story, Draft, Challenge, or Automatic mode rules without making those modes part of the current implementation.

| Upgrade | Cap | Level behavior |
| --- | ---: | --- |
| Double Shot | 1 | Level 0 fires one projectile; Level 1 fires exactly two parallel projectiles. |
| Rapid Fire | 3 | Fire interval: 0.35s, 0.30s, 0.25s, 0.20s. |
| Piercing Shot | 2 | A projectile damages 1, 2, then 3 distinct valid targets. Duplicate callbacks cannot damage a target twice. |
| Power Shot | 2 | Projectile damage is 1, 2, then 3. Enemy Health remains authoritative. |

All effects derive from one acquired-upgrade state. At each firing cycle it produces projectile count, future fire interval, damage, and distinct-hit capacity. Projectiles receive immutable damage and capacity when fired, so later rewards do not change a shot already in flight. Capped upgrades are excluded from candidates; if all upgrades are capped, the target resolves safely with no reward. Successful rewards show brief compact HUD feedback.

Candidate sampling remains random, with an injectable random source for deterministic tests.

## Development and testing tools

Gameplay Debug Tools V1 is development-only tooling, available in the Unity Editor and development builds. `F1` toggles a compact corner debug HUD which displays the effective shot configuration from the same `UpgradeSystem` configuration used by `AutoFire`, the acquired levels, and God Mode status. `F2` toggles God Mode; it intercepts player damage only, so waves, enemies, kills, and Upgrade Targets continue normally.

Keys `1` through `4` apply exactly one level of Double Shot, Rapid Fire, Piercing Shot, and Power Shot respectively through the normal runtime upgrade application API. Caps are respected and reported. `R` resets acquired upgrade levels to baseline without resetting the wave, enemies, player position, health, kill count, or session state. These controls exist to make deterministic gameplay verification possible; they are not player-facing UI.

## Railgun visual prototype (M1)

**EXPERIMENTAL / VISUAL NOT VALIDATED:** `F3` toggles the Railgun prototype; while enabled, one normal auto-fire event becomes a Railgun every 3 seconds. `F4` immediately fires one Railgun for rapid visual evaluation. `F5` injects an 18-enemy dense-horde burst through the current wave's normal spawn and accounting path. The compact debug HUD reports `RAILGUN PROTOTYPE: ON/OFF`.

The prototype Railgun is deliberately not a reward, rarity, or Fusion-acquisition implementation. Its temporary tuning is 6 damage and 24 distinct enemy hits, enough to kill current 1-HP Grunts and 4-HP Heavies while leaving existing projectile kill, enemy death, Defense Line, and Wave behavior authoritative. The Railgun travels at 80 units/second (about 3.6× normal projectile speed) and leaves a thin 0.14-second cyan tracer; this is a greybox trajectory cue, not final art. Future Fusion eligibility, persistence, UI, balance, VFX, and the remaining Fusion concepts are outside this milestone.

## Cryo Storm visual prototype (M2)

**EXPERIMENTAL / VISUAL NOT VALIDATED:** `F6` toggles the Cryo Storm prototype and `F7` immediately triggers it from the nearest real active wave enemy; `F5` remains the shared 18-enemy dense-horde setup. While enabled, Cryo Storm tries a chain every 4 seconds. The compact debug HUD reports `CRYO STORM PROTOTYPE: ON/OFF`.

Each prototype activation follows the nearest spatially valid, unvisited real wave enemy within a 4.5-unit range, with a maximum of 10 affected enemies and a 0.11-second transfer delay. Each transfer gets one short, thin cyan line; the arc disappears quickly so the horde remains legible. Affected Grunts and Heavies receive Frost for 3.5 seconds: their existing movement is slowed to 55% through `EnemyMover` without changing their authored base speed, and their existing renderers receive a temporary cool tint/emissive property block. Cryo Storm deliberately deals no damage, so it cannot create projectile kills or change Wave/Defense Line semantics.

This is only a clean M2 prototype seam, not the final Ice, Lightning, Frost/Freeze/Shatter, status, Fusion-acquisition, persistence, rarity, or visual-resolution architecture. Tuning values are for manual visual validation and are not future balance requirements.

## Volcano visual prototype (M3)

**EXPERIMENTAL / VISUAL NOT VALIDATED:** `F8` toggles periodic Volcano spawning and `F9` immediately spawns one formation during an active wave; `F5` remains the shared dense-horde setup. Only one formation may exist at once. It spawns in the left lane at Z `18`, offset `0.45` toward the outer side, with a `3.1 × 1.8` footprint, 42 HP, and a 12-second cleanup safeguard.

The formation is a real `Health` + collider world object, not a visual slow. Existing movers retain their exact forward/spawn-X behavior with no obstacle. When one blocks their local forward corridor, they select a stable lane-local bypass edge; when they reach its face before gaining sufficient lateral clearance, they stop and engage instead. The bounded per-enemy check has no NavMesh, scene search, or crowd-neighbor query, and preserves lane bounds and Frost's existing effective speed multiplier. Grunts deal 1 formation damage per one-second engagement tick; Heavies deal 2. Each engager also receives 1 volcanic Health damage per tick. Volcano deaths resolve wave enemies as **Removed**, rather than projectile kills, so they do not advance `KillCount` or upgrade triggers; standard Health/death cleanup remains authoritative.

This validates only a lightweight temporary-obstacle seam. It is not final Fire/Explosion acquisition, obstacle architecture, enemy navigation, crowd simulation, balance, VFX, or art. Because existing enemies do not collide with each other, observed congestion is local steering/engagement bunching rather than physical crowd pressure.

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

- The future rarity, reroll, and permanent/meta progression design.
- Fusions/Evolutions and their visual-resolution architecture.
- Currencies, inventory, shops, bosses, save/load, and procedural level generation.
