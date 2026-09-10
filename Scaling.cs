using System;

namespace SummonMastery;

// Pure rules shared with the regression executable. Rank means item upgrade quality.
public readonly struct Scaling
{
    public readonly float Health, Regen, Armor, Speed, Damage;
    public Scaling(float health, float regen, float armor, float speed, float damage)
    { Health = health; Regen = regen; Armor = armor; Speed = speed; Damage = damage; }

    public static Scaling ForRank(int rank, int maxRank, Scaling maximum)
    {
        // A non-upgradable weapon stays at baseline; rank zero cannot produce negative bonuses.
        float progress = maxRank <= 1 ? 0f : Math.Min(1f, Math.Max(0f, (rank - 1f) / (maxRank - 1f)));
        return new Scaling(1f + (maximum.Health - 1f) * progress,
            maximum.Regen * progress, maximum.Armor * progress,
            1f + (maximum.Speed - 1f) * progress, 1f + (maximum.Damage - 1f) * progress);
    }

    public static bool CanTravel(bool tagged, bool tamed, bool following, long summoner,
        long player, float distance, float range, bool dead) =>
        tagged && tamed && following && summoner != 0 && summoner == player &&
        distance <= range && !dead;

    public static float Regeneration(float health, float maxHealth, float fractionPerSecond,
        float secondsSinceDamage, float delay, float elapsed)
    {
        if (health <= 0 || maxHealth <= 0 || secondsSinceDamage < delay || elapsed <= 0) return 0;
        return Math.Max(0, Math.Min(maxHealth - health, maxHealth * fractionPerSecond * elapsed));
    }
}
