# Changelog

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
