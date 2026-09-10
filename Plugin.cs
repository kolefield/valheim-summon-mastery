using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace SummonMastery;

[BepInPlugin(Id, "Summon Mastery", Version)]
public sealed class Plugin : BaseUnityPlugin
{
    public const string Id = "local.summonmastery", Version = "0.1.0";
    internal const string Key = "summonmastery.v1.";
    internal static Plugin Instance;
    internal static ConfigEntry<float> MaxHealth, MaxRegen, MaxArmor, MaxSpeed, MaxDamage, RegenDelay, PortalRange;
    internal static ConfigEntry<bool> Portals;
    private Harmony harmony;
    private float nextScan;

    private void Awake()
    {
        Instance = this;
        MaxHealth = Number("Scaling", "Maximum rank health multiplier", 3f, 1f, 20f,
            "Multiplies the summon health after normal skill/star scaling. Rank 1 is unchanged.");
        MaxDamage = Number("Scaling", "Maximum rank damage multiplier", 2.5f, 1f, 10f,
            "Multiplies normal melee and projectile attack damage, retaining Blood Magic bonuses.");
        MaxArmor = Number("Scaling", "Maximum rank added armor", 35f, 0f, 200f,
            "Extra armor using Valheim's normal damage armor formula. Does not grant damage immunity.");
        MaxSpeed = Number("Scaling", "Maximum rank movement multiplier", 1.3f, 1f, 3f,
            "Walk, jog, run, swim and flight movement. Attack animation speed is unchanged.");
        MaxRegen = Number("Scaling", "Maximum rank regeneration fraction", 0.005f, 0f, 0.05f,
            "Fraction of maximum health regenerated per second after the damage delay. 0.005 is 0.5 percent.");
        RegenDelay = Number("Scaling", "Regeneration damage delay seconds", 10f, 1f, 120f,
            "Any health loss restarts this delay. Existing game regeneration is preserved.");
        Portals = Config.Bind("Travel", "Follow through portals", true,
            "Bring nearby, living, tamed summons following their original summoner on successful portal-style teleports.");
        PortalRange = Number("Travel", "Gather radius meters", 30f, 1f, 100f,
            "Only following summons within this distance at departure travel. Waiting pets stay behind.");
        harmony = new Harmony(Id);
        try { harmony.PatchAll(typeof(Plugin).Assembly); }
        catch { harmony.UnpatchSelf(); throw; }
        Logger.LogInfo($"Summon Mastery {Version} loaded. Scaling is recorded when the weapon summons a creature.");
    }

    private ConfigEntry<float> Number(string section, string name, float value, float min, float max, string text) =>
        Config.Bind(section, name, value, new ConfigDescription(text, new AcceptableValueRange<float>(min, max)));

    internal static ZDO Data(Character character)
    {
        if (!character || character.IsPlayer()) return null;
        var view = character.GetComponent<ZNetView>();
        return view && view.IsValid() ? view.GetZDO() : null;
    }

    internal static bool Tagged(ZDO data) => data != null && data.GetInt(Key + "rank") > 0;
    internal static void Warn(string message) => Instance.Logger.LogWarning(message);

    private void Update()
    {
        if (Time.unscaledTime < nextScan) return;
        nextScan = Time.unscaledTime + 1f;
        // Remote ZDO metadata can arrive after Character.Awake. Scan once per second, not per creature/frame.
        foreach (var character in Character.GetAllCharacters())
            if (character && Tagged(Data(character)) && !character.GetComponent<SummonState>())
                character.gameObject.AddComponent<SummonState>();
        PortalTravel.CheckTimeout();
    }

    private void OnDestroy()
    {
        PortalTravel.Cancel();
        harmony?.UnpatchSelf();
        foreach (var character in Character.GetAllCharacters())
            if (character && character.TryGetComponent<SummonState>(out var state)) Destroy(state);
        Instance = null;
    }
}

public sealed class SummonState : MonoBehaviour
{
    private Character character;
    private ZNetView view;
    private float speedFactor = 1f;
    private float lastHealth;
    private float sinceDamage;
    private float timer;

    internal void NotifyDamage() => sinceDamage = 0f;

    private void Awake()
    {
        character = GetComponent<Character>();
        view = GetComponent<ZNetView>();
        lastHealth = character.GetHealth();
        // Reloading or changing network owner never grants an immediate regeneration tick.
        sinceDamage = 0;
        ApplySpeed();
    }

    private void ApplySpeed()
    {
        if (!view || !view.IsValid()) return;
        float factor = view.GetZDO().GetFloat(Plugin.Key + "speed", 1f);
        float ratio = factor / speedFactor;
        character.m_speed *= ratio;
        character.m_walkSpeed *= ratio;
        character.m_runSpeed *= ratio;
        character.m_swimSpeed *= ratio;
        character.m_flySlowSpeed *= ratio;
        character.m_flyFastSpeed *= ratio;
        speedFactor = factor;
    }

    private void FixedUpdate()
    {
        if (!view || !view.IsValid() || !character) return;
        float health = character.GetHealth();
        if (!view.IsOwner()) { sinceDamage = 0; lastHealth = health; return; }
        if (health < lastHealth) sinceDamage = 0;
        else sinceDamage += Time.fixedDeltaTime;
        lastHealth = health;
        timer += Time.fixedDeltaTime;
        if (timer < 1f) return;
        float elapsed = timer;
        timer = 0;
        ApplySpeed();
        var data = view.GetZDO();
        float regen = Scaling.Regeneration(health, character.GetMaxHealth(),
            data.GetFloat(Plugin.Key + "regen"), sinceDamage,
            data.GetFloat(Plugin.Key + "delay", 10f), Math.Min(elapsed, 1.1f));
        if (!character.IsDead() && regen > 0) character.Heal(regen, false);
        lastHealth = character.GetHealth();
    }

    private void OnDestroy()
    {
        if (!character || speedFactor == 0) return;
        character.m_speed /= speedFactor;
        character.m_walkSpeed /= speedFactor;
        character.m_runSpeed /= speedFactor;
        character.m_swimSpeed /= speedFactor;
        character.m_flySlowSpeed /= speedFactor;
        character.m_flyFastSpeed /= speedFactor;
    }
}
