# Verification — 0.1.2 — 2026-09-13

## Feasibility confirmed before implementation

Read the installed game's SoftRef manifest and asset bundle with UnityPy 1.25.3 in an ignored local inspection directory. No game files were modified.

- `Assets/GameElements/Items/weapons/StaffSpiritCaller.prefab`: weapon name `StaffSpiritCaller`, maximum quality 4; its ItemDrop attack projectile references the spawn ability below.
- `Assets/GameElements/Items/weapons/_res/staffs/staff_SpiritCaller_spawn.prefab`: a standard SpawnAbility with the pool `Bjorn_spiritcaller`, `Moose_spiritcaller`, `Wolf_spiritcaller`, `Boar_spiritcaller`.
- The ability uses command-on-spawn and copies Blood Magic; max-instance handling remains the game's existing code. Its 2.5-second pre-spawn delay makes coroutine-step restoration relevant.
- This confirms the feature can filter a cast's existing pool without replacing creatures afterward or changing shared game assets.

## Trollstav exclusion added

Verified the installed weapon asset is exactly StaffRedTroll. The new Scaling / Exclude Trollstav setting defaults to false. Enabled casts record neutral values for all five stats and skip initial health changes while retaining dismissal tracking. Five additional regression checks cover all neutral stats, disabled behavior, other weapons and exact identity matching. Existing summons retain their recorded settings. Live Trollstav casting, toggling, dismissal and reload behavior remain unverified.

## Checks completed

- Release build: zero errors and the two existing transitive reference warnings.
- 59 executable checks passed, including actual four-entry pool selection, disabled/unrelated/unfinished weapon exclusions, missing/null prefab fallback, non-mutation, delayed coroutine steps, completion and exception restoration, plus prior scaling/dismissal/armor rules.
- 109 game/Unity member references and all reflected/patch targets checked against the installed assemblies. No additional Harmony targets were introduced.
- New setting is `Summoning / Spirit Caller wolves only`, false by default. The setting and selection are captured per cast; existing summons are not converted.
- DLL SHA-256: `86D3C0401D82177A7ACDDF8C8CBC5F7EA34D00BA53C39149DE98AB1932EE5BB5`.

Valheim was running. No active DLL/config/save was changed, and no game was launched. Live repeated casting, costs/caps, co-op behavior and configuration-manager toggling remain unverified. In a disposable world, enable the option and cast repeatedly, confirm every new spirit is a ghost wolf, check rank scaling/portal/dismissal, disable it and verify mixed summons return. Also confirm Dead Raiser and Trollstav retain their original summon types.

## Historical verification
# Verification — 0.1.1 — 2026-09-13

- Release build against the current installed Valheim assemblies and Gale Default BepInEx 5.4.2350 passed (zero errors; two existing transitive assembly warnings).
- 36 executable rules checks passed, including own-summon dismissal eligibility, rejection of other players, untracked creatures, missing identity and dead records.
- 109 game/Unity references resolved with public accessibility. All 12 Harmony targets, Player.TakeInput and reflected/injected fields matched the installed game.
- Dismissal enumerates the server's recorded summoner keys; no range, follow or tame filter excludes waiting, hostile or unloaded summons. The server obtains player identity from the requesting peer rather than trusting a submitted identity.
- Default O is a BepInEx KeyboardShortcut config entry; modifier combinations and None are supported. Input is checked every frame before discovery throttling and gated by vanilla Player.TakeInput.
- Reviewed network session re-registration, server-only deletion, result-sender validation, rate limiting and deletion without death rewards. These are code/API checks, not live networking tests.
- Valheim was running during this update. The active DLL was not replaced. Live shortcut input, configuration-manager rebinding and multiplayer/unloaded-world dismissal remain unverified.
- The user reported a Spirit Caller summon following through a portal with 0.1.0; exact combat bonuses were not measured.

DLL SHA-256: `D39C46213B8E503A422D20DC1D776C1ADEC5C8E19404929B0E694CB1BDF63CBE`

Before claiming full gameplay verification, test O and a rebound modifier shortcut; press them in chat, console, menus, inventory and map; dismiss own following/waiting/hostile and unloaded summons; ensure another player's tracked summons and ordinary pets remain; test two clients plus a dedicated server all on 0.1.1; verify no loot and no portal recreation after dismissal. Use disposable saves.

## Historical 0.1.0 verification
# Verification — 2026-09-10

## Completed

- Release build with .NET SDK 8.0.424 against the installed, unmodified Valheim 1.0.7
  `assembly_valheim.dll` and the Default profile's BepInEx 5.4.23.5 assemblies.
  Zero errors; two transitive assembly-version warnings described in BUILDING.md.
- 31 passing executable checks: rank boundaries and intermediate bonuses, different
  weapon maximum ranks, monotonic health, regeneration delay/cap/death, travel eligibility
  exclusions, and the installed game's actual `HitData.ApplyArmor` calculations.
- 91 game/Unity member references resolve and are public in the installed assemblies.
- All 12 Harmony target signatures and six injected/reflected fields match the installed
  binary. The armor transpiler has exactly one void resistance-call insertion anchor.
- Source inspection followed weapon data through SpawnAbility and normal projectile
  chaining, skill setup, NPC damage, player teleport completion, scene unloading and both
  distance/owner-disappearance dismissal paths.
- Found and corrected a health-reload issue during review: scaling after maximum-health
  assignment could clamp away health. Scaling now happens before vanilla assignment.
- Valheim startup log confirms BepInEx loaded Summon Mastery 0.1.0 and reached its
  successful initialization message after Harmony patch installation. This verifies startup,
  not combat, saved progression, or portal behavior.
- Package contents are restricted to the mod DLL, README, changelog, this report,
  MIT license, manifest and original 256x256 PNG icon. The packaging script verifies
  the archived DLL's SHA-256 matches the build.

DLL SHA-256:
`41BB47C315F5C651AB0D21508317EBB5B793DDBBC09FAFF8A3A613EE29BC2570`

The armor test initially assumed elemental damage bypassed armor. The installed game's
actual formula disproved that assumption; the test and description now reflect the binary:
35 armor reduces isolated 100 slash or fire damage to 65, while generic untyped damage bypasses it.

## Runtime limits

An attempted isolated .NET 8 Harmony installation probe failed inside the installed
Harmony build's `AccessTools` type initializer (`MethodInvoker.GetHandler`), before reaching
the mod patches. This was a host/harness failure. Subsequent Valheim startup verified
plugin initialization in the intended Unity runtime. The unusable probe was removed;
future runtime checks must use Valheim rather than the standalone .NET host.

Rules and static checks are **not** live gameplay. Coroutine execution, Unity physics,
save/rejoin behavior, actual damage application, AI movement, portal travel, TargetPortal
compatibility and multiplayer remain unverified. Initial balance is a design target.

## Focused in-game acceptance checks

Use an isolated r2modman test profile and new test world/character, with separate saves.
Exit the current gameplay session normally before installing/launching tests.

1. Start modded with only BepInEx and this DLL. Verify one Summon Mastery 0.1.0 startup
   message and no Harmony/field-access exceptions. Check logs after every scenario.
2. At a fixed Blood Magic level, summon melee skeletons and archers at each Dead Raiser
   rank. Confirm the default table in README; ordinary skeleton enemies remain unchanged.
   Swap weapons afterward and verify existing summons retain their original bonuses.
3. At max rank, fight representative Mistlands enemies (seeker, soldier, gjall) individually,
   then groups/starred enemies. Repeat at low/high Blood Magic. Aim for strong support,
   with real danger from groups and heavy attacks. Record time-to-kill, health loss and
   summon deaths before adjusting defaults; no claim of boss balance is made yet.
4. Wound a summon, hit again before 10 seconds, and verify healing is delayed. After 10
   damage-free seconds verify 0.5%/second healing at max rank. Kill it and verify no revival.
5. Save/reload full and wounded summons. Verify maximum/current health, rank and damage
   do not compound or reset. Repeat on a peer ownership transfer.
6. Gather several following summons within 30m and leave another waiting/outside range.
   Use a long-distance portal. Only gathered followers should arrive, with identical ZDO
   IDs, equipment and health (apart from normal regeneration/damage), without a cast cost.
7. Repeat with an obstructed exit, failed/restricted portal, elevated platform, cramped
   exit, repeated back-and-forth trips, summon death during loading and player death.
   Verify no duplication, free resurrection, falling damage from stale altitude, or lost pets.
8. Summon beyond the ordinary cap; normal dismissal must still work. Walk away normally
   and log out outside a journey; normal distance/logout dismissal must still apply.
9. Test Trollstav scaling while preserving its hostility; it must never become a portal
   follower. Confirm damage from independent area effects is scaled only once.
10. Repeat travel with TargetPortal, then with two clients plus a dedicated server all
    running this version/config. Keep another player in the source area to exercise source
    ownership and apparent-logout handling. Verify other players' summons stay behind.

Use disposable saves for automated smoke tests.
