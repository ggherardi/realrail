# Run Upgrades, Progression, and Fusions

## Status and terminology

This document is the design source of truth for RealRail's future progression,
run upgrades, rarity, Fusions/Evolutions, and their visual validation. It does
not describe an implemented feature set.

- **LOCKED** — agreed system principle.
- **CANDIDATE** — promising mechanic, content, name, formula, or implementation
  direction that needs validation.
- **VISUAL NOT VALIDATED** — a concept that must be judged in a running
  prototype before approval.

## Progression architecture

RealRail has three conceptual layers:

```text
Account progression → shared permanent bonuses
Character progression → character identity and skills
Run progression → temporary weapon/upgrades → current build
```

### Account progression

**LOCKED:** Account progression is universal, permanent, capped, and moderate
in power. There is no Account Skill Tree.

Current Account Stat **CANDIDATES** are:

| Area | Stats |
| --- | --- |
| Combat | Power, Vitality, Fire Rate, Movement Speed |
| Discovery | Luck, Choice / Insight (final name TBD), Reroll |
| Economy | Bounty, AFK Efficiency |

Exact caps, values, and formulas are TBD. Crit, elemental bonuses, piercing,
and other weapon/build-specific properties should generally remain part of
character or run identity rather than universal account stats.

### Economy and acquisition

**CANDIDATE direction:** Account XP drives Account Level, unlocks, and
progression gates; Character XP drives Character Level; Coins support general
permanent progression (likely Account Stats); Character Materials may support
character-specific progression; Gems are a premium/general store currency.
Avoid unnecessary additional currencies.

Characters have no gacha direction for now. They should preferably be directly
obtainable or purchasable with transparent requirements/prices and viable F2P
routes over time. Direct purchase may accelerate access. Monetization may offer
progression/access acceleration, variety, convenience, cosmetics, bundles, or
passes, but must not create a necessary exclusive power ceiling.

Losses should still grant meaningful progression—such as Account XP, Character
XP, Coins, and possible drops/rewards—while victory can add rewards. Bounty
improves gameplay rewards; AFK Efficiency improves Automatic/AFK yield. Store
design, pricing, bundles, passes, rotations, and exact formulas remain TBD.

## Character direction

Characters and weapons are distinct systems with soft synergies. The input
budget is touch/drag movement, auto-fire, and at most three active gameplay
buttons: Character Skill, Ultimate, and Weapon Skill.

Each character conceptually has two mutually exclusive Character Skills chosen
before a run, a character-specific Ultimate, and character-specific progression
or a possible Skill Tree. A skill tree may improve both alternatives without a
respec solely to change the selected skill. Its topology and whether it can be
fully maxed are TBD.

The following are **CANDIDATE character concepts**, not final balance:

| Character | Identity | Skills / Ultimate candidates |
| --- | --- | --- |
| Rogue | Priority target, single target, debuff | Assassination; lane-based Poison Trap with Poison + Vulnerability; crit progression is appropriate. |
| Mage | AoE, elemental, control | Fireball; Blizzard; Arcane Storm Ultimate. Elemental weapon interactions remain soft, not mandatory. |
| Soldier | Weapon-build specialist | Overdrive; Tactical Reload; Full Arsenal Ultimate. It amplifies/manipulates the current weapon build rather than replacing it. |
| Engineer | Deployables, lane control | Sentry Turret; Barrier; a permanent per-run turret specialization/upgrade Ultimate with capped progression and possible post-cap Overclock. Character-owned damage must scale in long runs; the turret is a secondary mini-build. |
| Berserker | Risk, pressure, kill chains | Rage resource; Frenzy; War Cry; Bloodbath Ultimate. Avoid simplistic low-HP damage mechanics because RealRail uses a low-HP structure. |

## Free-form run upgrades

**LOCKED:** Player builds do not use rigid weapon slots. Upgrades stack freely
unless a particular incompatibility is necessary. Internal design categories
may exist, but should not constrain the player by default; intentionally wild
late-run builds are a goal.

Level and rarity are separate axes:

- **Level** is primarily quantitative. Duplicate selections increase Level up
  to an upgrade-specific cap.
- **Rarity** adds cumulative qualitative mechanics: Common `A`, Rare `A + B`,
  Epic `A + B + C`, Legendary `A + B + C + D`. Higher rarity never removes a
  lower-rarity property.

**CANDIDATE duplicate behavior:** selecting an owned upgrade increases its
Level; a newly rolled higher rarity can also raise the owned rarity; rarity
never decreases. For example, `Ice Lv1 Rare` selected as `Ice Epic` becomes
`Ice Lv2 Epic`. A max-Level upgrade may remain eligible for rarity improvement;
a max-Level Legendary is fully maxed and can leave the normal pool. Level caps,
rarity probabilities, and detailed duplicate rules require testing.

The base offer is a **CANDIDATE** of about three distinct upgrades. Choice /
Insight may provide a chance for one extra candidate. Reroll should preferably
give limited player-controlled rerolls per run, rather than another random
proc. Luck modifies rarity with capped/diminishing behavior; Legendary must
remain meaningfully rare even at high Luck. Exact probabilities are TBD.

Current base-upgrade **CANDIDATES**:

| Category | Upgrades |
| --- | --- |
| Element / status | Ice, Lightning, Fire, Poison, Explosion |
| Ballistic / behavior | Piercing, Ricochet, Spread, Rapid Fire, Power Shot, Critical, Execute, Multishot |

Names and membership may change. Fire is aggressive damage, burning, and
propagation; Poison is stacking DoT, debuff, and contamination. They must have
distinct identities.

## Fusions / Evolutions

**LOCKED:** Maxed compatible upgrades can make specifically authored
Fusions/Evolutions eligible. Fusions are not generated for every mathematical
pair: they must introduce a meaningful emergent mechanic, preferably something
that owning both ingredients independently cannot achieve. They should feel
rewarding and avoid a core penalty that makes evolution regrettable.

Eligibility does not necessarily grant a Fusion automatically; acquisition is
TBD. Multiple Fusions may coexist, one base upgrade may participate in multiple
Fusions, and no arbitrary Fusion-count cap should be imposed until gameplay
shows it is needed.

### Current Fusion concepts (CANDIDATE)

| Family | Ingredients | Fusion | Emergent direction |
| --- | --- | --- | --- |
| Elemental | Ice + Lightning | Cryo Storm | Lightning propagates Frost/control through the horde. |
| Elemental | Ice + Fire | Thermal Shock | Heat/cold stress causes fracture/burst. |
| Elemental | Fire + Poison | Napalm | Persistent incendiary zones. |
| Elemental | Fire + Explosion | Volcano | Destructible lava/rock obstacle; enemies route around it where possible or attack it, taking heat/lava damage on contact/attack. It channels the horde temporarily, has HP, and breaks. |
| Elemental | Poison + Explosion | Toxic Cloud | Persistent toxic contamination from explosions. |
| Elemental | Lightning + Explosion | Thunderburst | Radial electrical propagation from explosive impacts. |
| Elemental | Lightning + Poison | Neurotoxin | Paralysis/control; paralyzed enemies may create local congestion. |
| Elemental | Ice + Explosion | Avalanche | Frozen/shattered targets create fragments and possible cascades. |
| Ballistic | Piercing + Ricochet | Railstorm | Projectiles repeatedly traverse and redirect through groups. |
| Ballistic | Spread + Ricochet | Pinball | Dense bouncing projectile chaos. |
| Ballistic | Rapid Fire + Spread | Bullet Storm | Sustained successful combat increases firing volume/intensity. |
| Ballistic | Power Shot + Piercing | Railgun | A powerful penetrating shot visibly opens a hole/corridor through the horde; high damage alone is not its fantasy. |
| Ballistic | Critical + Power Shot | Headhunter | High-impact critical burst for priority targets. |
| Ballistic | Critical + Rapid Fire | Focus Fire | Sustained hits on one target build toward a critical payoff. |
| Mixed | Ice + Piercing | Glacial Lance | Penetrating ice attack leaves a temporary freezing/slowing line. |
| Mixed | Lightning + Ricochet | Ball Lightning | Persistent/mobile electrical projectile through the horde. |
| Mixed | Poison + Piercing | Venom Lance | Projectile becomes more toxic while passing through enemies. |
| Mixed | Fire + Power Shot | Meteor | Periodic, world-space high-impact attack arriving from above. |
| Mixed | Lightning + Rapid Fire | Tesla | Sustained hits transition into an electrical beam/network state. |
| Mixed | Lightning + Spread | Arc Field | Spread impacts/nodes create temporary electrical connections/field. |

Ice + Poison deliberately has no Fusion concept; do not invent one merely to
complete the matrix. Power Shot + Rapid Fire / **Overheat** is explicitly
discarded and is not a current Fusion candidate.

## Visual language and future validation

**LOCKED:** Late-run spectacle must not make the horde unreadable. Power should
increase the complexity and consequence of world reactions, not merely particle
count. Not every upgrade changes projectile appearance.

Useful visual channels are Projectile, Trajectory, Impact, Enemy/Target,
Ground, Area, World Object, Horde Reaction, and Camera/Feedback. A Fusion
should use a dominant channel and only necessary secondary channels:

| Fusion | Dominant channel |
| --- | --- |
| Cryo Storm | Enemy / Chain |
| Thermal Shock | Enemy |
| Napalm | Ground |
| Volcano | World Object |
| Toxic Cloud | Area |
| Thunderburst | Impact |
| Neurotoxin | Enemy |
| Avalanche | Impact / Projectile |
| Railstorm | Trajectory |
| Pinball | Trajectory / Horde |
| Bullet Storm | Weapon / Projectile Density |
| Railgun | Horde Reaction / Trajectory |
| Headhunter | Impact |
| Focus Fire | Target |
| Glacial Lance | Ground / Trail |
| Ball Lightning | World Projectile |
| Venom Lance | Projectile Evolution |
| Meteor | World / Impact |
| Tesla | Beam / Chain |
| Arc Field | Area / Network |

All current Fusion visuals are **VISUAL NOT VALIDATED**. They are concepts, not
approved final art. Multiple Fusions may coexist, but every effect need not add
a layer to every projectile. A future visual hierarchy may let a dominant
presentation (for example Railgun trajectory) coexist with enemy propagation
(Cryo Storm) and a ground trail (Glacial Lance). This is an architectural
direction only: do not lock a Visual Resolver implementation or require a
prefab for every complete build combination.

### Proposed next milestone: Fusion Visual Prototype V1

The proposed prototype should use greybox/simple VFX, validate each Fusion
individually, then stress-test all three simultaneously. It must validate
mechanics, readability, and satisfaction before final art.

| Fusion | Prototype focus |
| --- | --- |
| Volcano (Fire + Explosion) | Battlefield manipulation, destructible obstacle, routing/lateral movement, congestion, persistent-world-object readability. Formation has HP, damages attackers/contacting enemies, then breaks to reopen the path. |
| Railgun (Power Shot + Piercing) | Horde reaction, tracer readability, a visible temporary corridor/hole through dense enemies, and impact fantasy. |
| Cryo Storm (Ice + Lightning) | Elemental propagation, readable Frost chaining, and low VFX noise; favor fast sequencing over dozens of simultaneous bolts. |

Questions for validation: Is each satisfying? Is its gameplay consequence
readable? Can the three coexist visually? Can players still read enemies,
lanes, threats, horde movement, and persistent battlefield effects? Prototype
visuals are not final art.
