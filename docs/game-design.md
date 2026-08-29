# Game Design

## Concept

A 3D action game in which the player character automatically fires forward while enemies approach along two lanes.

## Core Loop

1. Enemies spawn ahead of the player.
2. Enemies advance toward the player along one of two lanes.
3. The player automatically attacks approaching enemies.
4. Enemies take damage and can be defeated.
5. The player loses if the fail condition is reached.

## Initial Scope

The first playable prototype should contain only:

- one player character;
- two enemy lanes;
- Grunt and Heavy enemy variants;
- automatic firing;
- projectile damage;
- enemy health and death;
- enemy spawning;
- a basic lose condition.

## Out of Scope for the First Prototype

For now, do not implement:

- progression systems;
- multiple weapons;
- upgrades;
- bosses;
- inventory;
- currencies;
- shops;
- multiplayer;
- online services;
- procedural levels.

## Prototype Slice

The first playable scene is a two-lane horde corridor. The player stands at a fixed Z and strafes on X. Enemies spawn on a lane and walk toward the lane-wide Defense Line. The player auto-fires along +Z from their current X; shots do not home, so you must line up with a lane to hit.

### Player
- One capsule, 3 HP.
- Move with the Input System `Move` action (A/D, arrows, or left stick X).
- Movement is clamped to the corridor. Game Over stops strafing.

### Combat
- Auto-fire spawns a small projectile at the muzzle. Projectiles travel +Z only and deal 1 damage.
- Grunts have 1 HP; Heavies have 4 HP and advance at 75% of the equivalent Grunt speed.
- Projectile hits destroy the shot and damage the enemy. At 0 HP the enemy is destroyed.
- Crossing the Defense Line deals 1 player damage once and removes that enemy. It is independent of player/enemy X position and does not count as a kill.

### Fail state
- The run is lost at 0 player HP.
- `GameSession` switches to Lost and gameplay systems stop themselves.
- `Time.timeScale` is not changed.
- There is no win condition in this slice: spawning continues until the player dies.

### Scene
`SampleScene` contains the authored gameplay structure: systems, corridor, player, camera, and HUD. Enemies, projectiles, and Upgrade Targets are instantiated from prefabs during play.
