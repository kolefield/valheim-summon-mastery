# Changelog

## 0.1.2

- Add opt-in **Scaling / Exclude Trollstav**, off by default. New Trollstav summons keep
  normal stats but remain tracked for dismissal. Existing summons retain their recorded stats.

- Add opt-in **Summoning / Spirit Caller wolves only**, off by default.
- Restrict new Spirit Caller casts to its existing ghost wolf prefab while preserving
  normal costs, summon limits and Summon Mastery behavior.
- Preserve shared prefab pools and other weapons, restore selection after each coroutine
  step, and fall back to normal selection if the ghost wolf is unavailable.
- Verified the actual weapon-to-spawn-ability asset link and four-creature pool.
- Build, API checks and 59 rules/coroutine checks passed; live wolves-only casting and
  Trollstav exclusion unverified.

## 0.1.1

- Add configurable dismissal shortcut, default O, with optional modifiers or None to disable.
- Dismiss the requesting player's tracked summons across the world, including waiting,
  hostile and unloaded creatures; preserve other players' summons and ordinary pets.
- Block the shortcut during text entry, menus, inventory, map, death and teleportation.
- Validate ownership on the server and remove summons without death drops or skill rewards.
- All clients and servers must update to 0.1.1 to use dismissal. Live input and multiplayer
  dismissal remain unverified.

## 0.1.0

- Add weapon-rank scaling for summoned creature health, regeneration, armor, movement and damage.
- Preserve Blood Magic and normal summon limits, faction and lifetime rules.
- Add optional travel for nearby following summons using existing network identities.
- Add configurable maximum bonuses and regression/API checks.
- Initial development build; live gameplay and multiplayer validation pending.
