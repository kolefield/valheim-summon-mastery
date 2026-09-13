using System;
using System.Collections;
using System.Collections.Generic;

namespace SummonMastery;

public static class SpawnSelection
{
    public const string SpiritCaller = "StaffSpiritCaller";
    public const string SpiritWolf = "Wolf_spiritcaller";

    // Select from the cast's actual pool, never a guessed global prefab or an ordinary wolf.
    // Null means preserve the original pool, including compatibility fallback if wolves are absent.
    public static T[] WolvesOnly<T>(bool enabled, string weapon, T[] pool, Func<T, string> name)
    {
        if (!enabled || weapon != SpiritCaller || pool == null) return null;
        var wolves = new List<T>();
        foreach (var candidate in pool)
            if (name(candidate) == SpiritWolf) wolves.Add(candidate);
        return wolves.Count == 0 ? null : wolves.ToArray();
    }

    // Scope the replacement to one coroutine step; restore after yields, completion and exceptions.
    // Shared prefab arrays are never edited, and unrelated casts cannot inherit a filtered pool.
    public static bool Advance<T>(IEnumerator coroutine, T[] selected, Func<T[]> getPool, Action<T[]> setPool)
    {
        if (selected == null) return coroutine.MoveNext();
        var original = getPool();
        setPool(selected);
        try { return coroutine.MoveNext(); }
        finally { setPool(original); }
    }
}
