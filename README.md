# Summon Mastery 0.1.2

Summoned creatures grow stronger with the upgrade rank of the weapon that creates them.
Nearby following summons can accompany you through portals without being replaced or healed.

Initial development release for Valheim 1.0.7. Build, offline rules/API checks and plugin
initialization in Valheim have been verified. Combat balance, portal travel and multiplayer
still need gameplay validation. See the [verification report](https://github.com/kolefield/valheim-summon-mastery/blob/main/VERIFICATION.md).

## Dismiss summons

Press **O** during gameplay to dismiss all of your living summons tracked by this mod
in the current world, including waiting, hostile and unloaded summons. Other players'
summons, ordinary pets and wild creatures are left alone. Dismissal grants no drops or
skill rewards. It cannot be undone; summon new creatures if you want them back.

Change **Controls / Dismiss all summons** in a BepInEx configuration manager, or edit
`BepInEx/config/local.summonmastery.cfg` while the game is closed:

```ini
[Controls]
Dismiss all summons = O
```

Modifier shortcuts such as `O + LeftControl` are supported; `None` disables the shortcut.
The key is ignored while typing, in menus/inventory/map, during death/cutscenes or portal travel.
All clients and the server need version **0.1.1** for the new dismissal request.

## Spirit Caller: wolves only

Enable **Summoning / Spirit Caller wolves only** to make new Spirit Caller casts summon
only ghost wolves instead of randomly choosing bears, moose, wolves or boars. It is **off
by default**. Use a BepInEx configuration manager, or edit the config while the game is closed:

```ini
[Summoning]
Spirit Caller wolves only = true
```

Existing spirits remain unchanged. Normal casting costs, summon limits, Blood Magic,
weapon-rank scaling, portal following and dismissal continue through the normal paths.
Other weapons and wild wolves are unaffected. If another mod or a game update removes the
ghost wolf from the cast's selection pool, the mod logs a warning and keeps normal selection
for that cast. The option is captured at cast start; changes affect the next cast.

The selection mechanism has been verified against the installed game's assets and tested
with coroutine fixtures. Live wolves-only casting has not yet been tested. Use 0.1.2 on all
peers; this setting is per casting client, like the existing scaling settings.

## Scaling

**Scaling / Exclude Trollstav** is off by default. Enable it to leave newly summoned
Trollstav creatures at their normal health, regeneration, armor, movement and damage:

```ini
[Scaling]
Exclude Trollstav = true
```

Normal game and Blood Magic effects remain. Excluded trolls are still tracked for the
dismiss shortcut; their hostility and portal eligibility are unchanged. Existing summons
keep the stats recorded when they were cast, so dismiss and summon again after changing
this setting. Spirit Caller and Dead Raiser scaling are unaffected.

Rank means the weapon's upgrade quality, not Blood Magic level or creature stars.
Bonuses interpolate from unchanged stats at rank 1 to these configurable maximums:

| Stat | Maximum-rank bonus |
| --- | --- |
| Maximum health | 3 times normal summon health |
| Regeneration | 0.5% of maximum health per second, after 10 seconds without damage |
| Added armor | 35, using the installed game's normal armor formula |
| Movement | 30% faster walking, jogging, running, swimming and flying |
| Damage | 2.5 times normal attack damage |

For a weapon with four upgrade ranks:

| Rank | Health | Added armor | Movement | Damage | Regeneration per second |
| --- | --- | --- | --- | --- | --- |
| 1 | 1x | 0 | 1x | 1x | 0% |
| 2 | 1.667x | 11.67 | 1.10x | 1.5x | 0.167% |
| 3 | 2.333x | 23.33 | 1.20x | 2x | 0.333% |
| 4 | 3x | 35 | 1.30x | 2.5x | 0.5% |

The intention is strong same-biome support at maximum rank, especially Dead Raiser
skeletons in the Mistlands. These are initial tuning defaults, not a verified balance claim.
Existing Blood Magic, star, resistance and world-difficulty effects remain in place.
Armor still allows damage: an isolated 100-damage slash hit becomes 65 after the added
35 armor, before considering other effects. Damage restarts the healing delay. No immunity,
resurrection, attack-speed increase, or unlimited summon count is added.

Each creature records the casting weapon's rank and bonuses when summoned. Changing
weapons or upgrading the staff afterward does not change existing summons. Configuration
changes apply to newly summoned creatures. Recorded bonuses survive normal world reloads
and network ownership changes without multiplying repeatedly.

## Creature coverage

- Dead Raiser melee skeletons and archers use the game's `SpawnAbility` path covered here.
- The same hook covers other weapon-created creatures using `SpawnAbility`, including
  Trollstav's summoned troll when the weapon is passed through the normal projectile chain.
- Original factions and hostility are preserved: a hostile summoned troll becomes stronger
  unless **Exclude Trollstav** is enabled, and is **not** converted into a friendly portal companion.
- Direct attacks, attack-generated projectiles/areas and independent owned area effects
  are covered. Attack-generated areas are not multiplied twice.
- Wild creatures, ordinary pets, enemy summons, and spawned effects without a creature
  are excluded. Third-party summoning implementations that bypass `SpawnAbility` or omit
  the casting weapon need an explicit integration; universal mod compatibility is not claimed.
- Existing summons from before installing the mod cannot reveal which weapon rank created
  them. Summon them once with this mod loaded to enable tracking and portal support.

## Portal travel

Enabled by default. At departure, gather living, tamed summons within 30 meters that are
actively following their original summoner. Waiting creatures and other players' summons
stay behind. No extra eitr or health cost is charged.

The player's normal portal checks run first. When teleporting finishes, move each existing
summon network record to the player's exit point. Preserve its identity, health, equipment,
skill scaling and normal lifetime. Source-area unloading is handled through the existing
network record; the mod never creates a substitute creature. A creature that dies during
travel stays dead. Large summons/group collision at cramped or elevated exits need testing.

Distance dismissal and apparent-owner-logout dismissal are suspended for the gathered
summons for at most 45 seconds during the journey. Failed travel leaves them behind;
ordinary death and summon limits still apply, and normal distance/logout rules resume
after the journey. If you want them to come along,
keep them close and following. Portals do not make summons persist after logout.

The hook follows successful `distantTeleport` calls, also used by some portal mods and
console teleport commands. It does not bypass item restrictions or add new destinations.
Compatibility with TargetPortal remains untested.

## Installation

Install **Summon Mastery** through r2modman or Thunderstore Mod Manager, then start modded.
Keep the package enabled. If migrating from a manual copy, remove that copy first so the
plugin does not load twice.

For manual installation:

1. Exit Valheim normally before changing plugin files.
2. Put `SummonMastery.dll` in the intended profile's
   `BepInEx/plugins/SummonMastery` folder. Only one copy should be active.
3. Start the intended profile through **r2modman -> Start modded**.
4. First launch creates `BepInEx/config/local.summonmastery.cfg`.

BepInEx 5 is required. Jotunn is not required. A manual DLL is not included in normal
r2modman profile exports; send the DLL separately if sharing the profile.

For multiplayer, install this version on **every client and the server**. Damage and AI
can run on different peers, so a client-only install is insufficient. Per-creature values
are stored in its normal replicated/save data; use matching configuration on every client
to give newly created summons consistent bonuses. There is no server-enforced configuration
or version handshake in this development build. Multiplayer has not been verified.

To remove, close Valheim and remove only this DLL. Config and unrelated plugins can remain.
Spawned creatures' saved maximum health can remain until the game recalculates it; for a
clean return to vanilla, dismiss mod-created summons before uninstalling. No game assembly
is patched on disk.

Source and issue reports: [GitHub](https://github.com/kolefield/valheim-summon-mastery).
