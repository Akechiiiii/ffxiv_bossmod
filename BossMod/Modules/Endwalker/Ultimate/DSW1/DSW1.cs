﻿using System.Collections.Generic;
using System.Linq;

namespace BossMod.Endwalker.Ultimate.DSW1
{
    class EmptyDimension : CommonComponents.SelfTargetedAOE
    {
        public EmptyDimension() : base(ActionID.MakeSpell(AID.EmptyDimension), new AOEShapeDonut(6, 70)) {}
    }

    class FullDimension : CommonComponents.SelfTargetedAOE
    {
        public FullDimension() : base(ActionID.MakeSpell(AID.FullDimension), new AOEShapeCircle(6)) { }
    }

    class HoliestHallowing : CommonComponents.Interruptible
    {
        public HoliestHallowing() : base(ActionID.MakeSpell(AID.HoliestHallowing)) { }
    }

    [PrimaryActorOID((uint)OID.SerAdelphel)]
    public class DSW1 : BossModule
    {
        private Actor? _grinnaux;
        private Actor? _charibert;
        public Actor? SerAdelphel() => PrimaryActor;
        public Actor? SerGrinnaux() => _grinnaux;
        public Actor? SerCharibert() => _charibert;

        public DSW1(BossModuleManager manager, Actor primary)
            : base(manager, primary, true)
        {
            Arena.WorldHalfSize = 22;
            InitStates(new DSW1States(this).Build());
        }

        protected override void UpdateModule()
        {
            // TODO: this is an ugly hack, think how multi-actor fights can be implemented without it...
            // the problem is that on wipe, any actor can be deleted and recreated in the same frame
            if (_grinnaux == null)
                _grinnaux = Enemies(OID.SerGrinnaux).FirstOrDefault();
            if (_charibert == null)
                _charibert = Enemies(OID.SerCharibert).FirstOrDefault();
        }

        protected override void DrawArenaForegroundPost(int pcSlot, Actor pc)
        {
            Arena.Actor(SerAdelphel(), Arena.ColorEnemy);
            Arena.Actor(SerGrinnaux(), Arena.ColorEnemy);
            Arena.Actor(SerCharibert(), Arena.ColorEnemy);
            Arena.Actor(pc, Arena.ColorPC);
        }
    }
}