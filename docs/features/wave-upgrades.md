# Wave System and First Weapon Upgrade

## Goal

Introduce a three-wave structure with increasing difficulty and a first temporary in-run weapon upgrade.

## Wave Structure

- The run contains 3 waves.
- Each wave has a KillGoal.
- `EnemySpawner` continues spawning enemies at the configured interval until that wave's KillGoal has been reached.
- A wave is considered complete only when:
  - the KillGoal has been reached; and
  - no spawned enemies remain alive.
- Only enemies killed by player projectile damage count toward the KillGoal.
- An enemy that reaches the player still deals contact damage and is destroyed, but does not count as a kill or advance wave progression.
- Once the KillGoal is reached, spawning stops immediately. Enemies already alive remain in play and may be killed or reach the player.
- Kills beyond the KillGoal do not advance progression further.
- The next wave starts only after the previous wave is complete.
- Difficulty should increase between waves through a simple combination of:
  - KillGoal;
  - spawn interval;
  - enemy movement speed.

Keep the balancing values simple and serialized/configurable where practical.
Each wave configuration contains `KillGoal`, `SpawnInterval`, and `MoveSpeed`.

## Upgrade Phase

- Between Wave 1 and Wave 2, and between Wave 2 and Wave 3, spawn one Upgrade Target.
- The Upgrade Target occupies one of the two lanes.
- It moves toward the player similarly to an enemy.
- It can be damaged by projectiles.
- It starts with 3 HP; this value should remain configurable.
- It must be destroyed before reaching or passing the player.
- If it reaches/passes the player, it disappears and the upgrade is lost.
- Whether the player obtains the upgrade or misses it, the next wave starts afterward.
- Upgrade Targets do not damage the player.

## Double Shot

Destroying an Upgrade Target grants Double Shot.

Before the upgrade:

- each auto-fire cycle creates one projectile.

After the upgrade:

- each auto-fire cycle creates two parallel projectiles;
- the projectiles have a small horizontal separation;
- both travel straight forward;
- Double Shot remains active for the rest of the run.

Double Shot is not stackable.

If the player already has Double Shot and destroys another Double Shot target, the weapon must remain at two projectiles per firing cycle.

## Victory

- After Wave 3 has reached its KillGoal and the field is clear, the run ends in Victory.
- Show a simple Victory message.
- Gameplay systems stop through the existing session-state mechanism.
- Do not use `Time.timeScale = 0`.
- The existing Game Over behavior must continue to work.

## Initial Balancing

Exact values may be adjusted during implementation, but the initial plan should use simple values in this general direction:

| Wave | KillGoal | SpawnInterval | MoveSpeed |
| --- | ---: | ---: | ---: |
| 1 | 5 | 1.6s | 4.0 |
| 2 | 8 | 1.2s | 4.5 |
| 3 | 12 | 0.9s | 5.0 |

Do not introduce a separate balancing framework solely for this milestone.

## Out of Scope

Do not implement:

- multiple upgrade types;
- upgrade selection UI;
- upgrade rarity;
- stacking Double Shot beyond two projectiles;
- permanent or meta progression;
- currencies;
- inventory;
- shops;
- bosses;
- save/load;
- procedural level generation.

## Acceptance Criteria

The milestone is complete when:

1. The game progresses through exactly three KillGoal-based waves.
2. Only projectile kills count toward a wave KillGoal; enemies reaching the player do not advance it.
3. A wave does not finish while enemies from that wave are still alive, even after its KillGoal is reached.
4. Difficulty visibly increases across the three waves.
5. An Upgrade Target appears between Wave 1→2 and Wave 2→3.
6. Shooting and destroying the Upgrade Target grants Double Shot.
7. Missing the Upgrade Target does not grant the upgrade and does not block progression.
8. Double Shot produces exactly two projectiles per firing cycle and does not stack further.
9. Existing enemy damage, player HP, and Game Over still work.
10. Clearing Wave 3 results in Victory.
11. Existing automated tests still pass and new testable behavior is covered where reasonable.
