namespace Rubilovo.Logic.Tests;

public static class Check
{
    public static int Failed;

    public static void True(string name, bool condition)
    {
        if (condition) Pass(name);
        else Fail($"{name} [expected TRUE]");
    }

    public static void Eq(string name, long expected, long actual)
    {
        if (expected == actual) Pass(name);
        else Fail($"{name} [expected {expected}, got {actual}]");
    }

    public static void Near(string name, double expected, double actual, double tol = 1e-4)
    {
        if (Math.Abs(expected - actual) <= tol) Pass(name);
        else Fail($"{name} [expected {expected} ±{tol}, got {actual}]");
    }

    public static void Range(string name, double actual, double min, double max)
    {
        if (actual >= min && actual <= max) Pass(name);
        else Fail($"{name} [expected in [{min};{max}], got {actual}]");
    }

    private static void Pass(string name) => Console.WriteLine($"  PASS {name}");

    private static void Fail(string reason)
    {
        Failed++;
        Console.WriteLine($"  FAIL {reason}");
    }
}

public static class Program
{
    public static int Main()
    {
        Console.WriteLine("== XpCurve (спека 01 §7) ==");
        Check.Eq("need(1)=15", 15, XpCurve.Need(1));
        Check.Eq("need(5)=55", 55, XpCurve.Need(5));
        Check.Eq("need(10)=105", 105, XpCurve.Need(10));
        Check.Eq("need(19)=195", 195, XpCurve.Need(19));
        Check.Eq("need(20)=208", 208, XpCurve.Need(20));
        Check.Eq("need(30)=338", 338, XpCurve.Need(30));
        Check.Eq("need(39)=455", 455, XpCurve.Need(39));
        Check.Eq("need(40)=471", 471, XpCurve.Need(40));

        Console.WriteLine("== XpCurve v0.2: путь до 100 уровня (спека 01 §7) ==");
        Check.Eq("need(59)=775", 775, XpCurve.Need(59));
        Check.Eq("need(60)=795", 795, XpCurve.Need(60));
        Check.Eq("need(79)=1175", 1175, XpCurve.Need(79));
        Check.Eq("need(80)=1200", 1200, XpCurve.Need(80));
        Check.Eq("need(99)=1675", 1675, XpCurve.Need(99));
        Check.Eq("need capped at 100", 1700, XpCurve.Need(101));
        Check.Range("total xp to L100 ~69.5k", XpCurve.TotalToReach(100), 69000, 70200);
        bool monotonic = true;
        for (int lv = 1; lv < 100; lv++) monotonic &= XpCurve.Need(lv + 1) > XpCurve.Need(lv);
        Check.True("curve strictly increasing to 100", monotonic);

        Console.WriteLine("== Overflow power после капа роста (спека 01 §7) ==");
        Check.Near("power @25 ups = 1.00", 1.00, Kinematics.PowerFromLevels(25), 1e-4);
        Check.Near("power @35 ups = 1.20", 1.20, Kinematics.PowerFromLevels(35), 1e-4);
        Check.Near("power @99 ups = 2.48", 2.48, Kinematics.PowerFromLevels(99), 1e-4);

        Console.WriteLine("== Survival формулы (спека 01 §13) ==");
        Check.Near("surv interval @10min = campaign floor",
            WaveScript.SpawnInterval(int.MaxValue / 2), WaveScript.SurvivalSpawnInterval(10f), 1e-4);
        Check.Near("surv interval @30min = late floor", GameBalance.Surv_SpawnFloorLate,
            WaveScript.SurvivalSpawnInterval(30f), 1e-4);
        Check.Range("surv interval @22min between floors",
            WaveScript.SurvivalSpawnInterval(22f), GameBalance.Surv_SpawnFloorLate, GameBalance.Spawn_Floor);
        Check.Eq("surv batch @14min=4", 4, WaveScript.SurvivalBatch(14f));
        Check.Eq("surv batch @18min=5", 5, WaveScript.SurvivalBatch(18f));
        Check.Eq("surv batch @27min=8", 8, WaveScript.SurvivalBatch(27f));
        Check.True("surv batch capped", WaveScript.SurvivalBatch(90f) <= GameBalance.Surv_BatchMax);
        Check.Eq("surv cap @19=120", 120, WaveScript.SurvivalAliveCap(19f));
        Check.Eq("surv cap @20=160", 160, WaveScript.SurvivalAliveCap(20f));
        var (elo, ehi) = WaveScript.EliteTimer(25f);
        Check.Near("surv elite faster min", GameBalance.Surv_EliteTimerMinSec, elo);
        Check.Near("surv elite faster max", GameBalance.Surv_EliteTimerMaxSec, ehi);
        var (elo15, _) = WaveScript.EliteTimer(15f);
        Check.Near("campaign elite timer unchanged @15", WaveScript.WaveConstants.Elite_TimerMinSec, elo15);

        BossStats sb = WaveScript.SurvivalBossAt(18f);
        Check.Near("surv boss@18 hp grown x1.7", 800 * (1f + 0.05f * 14), sb.Hp, 1e-3);
        Check.Eq("surv boss minute stamped", 18, sb.Minute);
        BossStats sb24 = WaveScript.SurvivalBossAt(24f);
        Check.True("surv bosses cycle slots", sb24.Hp > 0);

        Console.WriteLine("== Kinematics: скорость от размера (спека 01 §2) ==");
        double[] scales = { 1.0, 2.0, 3.0, 3.5, 4.5 };
        double[] expectedV = { 4.00, 2.83, 2.31, 2.48, 2.48 };
        for (int i = 0; i < scales.Length; i++)
            Check.Near($"v(scale={scales[i]})={expectedV[i]}",
                expectedV[i], Kinematics.FinalSpeed(1f, (float)scales[i], 0, 0), 0.01);

        Check.Near("ortho(2.9)", 7.90, Kinematics.OrthoTarget(2.9f), 0.05);
        Check.Near("ortho(4.3)", 9.36, Kinematics.OrthoTarget(4.3f), 0.05);
        Check.True("ortho cap 9.5", Kinematics.OrthoTarget(100f) <= GameBalance.Cam_OrthoMax);
        Check.Range("scale after 26 levelups hits cap",
            Kinematics.ScaleAfterLevelUps(26), 4.49, 4.51);

        Console.WriteLine("== WaveScript: скейлинг и скрипт забега (спека 01 §8-10) ==");
        Check.Near("hp(walker,min0)=18", 18.0, WaveScript.ScaledHp(0f, EnemyKind.Walker), 0.001);
        Check.Near("hp(walker,min1)=19.8", 19.8, WaveScript.ScaledHp(1f, EnemyKind.Walker), 0.001);
        Check.Near("dmg mult min15 x1.5", 12.0,
            WaveScript.ScaledDamage(15f, EnemyKind.Walker), 0.001);
        Check.Near("interval decay floor", 0.15, WaveScript.SpawnInterval(500), 0.0001);
        Check.Eq("batch @0.5min=1", 1, WaveScript.BatchSize(0.5f));
        Check.Eq("batch @5min=2", 2, WaveScript.BatchSize(5f));
        Check.Eq("batch @7.5min=3", 3, WaveScript.BatchSize(7.5f));
        Check.Eq("batch @13min=4", 4, WaveScript.BatchSize(13f));
        Check.Eq("aliveCap @10=80", 80, WaveScript.AliveCap(10f));
        Check.Eq("aliveCap @11=120", 120, WaveScript.AliveCap(11f));

        bool gotBoss = WaveScript.TryBossAtMinute(4f, out BossStats b4);
        Check.True("boss @4min exists", gotBoss);
        Check.Near("boss4 hp=800", 800, b4.Hp);
        bool window = WaveScript.IsBossWindow(4.5f, out _);
        Check.True("boss window pauses spawns", window);

        var rng = new Random(7);
        var kinds = new HashSet<EnemyKind>();
        for (int i = 0; i < 200; i++) kinds.Add(WaveScript.RollKind(13f, rng.NextDouble()));
        Check.True("late game has kamikaze", kinds.Contains(EnemyKind.Kamikaze));
        Check.True("late game has tank", kinds.Contains(EnemyKind.Tank));

        Console.WriteLine("== Economy: МЯСО и дерево (спека 02 §1-3) ==");
        Check.Eq("meat K1100 E3 B1 = 139", 139, Economy.MeatForRun(1100, 3, 1));
        Check.Eq("meat K2800 E8 B2 = 324", 324, Economy.MeatForRun(2800, 8, 2));
        Check.Eq("anti-farm run#4 halves", 69, Economy.MeatForRunFinal(1100, 3, 1, 4));
        Check.Eq("anti-farm run#6 x0.1", 13, Economy.MeatForRunFinal(1100, 3, 1, 6));

        Check.Eq("tree step 1 = 100", 100, Economy.TreeStepCost(1));
        Check.Eq("tree step 16 = 813", 813, Economy.TreeStepCost(16));
        Check.Range("branch total ~5566", Economy.TreeBranchTotalCost(), 5500, 5630);
        Check.Range("full tree ~33400", Economy.TreeFullTotalCost(), 33000, 33900);

        Console.WriteLine("== WeaponsCatalog (спека 01 §4) ==");
        var bladesL1 = WeaponsCatalog.Stats(WeaponId.Blades, 1);
        Check.Near("blades L1 dmg=10", 10, bladesL1.Damage);
        Check.Eq("blades L1 count=2", 2, bladesL1.Count);
        var bladesL4 = WeaponsCatalog.Stats(WeaponId.Blades, 4);
        Check.Near("blades L4 uptime=10", 10, bladesL4.UptimeSec);
        Check.Near("blades L4 downtime=4", 4, bladesL4.Extra);
        var daggersL5 = WeaponsCatalog.Stats(WeaponId.Daggers, 5);
        Check.Eq("daggers L5 pierce=1", 1, (long)daggersL5.Extra);
        Check.Near("daggers L5 cd=0.85", 0.85, daggersL5.CooldownSec);
        Check.Near("whip backswing L3=50%", 0.5, WeaponsCatalog.BackswingDamagePct(WeaponId.Whip, 3));
        Check.Near("whip backswing L5=100%", 1.0, WeaponsCatalog.BackswingDamagePct(WeaponId.Whip, 5));

        bool foundEvo = Evolutions.TryFind(WeaponId.Blades, out var evo);
        Check.True("blades evolution recipe", foundEvo);
        Check.True("blades needs Armor", evo.RequiredPassive == PassiveId.Armor);

        Console.WriteLine("== UpgradeDeck (спека 01 §7) ==");
        var seedA = new Random(42);
        var seedB = new Random(42);
        var deckA = UpgradeDeck.OfferThree(new LoadoutState(), seedA);
        var deckB = UpgradeDeck.OfferThree(new LoadoutState(), seedB);
        bool deterministic = deckA.Count == deckB.Count;
        for (int i = 0; i < Math.Min(deckA.Count, deckB.Count); i++)
            deterministic &= deckA[i].Type == deckB[i].Type && deckA[i].Weapon == deckB[i].Weapon;
        Check.True("same seed -> same cards", deterministic);

        var mid = new LoadoutState();
        mid.WeaponLevels[(int)WeaponId.Blades] = 2; mid.WeaponCount = 1;
        mid.PassiveLevels[(int)PassiveId.Magnet] = 1; mid.PassiveCount = 1;
        var dist = new Random(1234);
        int nw = 0, uw = 0, ps = 0, n = 6000;
        for (int i = 0; i < n; i++)
        {
            var c = UpgradeDeck.OfferThree(mid, dist)[0];
            if (c.Type == CardType.NewWeapon) nw++;
            else if (c.Type == CardType.UpgradeWeapon) uw++;
            else ps++;
        }
        Check.Range($"new weapon share ~25% ({100.0*nw/n:F1})", 100.0 * nw / n, 18, 32);
        Check.Range($"upgrade weapon share ~45% ({100.0*uw/n:F1})", 100.0 * uw / n, 38, 52);
        Check.Range($"passive share ~30% ({100.0*ps/n:F1})", 100.0 * ps / n, 23, 37);

        var fullWeapons = new LoadoutState();
        for (int w = 0; w < 4; w++) { fullWeapons.WeaponLevels[w] = 5; fullWeapons.WeaponCount = 4; }
        var rngSlots = new Random(99);
        bool allPassiveOrFallback = true;
        for (int i = 0; i < 300; i++)
            foreach (var c in UpgradeDeck.OfferThree(fullWeapons, rngSlots))
                if (c.Type != CardType.Passive) allPassiveOrFallback = false;
        Check.True("weapon slots full -> only passives", allPassiveOrFallback);

        Console.WriteLine(Check.Failed == 0
            ? "\nALL TESTS PASSED"
            : $"\n{Check.Failed} TEST(S) FAILED");
        return Check.Failed == 0 ? 0 : 1;
    }
}
