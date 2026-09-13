using System;
using System.Collections;
using SummonMastery;

static class Program
{
    static int count;
    static void Check(bool value, string message)
    {
        if (!value) throw new Exception("FAIL: " + message);
        count++;
        System.Console.WriteLine("PASS: " + message);
    }
    static bool Near(float a, float b) => Math.Abs(a - b) < 0.0001f;
    static void Main()
    {
        WolfSelectionTests();
        var maximum = new Scaling(3, .005f, 35, 1.3f, 2.5f);
        var excluded = Scaling.ForWeapon(true, "StaffRedTroll", 4, 4, maximum);
        Check(excluded.Health == 1 && excluded.Regen == 0 && excluded.Armor == 0 && excluded.Speed == 1 && excluded.Damage == 1,
            "Trollstav exclusion neutralizes all five mod scaling values");
        Check(Scaling.ForWeapon(false, "StaffRedTroll", 4, 4, maximum).Health == 3,
            "disabled Trollstav exclusion preserves rank scaling");
        Check(Scaling.ForWeapon(true, "StaffSpiritCaller", 4, 4, maximum).Damage == 2.5f,
            "Trollstav exclusion leaves Spirit Caller scaling unchanged");
        Check(Scaling.ForWeapon(true, "StaffSkeleton", 4, 4, maximum).Armor == 35,
            "Trollstav exclusion leaves Dead Raiser scaling unchanged");
        Check(!Scaling.IsExcluded(true, null) && !Scaling.IsExcluded(true, "StaffRedTrollCustom"),
            "exclusion requires the exact Trollstav weapon identity");
        Check(Scaling.CanDismiss(true, 10, 10, false), "own tracked summon can be dismissed without a range or follow requirement");
        Check(!Scaling.CanDismiss(true, 10, 11, false), "dismissal excludes another player's summons");
        Check(!Scaling.CanDismiss(false, 10, 10, false), "dismissal excludes untracked creatures and pets");
        Check(!Scaling.CanDismiss(true, 0, 0, false), "dismissal rejects missing player identity");
        Check(!Scaling.CanDismiss(true, 10, 10, true), "dismissal leaves dead records alone");
        var first = Scaling.ForRank(1, 4, maximum);
        Check(first.Health == 1 && first.Regen == 0 && first.Armor == 0 && first.Speed == 1 && first.Damage == 1,
            "rank 1 preserves all five original stats");
        var last = Scaling.ForRank(4, 4, maximum);
        Check(last.Health == 3 && last.Regen == .005f && last.Armor == 35 && last.Speed == 1.3f && last.Damage == 2.5f,
            "max rank reaches all five intended defaults");
        var second = Scaling.ForRank(2, 4, maximum);
        Check(Near(second.Health, 1.6666667f) && Near(second.Damage, 1.5f) && Near(second.Armor, 11.6666667f),
            "intermediate rank has proportional bonuses");
        Check(Scaling.ForRank(8, 8, maximum).Health == 3 && Scaling.ForRank(2, 2, maximum).Health == 3,
            "different weapon maximum ranks normalize independently");
        Check(Scaling.ForRank(999, 4, maximum).Health == 3 && Scaling.ForRank(-2, 4, maximum).Health == 1 &&
            Scaling.ForRank(1, 1, maximum).Health == 1, "out-of-range and non-upgradable weapons have bounded scaling");
        float previous = 1;
        for (int i = 1; i <= 10; i++) { var s = Scaling.ForRank(i, 10, maximum); Check(s.Health >= previous, $"rank {i} never weakens health"); previous = s.Health; }
        Check(Scaling.Regeneration(500, 1200, .005f, 9.99f, 10, 1) == 0, "damage delay blocks healing");
        Check(Near(Scaling.Regeneration(500, 1200, .005f, 10, 10, 1), 6), "healing begins at delay boundary");
        Check(Scaling.Regeneration(1199, 1200, .005f, 11, 10, 1) == 1, "regeneration cannot exceed maximum health");
        Check(Scaling.Regeneration(0, 1200, .005f, 11, 10, 1) == 0, "regeneration never resurrects dead summons");
        Check(Scaling.Regeneration(1200, 1200, .005f, 11, 10, 1) == 0, "full health does not generate excess healing");
        Check(Scaling.CanTravel(true, true, true, 10, 10, 30, 30, false), "own living follower at radius boundary is eligible");
        Check(!Scaling.CanTravel(true, true, true, 10, 11, 5, 30, false), "another player's summon is excluded");
        Check(!Scaling.CanTravel(true, false, true, 10, 10, 5, 30, false), "hostile summons are excluded from travel");
        Check(!Scaling.CanTravel(true, true, false, 10, 10, 5, 30, false), "waiting summons stay behind");
        Check(!Scaling.CanTravel(true, true, true, 10, 10, 30.01f, 30, false), "distant summons stay behind");
        Check(!Scaling.CanTravel(false, true, true, 10, 10, 5, 30, false), "ordinary tamed animals stay behind");
        Check(!Scaling.CanTravel(true, true, true, 10, 10, 5, 30, true), "dead summons cannot travel");

        // Invoke the installed game's actual armor math, not a rewritten approximation.
        var hit = new HitData();
        hit.m_damage.m_slash = 100;
        hit.ApplyArmor(35);
        Check(Near(hit.m_damage.m_slash, 65), "100 physical damage still inflicts 65 after maximum added armor");
        hit = new HitData(); hit.m_damage.m_slash = 20; hit.ApplyArmor(35);
        Check(hit.m_damage.m_slash > 0 && hit.m_damage.m_slash < 20, "weak physical hits are reduced but not nullified");
        hit = new HitData(); hit.m_damage.m_fire = 100; hit.ApplyArmor(35);
        Check(Near(hit.m_damage.m_fire, 65), "installed game's armor also reduces fire without granting immunity");
        hit = new HitData(); hit.m_damage.m_damage = 100; hit.ApplyArmor(35);
        Check(hit.m_damage.m_damage == 100, "generic untyped damage bypasses armor");
        System.Console.WriteLine($"{count} checks passed. These are rules and actual armor math, not gameplay or networking tests.");
    }

    static void WolfSelectionTests()
    {
        var pool = new[] { "Bjorn_spiritcaller", "Moose_spiritcaller", "Wolf_spiritcaller", "Boar_spiritcaller" };
        var selected = SpawnSelection.WolvesOnly(true, "StaffSpiritCaller", pool, x => x);
        Check(selected.Length == 1 && selected[0] == "Wolf_spiritcaller", "verified Spirit Caller pool selects only ghost wolves");
        Check(pool.Length == 4 && pool[0] == "Bjorn_spiritcaller", "selection does not mutate shared source arrays");
        Check(SpawnSelection.WolvesOnly(false, "StaffSpiritCaller", pool, x => x) == null, "disabled option preserves random spirit selection");
        Check(SpawnSelection.WolvesOnly(true, "StaffSkeleton", pool, x => x) == null, "Dead Raiser remains unchanged even with a similar pool");
        Check(SpawnSelection.WolvesOnly(true, "StaffRedTroll", pool, x => x) == null, "Trollstav remains unchanged");
        Check(SpawnSelection.WolvesOnly(true, null, pool, x => x) == null, "missing weapon identity cannot affect other summons");
        Check(SpawnSelection.WolvesOnly(true, "StaffSpiritCallerUncooked", pool, x => x) == null, "unfinished Spirit Caller is not confused with the weapon");
        Check(SpawnSelection.WolvesOnly(true, "StaffSpiritCaller", new[] { "Wolf", "Wolf_cub", "Bjorn_spiritcaller" }, x => x) == null,
            "ordinary wolves are never substituted if ghost wolf is missing");
        Check(SpawnSelection.WolvesOnly<string>(true, "StaffSpiritCaller", null, x => x) == null,
            "missing pool falls back to original behavior");
        var nullable = SpawnSelection.WolvesOnly(true, "StaffSpiritCaller", new[] { null, "Wolf_spiritcaller" }, x => x);
        Check(nullable.Length == 1, "missing entries do not prevent selecting a valid ghost wolf");

        var active = pool;
        IEnumerator Cast()
        {
            Check(ReferenceEquals(active, selected), "first coroutine step sees filtered pool");
            yield return null;
            Check(ReferenceEquals(active, selected), "delayed coroutine step still sees filtered pool");
        }
        var cast = Cast();
        Check(SpawnSelection.Advance(cast, selected, () => active, x => active = x), "filtered cast preserves original yield");
        Check(ReferenceEquals(active, pool), "original pool restored while cast waits");
        Check(!SpawnSelection.Advance(cast, selected, () => active, x => active = x), "filtered cast preserves completion");
        Check(ReferenceEquals(active, pool), "original pool restored after completion");
        IEnumerator Failure()
        {
            if (active == selected) throw new InvalidOperationException("fixture");
            yield break;
        }
        bool propagated = false;
        try { SpawnSelection.Advance(Failure(), selected, () => active, x => active = x); }
        catch (InvalidOperationException) { propagated = true; }
        Check(propagated && ReferenceEquals(active, pool), "exceptions propagate and restore original pool");

        IEnumerator Unchanged()
        {
            Check(ReferenceEquals(active, pool), "unfiltered cast sees original pool");
            yield break;
        }
        SpawnSelection.Advance(Unchanged(), null, () => active, x => active = x);
    }
}
