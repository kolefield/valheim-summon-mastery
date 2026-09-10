using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace SummonMastery;

internal static class PortalTravel
{
    private sealed class Journey
    {
        internal Player Player;
        internal Vector3 Origin;
        internal float Deadline;
        internal readonly List<ZDOID> Summons = new();
    }

    private static Journey journey;
    private static readonly AccessTools.FieldRef<Character, float> MaxAirAltitude =
        AccessTools.FieldRefAccess<Character, float>("m_maxAirAltitude");

    internal static long Now => ZNet.instance ? ZNet.instance.GetTime().Ticks : 0;

    internal static void Begin(Player player)
    {
        Cancel();
        var pending = new Journey { Player = player, Origin = player.transform.position,
            Deadline = Time.unscaledTime + 45f };
        foreach (var character in Character.GetAllCharacters())
        {
            if (!character || character.IsPlayer()) continue;
            var data = Plugin.Data(character);
            var ai = character.GetComponent<MonsterAI>();
            if (!Scaling.CanTravel(Plugin.Tagged(data), character.IsTamed(),
                ai && ai.GetFollowTarget() == player.gameObject,
                data?.GetLong(Plugin.Key + "summoner") ?? 0, player.GetPlayerID(),
                Vector3.Distance(character.transform.position, player.transform.position),
                Plugin.PortalRange.Value, character.IsDead())) continue;
            // Valheim permits ownership claims (also used by normal tame interactions).
            // Only claim our own explicitly following summons, never another player's creatures.
            character.GetComponent<ZNetView>().ClaimOwnership();
            data.Set(Plugin.Key + "travelUntil", Now + TimeSpan.FromSeconds(45).Ticks);
            ZDOMan.instance.ForceSendZDO(data.m_uid);
            pending.Summons.Add(data.m_uid);
        }
        if (pending.Summons.Count > 0) journey = pending;
    }

    internal static void Finish(Player player)
    {
        if (journey == null || journey.Player != player) return;
        var pending = journey;
        journey = null;
        bool success = !player.IsDead() && Vector3.Distance(player.transform.position, pending.Origin) > 2f;
        foreach (var id in pending.Summons)
        {
            // Resolve the EXISTING network record: if the creature died, do not resurrect it.
            var data = ZDOMan.instance?.GetZDO(id);
            if (data == null || !data.IsValid() || !Plugin.Tagged(data) ||
                data.GetLong(Plugin.Key + "summoner") != player.GetPlayerID()) continue;
            if (data.GetFloat(ZDOVars.s_health, 1f) <= 0) continue;
            var instance = ZNetScene.instance.FindInstance(id);
            var character = instance ? instance.GetComponent<Character>() : null;
            var ai = character ? character.GetComponent<MonsterAI>() : null;
            // Recheck commands in case somebody told a loaded pet to wait during loading.
            bool stillFollowing = character ? character.IsTamed() && ai &&
                ai.GetFollowTarget() == player.gameObject :
                data.GetBool(ZDOVars.s_tamed, true) && data.GetString(ZDOVars.s_follow) == player.GetPlayerName();
            data.SetOwner(ZDOMan.GetSessionID());
            data.Set(Plugin.Key + "travelUntil", 0L);
            if (success && stillFollowing && (!character || !character.IsDead()))
            {
                // Use the player's validated exit point. Do not guess terrain offsets on elevated bases
                // or put a large summon behind the portal. Normal creature collision separates the group.
                Vector3 destination = player.transform.position + Vector3.up * 0.2f;
                data.SetPosition(destination);
                data.SetRotation(player.transform.rotation);
                if (character)
                {
                    character.transform.SetPositionAndRotation(destination, player.transform.rotation);
                    var body = character.GetComponent<Rigidbody>();
                    if (body)
                    {
                        body.position = destination;
                        body.rotation = player.transform.rotation;
                        body.linearVelocity = Vector3.zero;
                        body.angularVelocity = Vector3.zero;
                    }
                    MaxAirAltitude(character) = destination.y;
                    character.GetComponent<ZSyncTransform>()?.SyncNow();
                }
                ZDOMan.instance.ForceSendZDO(id);
            }
        }
    }

    internal static void CheckTimeout()
    {
        if (journey != null && (!journey.Player || journey.Player.IsDead() ||
            Time.unscaledTime > journey.Deadline)) Cancel();
    }

    internal static void Cancel()
    {
        if (journey == null) return;
        if (ZDOMan.instance != null)
            foreach (var id in journey.Summons)
            {
                var data = ZDOMan.instance.GetZDO(id);
                if (data != null && data.IsOwner()) data.Set(Plugin.Key + "travelUntil", 0L);
            }
        journey = null;
    }
}

[HarmonyPatch(typeof(Player), nameof(Player.TeleportTo))]
internal static class StartTravel
{
    private static void Postfix(Player __instance, bool distantTeleport, bool __result)
    {
        if (__result && distantTeleport && Plugin.Portals.Value && __instance == Player.m_localPlayer)
            PortalTravel.Begin(__instance);
    }
}

[HarmonyPatch(typeof(Player), "UpdateTeleport")]
internal static class CompleteTravel
{
    private static void Prefix(Player __instance, out bool __state) => __state = __instance.IsTeleporting();
    private static void Postfix(Player __instance, bool __state)
    {
        if (__state && !__instance.IsTeleporting()) PortalTravel.Finish(__instance);
    }
}

[HarmonyPatch(typeof(Tameable), "UpdateSummon")]
internal static class ProtectTravel
{
    internal static bool Prefix(Tameable __instance)
    {
        var data = Plugin.Data(__instance.GetComponent<Character>());
        if (!Plugin.Tagged(data)) return true;
        long remaining = data.GetLong(Plugin.Key + "travelUntil") - PortalTravel.Now;
        // Suppress distance dismissal ONLY during a bounded journey, never damage, death, or summon caps.
        return remaining <= 0 || remaining > TimeSpan.FromSeconds(46).Ticks;
    }
}

[HarmonyPatch(typeof(Tameable), "UpdateSavedFollowTarget")]
internal static class ProtectFollowDuringTravel
{
    // A source-area peer may unload the departing player and otherwise mistake portal loading
    // for a logout. Use the same bounded ticket for this second vanilla dismissal path.
    private static bool Prefix(Tameable __instance) => ProtectTravel.Prefix(__instance);
}
