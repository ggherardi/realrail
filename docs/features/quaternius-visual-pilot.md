# Quaternius Visual Pilot — Orc Grunt + Yeti Heavy

This controlled presentation-only pilot replaces the production visuals of the
existing Grunt and Heavy enemy prefabs without changing their gameplay roots,
collision volumes, movement, waves, or combat values.

## Source and provenance

Only these source files are imported from the local Quaternius archive
`/Users/ggherardi/Downloads/All in One - Quaternius[Patreon].zip`:

- `Characters and Animals/Ultimate Monsters - Oct 2022/Big/FBX/Orc.fbx`
- `Characters and Animals/Ultimate Monsters - Oct 2022/Big/FBX/Yeti.fbx`
- `Characters and Animals/Ultimate Monsters - Oct 2022/License.txt`

They live in `Assets/Art/ThirdParty/Quaternius/UltimateMonstersOct2022/`.
The preserved source license is CC0 1.0 Universal.

## Presentation setup

- `Enemy.prefab` retains its gameplay root and contains an Orc below `Visual`.
- `Enemy_Heavy.prefab` remains a variant of `Enemy.prefab` and replaces that
  visual-only child with a Yeti.
- Both FBX files use Generic rigs, source-authored materials, no generated
  colliders/cameras/lights, and native `Walk` clips configured to loop.
- The native Walk loop was chosen for both characters. Its one-second cadence
  is visually less hurried at the existing EnemyMover speeds than the
  source's 0.57-second Run loop.
- Animator root motion is disabled. EnemyMover remains the sole source of
  gameplay translation.

The model roots are rotated 180 degrees to face RealRail's movement direction.
The Orc is uniformly scaled to `0.6`; the Yeti uses its source scale so the Heavy remains
legible from the existing camera without changing any gameplay collision data.

## Baseline

| Character | Skinned renderers | Materials | Bones | Triangles | LODs |
| --- | ---: | ---: | ---: | ---: | --- |
| Orc | 2 | 2 | 43 | 7,344 | None in selected FBX |
| Yeti | 1 | 1 | 43 | 6,094 | None in selected FBX |

Each spawned enemy uses one Animator. This is intentionally a visual-quality
pilot; it does not alter spawning or add a production optimization system.
