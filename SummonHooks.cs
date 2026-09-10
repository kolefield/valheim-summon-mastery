using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace SummonMastery;

internal sealed class SpawnContext
{
    [ThreadStatic] internal static SpawnContext Current;
    internal readonly List<Character> Created = new();
    private readonly HashSet<string> prefabs = new(StringComparer.Ordinal);
    private readonly long summoner;
    private readonly int rank, maximumRank;
    private readonly string weapon;
    private readonly Scaling scaling;
    private readonly float delay;

    internal SpawnContext(SpawnAbility ability, Player player, ItemDrop.ItemData item)
    {
        summoner = player.GetPlayerID();
        rank = Math.Max(1, item.m_quality);
        maximumRank = Math.Max(1, item.m_shared.m_maxQuality);
        weapon = item.m_dropPrefab ? item.m_dropPrefab.name : item.m_shared.m_name;
        scaling = Scaling.ForRank(rank, maximumRank, new Scaling(Plugin.MaxHealth.Value,
            Plugin.MaxRegen.Value, Plugin.MaxArmor.Value, Plugin.MaxSpeed.Value, Plugin.MaxDamage.Value));
        delay = Plugin.RegenDelay.Value;
        foreach (var prefab in ability.m_spawnPrefab)
            if (prefab && prefab.GetComponent<Character>()) prefabs.Add(prefab.name);
    }

    internal void Capture(Character character)
    {
        if (character && prefabs.Contains(Utils.GetPrefabName(character.gameObject))) Created.Add(character);
    }

    internal void Apply()
    {
        foreach (var character in Created)
        {
            var data = Plugin.Data(character);
            if (data == null || !data.IsOwner() || Plugin.Tagged(data) || character.IsDead()) continue;
            // Run AFTER the original coroutine step has applied skill levels and commanded the pet.
            // This also handles delayed/multi-creature spawns without a global "last cast weapon".
            float fraction = character.GetHealthPercentage();
            float health = character.GetMaxHealth() * scaling.Health;
            data.Set(Plugin.Key + "rank", rank);
            data.Set(Plugin.Key + "maxRank", maximumRank);
            data.Set(Plugin.Key + "summoner", summoner);
            data.Set(Plugin.Key + "weapon", weapon);
            data.Set(Plugin.Key + "health", scaling.Health);
            data.Set(Plugin.Key + "regen", scaling.Regen);
            data.Set(Plugin.Key + "armor", scaling.Armor);
            data.Set(Plugin.Key + "speed", scaling.Speed);
            data.Set(Plugin.Key + "damage", scaling.Damage);
            data.Set(Plugin.Key + "delay", delay);
            character.SetMaxHealth(health);
            character.SetHealth(health * fraction);
            if (!character.GetComponent<SummonState>()) character.gameObject.AddComponent<SummonState>();
        }
        Created.Clear();
    }

    internal IEnumerator Wrap(IEnumerator original)
    {
        try
        {
            while (true)
            {
                var previous = Current;
                bool next;
                Current = this;
                try { next = original.MoveNext(); }
                finally
                {
                    Current = previous;
                    Apply();
                }
                if (!next) yield break;
                yield return original.Current;
            }
        }
        finally { (original as IDisposable)?.Dispose(); }
    }
}

[HarmonyPatch(typeof(SpawnAbility), "Spawn")]
internal static class CaptureCast
{
    private static void Postfix(SpawnAbility __instance, Character ___m_owner,
        ItemDrop.ItemData ___m_weapon, ref IEnumerator __result)
    {
        if (___m_owner is Player player && ___m_weapon != null && __result != null)
            __result = new SpawnContext(__instance, player, ___m_weapon).Wrap(__result);
    }
}

[HarmonyPatch(typeof(Character), "Awake")]
internal static class CaptureCreature
{
    private static void Postfix(Character __instance) => SpawnContext.Current?.Capture(__instance);
}

[HarmonyPatch(typeof(Character), "GetMaxHealthBase")]
internal static class RestoreHealth
{
    private static void Postfix(Character __instance, ref float __result)
    {
        var data = Plugin.Data(__instance);
        // Scale the base calculation BEFORE SetupMaxHealth calls SetMaxHealth. Applying it afterward
        // would briefly lower the maximum and clamp away health on a full-health reload.
        if (Plugin.Tagged(data)) __result *= data.GetFloat(Plugin.Key + "health", 1f);
    }
}

[HarmonyPatch(typeof(Attack), "ModifyDamage")]
internal static class ScaleAttack
{
    private static void Postfix(Character ___m_character, HitData hitData)
    {
        var data = Plugin.Data(___m_character);
        if (Plugin.Tagged(data)) hitData.m_damage.Modify(data.GetFloat(Plugin.Key + "damage", 1f));
    }
}

[HarmonyPatch(typeof(Aoe), "GetDamage", new Type[] { typeof(int) })]
internal static class ScaleAreaDamage
{
    private static void Postfix(Aoe __instance, Character ___m_owner, HitData ___m_hitData,
        ref HitData.DamageTypes __result)
    {
        // Attack-generated areas already inherited the scaled HitData. Independent creature
        // effects (including the summoned troll's spawn area) need their own multiplier.
        if (__instance.m_useAttackSettings && ___m_hitData != null) return;
        var data = Plugin.Data(___m_owner);
        if (Plugin.Tagged(data)) __result.Modify(data.GetFloat(Plugin.Key + "damage", 1f));
    }
}

[HarmonyPatch(typeof(Character), "GetBodyArmor")]
internal static class DisplayArmor
{
    private static void Postfix(Character __instance, ref float __result)
    {
        var data = Plugin.Data(__instance);
        if (Plugin.Tagged(data)) __result += data.GetFloat(Plugin.Key + "armor");
    }
}

[HarmonyPatch(typeof(Character), nameof(Character.ApplyDamage))]
internal static class DelayHealingAfterDamage
{
    private static void Prefix(Character __instance, out float __state) => __state = __instance.GetHealth();
    private static void Postfix(Character __instance, float __state)
    {
        // Observe each damage event even when a heal occurs before the next fixed update.
        if (__instance.GetHealth() < __state)
            __instance.GetComponent<SummonState>()?.NotifyDamage();
    }
}

[HarmonyPatch(typeof(Character), "RPC_Damage")]
internal static class CreatureArmor
{
    internal static void Apply(Character character, HitData hit)
    {
        var data = Plugin.Data(character);
        if (Plugin.Tagged(data)) hit.ApplyArmor(data.GetFloat(Plugin.Key + "armor"));
    }

    // Vanilla only calls GetBodyArmor for players. Insert after resistance and before damage-over-time
    // extraction, using the normal armor formula and leaving every vanilla branch intact.
    internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        int matches = 0;
        foreach (var instruction in instructions)
        {
            yield return instruction;
            if (instruction.operand is MethodInfo method && method.DeclaringType == typeof(HitData)
                && method.Name == nameof(HitData.ApplyResistance))
            {
                matches++;
                yield return new CodeInstruction(OpCodes.Ldarg_0);
                yield return new CodeInstruction(OpCodes.Ldarg_2);
                yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CreatureArmor), nameof(Apply)));
            }
        }
        if (matches != 1) throw new InvalidOperationException($"Expected one damage resistance hook, found {matches}. Game API changed.");
    }
}
