param(
    [string]$Assembly = (Join-Path $PSScriptRoot '../bin/Release/netstandard2.1/SummonMastery.dll'),
    [string]$GameDir = 'C:\Program Files (x86)\Steam\steamapps\common\Valheim',
    [string]$ProfileDir = "$env:APPDATA\r2modmanPlus-local\Valheim\profiles\Default"
)
$ErrorActionPreference = 'Stop'
Add-Type -Path "$ProfileDir/BepInEx/core/Mono.Cecil.dll"
$resolver = [Mono.Cecil.DefaultAssemblyResolver]::new()
$resolver.AddSearchDirectory("$GameDir/valheim_Data/Managed")
$resolver.AddSearchDirectory("$ProfileDir/BepInEx/core")
$parameters = [Mono.Cecil.ReaderParameters]::new()
$parameters.AssemblyResolver = $resolver
$mod = [Mono.Cecil.AssemblyDefinition]::ReadAssembly((Resolve-Path $Assembly), $parameters)
$game = [Mono.Cecil.AssemblyDefinition]::ReadAssembly("$GameDir/valheim_Data/Managed/assembly_valheim.dll", $parameters)
$findings = 0
$count = 0
foreach ($member in $mod.MainModule.GetMemberReferences()) {
    if ($member.DeclaringType.Scope.Name -notmatch '^(assembly_|Unity)') { continue }
    $count++
    try {
        $resolved = $member.Resolve()
        if ($null -eq $resolved) { throw 'does not resolve' }
        if (($resolved -is [Mono.Cecil.MethodDefinition] -or $resolved -is [Mono.Cecil.FieldDefinition]) -and !$resolved.IsPublic) {
            throw 'not public in the installed assembly'
        }
    } catch { Write-Output "FAIL: $($member.FullName): $_"; $findings++ }
}
Write-Output "Checked $count game/Unity references, including public accessibility."
$targets = @(
    @('SpawnAbility','Spawn','','System.Collections.IEnumerator'),
    @('Character','Awake','','System.Void'),
    @('Character','GetMaxHealthBase','','System.Single'),
    @('Character','GetBodyArmor','','System.Single'),
    @('Character','RPC_Damage','System.Int64,HitData','System.Void'),
    @('Character','ApplyDamage','HitData,System.Boolean,System.Boolean,HitData/DamageModifier','System.Void'),
    @('Attack','ModifyDamage','HitData,System.Single','System.Void'),
    @('Aoe','GetDamage','System.Int32','HitData/DamageTypes'),
    @('Player','TeleportTo','UnityEngine.Vector3,UnityEngine.Quaternion,System.Boolean','System.Boolean'),
    @('Player','UpdateTeleport','System.Single','System.Void'),
    @('Tameable','UpdateSummon','','System.Void'),
    @('Tameable','UpdateSavedFollowTarget','','System.Void')
)
foreach ($target in $targets) {
    $matches = @($game.MainModule.GetType($target[0]).Methods | Where-Object {
        $_.Name -eq $target[1] -and (($_.Parameters | ForEach-Object { $_.ParameterType.FullName }) -join ',') -eq $target[2] -and
        $_.ReturnType.FullName -eq $target[3] -and !$_.IsStatic
    })
    if ($matches.Count -ne 1) { Write-Output "FAIL: target $($target[0]).$($target[1])"; $findings++ }
    else { Write-Output "PASS: target $($target[0]).$($target[1])" }
}
foreach ($field in @(@('SpawnAbility','m_owner','Character'), @('SpawnAbility','m_weapon','ItemDrop/ItemData'),
    @('Attack','m_character','Humanoid'), @('Character','m_maxAirAltitude','System.Single'),
    @('Aoe','m_owner','Character'), @('Aoe','m_hitData','HitData'))) {
    $found = @($game.MainModule.GetType($field[0]).Fields | Where-Object { $_.Name -eq $field[1] -and $_.FieldType.FullName -eq $field[2] })
    if ($found.Count -ne 1) { Write-Output "FAIL: field $($field[0]).$($field[1])"; $findings++ }
    else { Write-Output "PASS: injected/reflected field $($field[0]).$($field[1])" }
}
$damage = $game.MainModule.GetType('Character').Methods | Where-Object Name -eq 'RPC_Damage'
$anchors = @($damage.Body.Instructions | Where-Object { $_.Operand -is [Mono.Cecil.MethodReference] -and $_.Operand.FullName -match 'HitData::ApplyResistance' })
if ($anchors.Count -ne 1 -or $anchors[0].Operand.ReturnType.FullName -ne 'System.Void') {
    Write-Output 'FAIL: armor insertion anchor changed'; $findings++
} else { Write-Output 'PASS: exactly one void resistance call for armor insertion' }
$mod.Dispose()
$game.Dispose()
if ($findings) { throw "$findings API check findings" }
Write-Output 'PASS: static API checks. Runtime Harmony installation is a separate test.'
