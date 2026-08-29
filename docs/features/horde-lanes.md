# Horde & Lane Gameplay

## Goal

Milestone 3 changes the prototype from two sparse, aligned enemy columns into a dense horde while retaining exactly two tactically distinct lanes.

## Lane layout

- The game continues to have exactly two lanes.
- Each visual lane is 4.5 units wide, centered at X `-2.5` and X `2.5`.
- Both colored lane floors extend from the Player / Defense Line at Z `0` through the arena's far end at Z `39`.
- Enemies spawn in randomized horizontal ranges within their selected lane:
  - left: X `-4.2` to `-0.8`;
  - right: X `0.8` to `4.2`.
- An enemy retains its spawn X and moves straight toward the player.
- The player may strafe from X `-4.5` to X `4.5`.
- Upgrade Targets remain centered in their randomly selected lane; their X position is not randomized.

## Divider

- A visual, physical divider is centered at X `0`.
- It is approximately 0.4 units wide and 1.5 units high.
- It extends from Z `3` through Z `39`, leaving a clear player-side crossing area at the Defense Line and player position (Z `0`).
- Projectiles are destroyed when they hit the divider, preventing attacks through to the opposite lane.
- The divider does not collide with the player or enemies. Enemies remain in their selected lane because their movement preserves spawn X.

## Horde balance

Grunts have 1 HP. Heavies have 4 HP and use 75% of the equivalent Grunt movement speed. Initial wave values are:

| Wave | KillGoal | SpawnInterval | Grunt speed | Heavy chance |
| --- | ---: | ---: | ---: | ---: |
| 1 | 20 | 0.35s | 3.6 | 0% |
| 2 | 40 | 0.22s | 4.0 | 10% |
| 3 | 70 | 0.14s | 4.4 | 15% |

Enemies spawn at Z `36`. Crossing the lane-wide Defense Line at Z `0` damages the player once and removes that enemy, regardless of lane or X position. This is not a kill and does not advance the wave goal.

Double Shot is unchanged. The intended difficulty comes from enemy density and spatial distribution rather than increased enemy durability.

## Performance approach

This milestone retains the existing Instantiate/Destroy lifecycle. Object pooling is deferred until Play Mode profiling demonstrates allocation or frame-time pressure. Profile Wave 3 for active enemy/projectile counts, Instantiate/Destroy and GC costs, trigger/physics time, and visible frame spikes.
