# Building

Requires .NET 8 SDK, installed Valheim 1.0.7, and a BepInEx 5 profile.
Game/Unity/loader DLLs are referenced locally and excluded from the distributable.

```powershell
dotnet build .\SummonMastery.csproj -c Release
dotnet run --project .\tests\Rules -c Release
.\scripts\check-api.ps1
.\scripts\package.ps1
```

Check `dotnet --list-sdks` before building. A .NET runtime alone is insufficient.
PowerShell 7 is required for the packaging script. Regenerate the original geometric
256x256 icon on Windows with `./scripts/create-icon.ps1`.

Default references use the Steam install under `C:\Program Files (x86)\Steam\steamapps\common\Valheim`
and `%APPDATA%\r2modmanPlus-local\Valheim\profiles\Default`.
Override with `-p:GameDir="..." -p:ProfileDir="..."`. Pass the matching `-GameDir` and
`-ProfileDir` values to the API checker.

Output: `bin/Release/netstandard2.1/SummonMastery.dll`.
Package: `dist/SummonMastery-0.1.1.zip`, suitable for Thunderstore or manual installation.
Packaging validates the manifest, icon, versions, allowed contents and DLL hash, using the already-built DLL;
rebuild after source changes.

The game references produce two MSB3277 warnings involving transitive System.Net.Http
and System.IO.Compression versions. This mod does not call either library.

## Implementation notes

- `SpawnContext` wraps each coroutine step, captures matching creature instances in
  `Character.Awake`, then applies bonuses after vanilla skill/command initialization.
  The thread-local context is restored in `finally`, including nested calls and exceptions.
- Maximum health scales in `GetMaxHealthBase`, before vanilla clamps current health.
  Post-scaling `SetupMaxHealth` would incorrectly damage full-health summons on reload.
- `Attack.ModifyDamage` scales melee and projectile damage. `Aoe.GetDamage(int)` handles
  independent creature areas without double-scaling inherited attack data.
- Armor inserts one call after resistance handling in `Character.RPC_Damage` because
  vanilla NPC damage does not use `GetBodyArmor`. A changed/ambiguous anchor fails loading
  and removes this mod's partial patches rather than silently dropping armor support.
- Regeneration and movement use a per-summon component. Health writes occur only on
  the network owner. Damage events plus health-loss polling restart the regeneration delay.
- Once-per-second discovery handles remote metadata arriving after `Awake`.
- Travel claims only the local player's own nearby followers. It uses normal ZDO ownership
  and updates the same ZDO position after completion. It sends no custom RPC and introduces
  no serialized positional protocol. Normal Valheim peer trust still applies.
- Persistent keys use the `summonmastery.v1.` prefix: rank, maxRank, summoner, weapon,
  health, regen, armor, speed, damage, delay, travelUntil. Travel timestamps expire after
  45 seconds. Existing configs/saves should retain this namespace in compatible updates.
- Exactly 12 methods are patched. Dependencies and hook signatures must be rechecked
  after game updates. Do not compile against publicized assemblies as a substitute for
  runtime-accessible members.

## Dismissal (0.1.1)

`Dismissal.cs` polls the configurable BepInEx KeyboardShortcut before the once-per-second
discovery throttle and uses Player.TakeInput for vanilla UI/input rejection. It registers
two routed RPCs for each network session: `local.summonmastery.DismissAll.v1` (no arguments,
client to server) and `local.summonmastery.DismissResult.v1` (integer count, server to client).
The server derives character identity from the connected peer, validates character ownership,
and enumerates tracked summoner ZDO keys across loaded and unloaded sectors. It claims and
destroys only matching living records using vanilla ZDO destruction, without invoking death.
Requests are rate limited to one per second. Existing summon/save/config keys are unchanged.
The response is accepted only from the server. All peers need 0.1.1 for these new RPCs.

`work/` contains local inspection files and test logs and is deliberately ignored/excluded
from compilation and packaging. It includes proprietary decompiled game code used only
for local compatibility inspection; do not distribute it.
