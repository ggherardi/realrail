# Feature Backlog

## Future Game Modes

Status: Future / Not Implemented

All future modes can configure the Run Upgrade Pool, the explicit list of upgrades allowed to appear during a run. They do not imply an implemented mode-selection flow, backend, or leaderboard.

### Story

Progression may move through eras, with era-dependent weapons, upgrades, or other content. The relationship between base-weapon identity and run upgrades remains deliberately TBD.

### Draft

Before a run, the player may configure its Run Upgrade Pool. There is intentionally no minimum pool size, allowing deliberate small-pool build experimentation as well as broad pools.

### Challenge

A system-defined pool and rules can provide fair shared conditions. Scoring, leaderboards, networking, and possible deterministic seeds are future work.

### Automatic

Automated combat can use configured/unlocked loadouts and upgrades to determine progression, with rewards based on level or wave reached. This is related to the planned AFK-style Auto Combat feature, but is not a manual-gameplay simulation and is not implemented here.

## Player Build / Weapon Stats Screen

Status: Planned

A future player-facing run screen should show the effective weapon statistics, acquired upgrades, component effects, and eventually synergies, status effects, and elemental properties. The Gameplay Debug HUD is development tooling for deterministic testing and is not this player feature; its final UI/UX remains to be designed.

---

## Auto Combat / Auto Progression
Status: Planned

Modalità separata dal gameplay principale in cui il personaggio affronta
automaticamente orde successive.

### Core concept
- Nessun upgrade temporaneo durante il combattimento.
- La performance dipende dalla progressione permanente del giocatore.
- Progressione attraverso stage/wave.
- Il giocatore continua finché la build non riesce più a superare uno stage.
- Orde differenti dal gameplay normale e progettate per favorire build diverse.

### Progression
- Nuove armi sbloccate raggiungendo determinati Auto Stages.
- Le armi sbloccate possono essere utilizzate anche nel gameplay principale.
- Possibili milestone con passive, weapon slot o altre ricompense.

### Future
- Ricompense offline basate sul massimo Auto Stage raggiunto.

---

## Weapon Era Progression

Status: Planned

The weapon progression should follow the technological evolution of warfare, starting from ancient weapons and gradually reaching futuristic and science-fiction weapons.

### Core concept

* Weapons are organized into technological eras.
* Progressing through Auto Combat unlocks increasingly advanced weapon eras.
* Reaching a new era should feel like a major progression milestone, not just a numerical power increase.
* Weapons from different eras should have distinct mechanics rather than simply higher damage values.

### Possible eras

* Ancient Age — sling, bow, spear, javelin.
* Medieval Age — longbow, crossbow and other mechanical projectile weapons.
* Gunpowder Age — arquebus, musket, flintlock weapons.
* Industrial Age — revolver, lever-action rifle, early machine guns.
* Modern Age — shotgun, assault rifle, sniper rifle, SMG.
* Advanced Age — smart weapons, drones, experimental kinetic weapons.
* Sci-Fi Age — laser, plasma, railgun and other futuristic weapon technologies.

### Gameplay principles

* Older weapons should remain mechanically distinctive even after more advanced eras are unlocked.
* New eras can introduce new combat mechanics such as:

  * piercing projectiles;
  * reload systems;
  * sustained automatic fire;
  * explosive damage;
  * homing or smart projectiles;
  * energy beams;
  * chaining attacks;
  * other mechanics that break the rules established by previous eras.
* Weapon variety should encourage different builds instead of making every newly unlocked weapon a direct replacement for older ones.

### Auto Combat integration

* Auto Combat progression can be divided into eras rather than being represented only by stage numbers.
* Reaching specific Auto Combat milestones unlocks new weapons and eventually entire technological eras.
* Enemy waves and environments may also evolve alongside the current era.
* Different Auto Combat stages can favor different weapon characteristics and builds.

Example progression:

`Ancient Age → Medieval Age → Gunpowder Age → Industrial Age → Modern Age → Advanced Age → Sci-Fi Age`

### Future considerations

* Decide whether players can freely mix weapons from different eras.
* Define whether reaching a new era changes only weapon availability or also enemies, environments and visual style.
* Determine the exact relationship between Auto Combat stages, weapon unlocks and era transitions.

---

## Modular Weapon System

Weapons are composed of multiple interchangeable components that, when combined,
define the final weapon behavior.

Each component can modify one or more aspects of the weapon, such as:

- damage type;
- projectile behavior;
- fire rate;
- spread;
- range;
- status effects;
- elemental properties;
- special interactions.

The important part is that components should not always produce purely additive
benefits. Some combinations may interact poorly or even partially cancel each
other.

Example:

- one component causes the weapon to apply Ice;
- another component converts the weapon to Fire;
- mounting both may make the Ice component ineffective, inefficient, or create
  a different interaction depending on the final system design.

This should make weapon construction a meaningful build decision rather than a
simple sequence of upgrades.

Players will therefore need to reason about which components work well together
when assembling a weapon.

Weapons and/or their components are intended to be discovered after a run has
already started, so the player will progressively assemble and adapt their build
during the match rather than always entering with a fully predefined weapon.

The exact component slots, compatibility rules, elemental interactions, rarity,
and acquisition system are still to be designed.

---

## Lane Floor Effects

Status: Planned

The floor of one or both lanes can temporarily change state and modify the rules of combat for anything happening on that lane.

### Core concept

* A lane can acquire a temporary floor effect, visually communicated through a clear color, material or animation change.
* Floor effects can alter weapon behavior, enemy behavior, player statistics or general combat rules.
* The player should be able to recognize the effect quickly and decide whether to remain on that lane or switch to the other one.
* Effects should create tactical decisions rather than simply applying unavoidable penalties.

### Example

**Healing Floor**

* The lane floor turns yellow.
* While the effect is active, attacks that would normally damage enemies instead heal them.
* The player is encouraged to immediately stop firing on that lane or move to the other lane.

### Possible effects

* **Damage Amplification** — attacks deal increased damage.
* **Damage Reduction** — attacks deal reduced damage.
* **Healing Floor** — weapon damage heals enemies instead.
* **Critical Zone** — increased critical hit chance or guaranteed critical hits.
* **Slow Zone** — enemies moving through the lane are slowed.
* **Acceleration Zone** — enemies move faster.
* **Weapon Overcharge** — increased fire rate or projectile speed.
* **Weapon Jam** — reduced fire rate or temporary firing interruptions.
* **Explosive Floor** — enemies killed on the lane trigger explosions.
* **Projectile Modifier** — projectiles gain piercing, bouncing, splitting or other properties.
* **Elemental Zone** — attacks gain a temporary elemental effect.
* **Enemy Buff Zone** — enemies gain armor, regeneration or another temporary advantage.

### Gameplay principles

* Effects should be highly readable through visuals and, where necessary, UI feedback.
* Positive and negative effects can both exist.
* Some effects may apply only to the player, only to enemies or to both.
* Effects should encourage lane switching and make positioning more meaningful.
* Avoid effects that remove player agency or are difficult to understand during fast gameplay.

### Future considerations

* Define whether floor effects appear randomly, follow predefined patterns or are triggered by specific enemies/events.
* Determine whether both lanes can have different effects at the same time.
* Explore interactions between floor effects and specific weapons, upgrades or enemy types.
* Consider rare combinations where multiple floor modifiers can be active simultaneously.

---
