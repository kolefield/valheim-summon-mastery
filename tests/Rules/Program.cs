using System;
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
        var maximum = new Scaling(3, .005f, 35, 1.3f, 2.5f);
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
}
