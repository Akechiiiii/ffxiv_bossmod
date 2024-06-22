﻿namespace BossMod.GNB;

public enum AID : uint
{
    None = 0,
    Sprint = ClassShared.AID.Sprint,

    GunmetalSoul = 17105, // LB3, instant, range 0, AOE 50 circle, targets=self, animLock=3.860
    KeenEdge = 16137, // L1, instant, GCD, range 3, single-target, targets=hostile
    NoMercy = 16138, // L2, instant, 60.0s CD (group 10), range 0, single-target, targets=self
    BrutalShell = 16139, // L4, instant, GCD, range 3, single-target, targets=hostile
    Camouflage = 16140, // L6, instant, 90.0s CD (group 15), range 0, single-target, targets=self
    DemonSlice = 16141, // L10, instant, GCD, range 0, AOE 5 circle, targets=self
    RoyalGuard = 16142, // L10, instant, 2.0s CD (group 1), range 0, single-target, targets=self
    ReleaseRoyalGuard = 32068, // L10, instant, 1.0s CD (group 1), range 0, single-target, targets=self
    LightningShot = 16143, // L15, instant, GCD, range 20, single-target, targets=hostile
    DangerZone = 16144, // L18, instant, 30.0s CD (group 4), range 3, single-target, targets=hostile
    SolidBarrel = 16145, // L26, instant, GCD, range 3, single-target, targets=hostile
    BurstStrike = 16162, // L30, instant, GCD, range 3, single-target, targets=hostile
    Nebula = 16148, // L38, instant, 120.0s CD (group 21), range 0, single-target, targets=self
    DemonSlaughter = 16149, // L40, instant, GCD, range 0, AOE 5 circle, targets=self
    Aurora = 16151, // L45, instant, 60.0s CD (group 19/71), range 30, single-target, targets=self/party/alliance/friendly
    Superbolide = 16152, // L50, instant, 360.0s CD (group 24), range 0, single-target, targets=self
    SonicBreak = 16153, // L54, instant, 60.0s CD (group 13/57), range 3, single-target, targets=hostile
    RoughDivide = 16154, // L56, instant, 30.0s CD (group 9/70) (2? charges), range 20, single-target, targets=hostile
    GnashingFang = 16146, // L60, instant, 30.0s CD (group 5/57), range 3, single-target, targets=hostile, animLock=0.700
    SavageClaw = 16147, // L60, instant, GCD, range 3, single-target, targets=hostile, animLock=0.500
    WickedTalon = 16150, // L60, instant, GCD, range 3, single-target, targets=hostile, animLock=0.770
    BowShock = 16159, // L62, instant, 60.0s CD (group 11), range 0, AOE 5 circle, targets=self
    HeartOfLight = 16160, // L64, instant, 90.0s CD (group 16), range 0, AOE 30 circle, targets=self
    HeartOfStone = 16161, // L68, instant, 25.0s CD (group 3), range 30, single-target, targets=self/party
    AbdomenTear = 16157, // L70, instant, 1.0s CD (group 0), range 5, single-target, targets=hostile
    JugularRip = 16156, // L70, instant, 1.0s CD (group 0), range 5, single-target, targets=hostile
    EyeGouge = 16158, // L70, instant, 1.0s CD (group 0), range 5, single-target, targets=hostile
    Continuation = 16155, // L70, instant, 1.0s CD (group 0), range 0, single-target, targets=self, animLock=???
    FatedCircle = 16163, // L72, instant, GCD, range 0, AOE 5 circle, targets=self
    Bloodfest = 16164, // L76, instant, 120.0s CD (group 14), range 25, single-target, targets=hostile
    BlastingZone = 16165, // L80, instant, 30.0s CD (group 4), range 3, single-target, targets=hostile
    HeartOfCorundum = 25758, // L82, instant, 25.0s CD (group 3), range 30, single-target, targets=self/party
    Hypervelocity = 25759, // L86, instant, 1.0s CD (group 0), range 5, single-target, targets=hostile
    DoubleDown = 25760, // L90, instant, 60.0s CD (group 12/57), range 0, AOE 5 circle, targets=self

    // Shared
    ShieldWall = ClassShared.AID.ShieldWall, // LB1, instant, range 0, AOE 50 circle, targets=self, animLock=1.930
    Stronghold = ClassShared.AID.Stronghold, // LB2, instant, range 0, AOE 50 circle, targets=self, animLock=3.860
    Rampart = ClassShared.AID.Rampart, // L8, instant, 90.0s CD (group 46), range 0, single-target, targets=self
    LowBlow = ClassShared.AID.LowBlow, // L12, instant, 25.0s CD (group 41), range 3, single-target, targets=hostile
    Provoke = ClassShared.AID.Provoke, // L15, instant, 30.0s CD (group 42), range 25, single-target, targets=hostile
    Interject = ClassShared.AID.Interject, // L18, instant, 30.0s CD (group 43), range 3, single-target, targets=hostile
    Reprisal = ClassShared.AID.Reprisal, // L22, instant, 60.0s CD (group 44), range 0, AOE 5 circle, targets=self
    ArmsLength = ClassShared.AID.ArmsLength, // L32, instant, 120.0s CD (group 48), range 0, single-target, targets=self
    Shirk = ClassShared.AID.Shirk, // L48, instant, 120.0s CD (group 49), range 25, single-target, targets=party
}

public enum TraitID : uint
{
    None = 0,
    TankMastery = 320, // L1
    CartridgeCharge = 257, // L30
    EnhancedBrutalShell = 258, // L52
    DangerZoneMastery = 259, // L80
    HeartOfStoneMastery = 424, // L82
    EnhancedAurora = 425, // L84
    MeleeMastery = 507, // L84
    EnhancedContinuation = 426, // L86
    CartridgeChargeII = 427, // L88
}

public sealed class Definitions : IDisposable
{
    public Definitions(ActionDefinitions d)
    {
        d.RegisterSpell(AID.GunmetalSoul, instantAnimLock: 3.86f);
        d.RegisterSpell(AID.KeenEdge);
        d.RegisterSpell(AID.NoMercy);
        d.RegisterSpell(AID.BrutalShell);
        d.RegisterSpell(AID.Camouflage);
        d.RegisterSpell(AID.DemonSlice);
        d.RegisterSpell(AID.RoyalGuard);
        d.RegisterSpell(AID.ReleaseRoyalGuard);
        d.RegisterSpell(AID.LightningShot);
        d.RegisterSpell(AID.DangerZone);
        d.RegisterSpell(AID.SolidBarrel);
        d.RegisterSpell(AID.BurstStrike);
        d.RegisterSpell(AID.Nebula);
        d.RegisterSpell(AID.DemonSlaughter);
        d.RegisterSpell(AID.Aurora, maxCharges: 2);
        d.RegisterSpell(AID.Superbolide);
        d.RegisterSpell(AID.SonicBreak);
        d.RegisterSpell(AID.RoughDivide, maxCharges: 2);
        d.RegisterSpell(AID.GnashingFang, instantAnimLock: 0.70f);
        d.RegisterSpell(AID.SavageClaw, instantAnimLock: 0.50f);
        d.RegisterSpell(AID.WickedTalon, instantAnimLock: 0.77f);
        d.RegisterSpell(AID.BowShock);
        d.RegisterSpell(AID.HeartOfLight);
        d.RegisterSpell(AID.HeartOfStone);
        d.RegisterSpell(AID.AbdomenTear);
        d.RegisterSpell(AID.JugularRip);
        d.RegisterSpell(AID.EyeGouge);
        d.RegisterSpell(AID.Continuation); // animLock=???
        d.RegisterSpell(AID.FatedCircle);
        d.RegisterSpell(AID.Bloodfest);
        d.RegisterSpell(AID.BlastingZone);
        d.RegisterSpell(AID.HeartOfCorundum);
        d.RegisterSpell(AID.Hypervelocity);
        d.RegisterSpell(AID.DoubleDown);

        Customize(d);
    }

    public void Dispose() { }

    private void Customize(ActionDefinitions d)
    {
        //d.Spell(AID.Camouflage)!.EffectDuration = 20;
        //d.Spell(AID.Nebula)!.EffectDuration = 15;
        //d.Spell(AID.Aurora)!.EffectDuration = 18;
        //d.Spell(AID.Superbolide)!.EffectDuration = 10;
        //d.Spell(AID.HeartOfLight)!.EffectDuration = 15;
        //d.Spell(AID.HeartOfStone)!.EffectDuration = 4;
        //d.Spell(AID.HeartOfCorundum)!.EffectDuration = 4;
    }
}