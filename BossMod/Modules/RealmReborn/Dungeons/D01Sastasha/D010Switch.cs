﻿using System.Linq;

namespace BossMod.RealmReborn.Dungeons.D01Sastasha.D010Switch
{
    public enum OID : uint
    {
        Blue = 0x1E8554,
        Red = 0x1E8A8C,
        Green = 0x1E8A8D,
    }

    class SwitchHint : BossComponent
    {
        private string _hint = "";

        public override void AddGlobalHints(BossModule module, GlobalHints hints)
        {
            if (_hint.Length == 0)
            {
                var a = module.WorldState.Actors.FirstOrDefault(a => a.IsTargetable && (OID)a.OID is OID.Blue or OID.Red or OID.Green);
                if (a != null)
                    _hint = ((OID)a.OID).ToString();
            }
            hints.Add($"Correct switch: {_hint}");
        }
    }

    class D010SwitchStates : StateMachineBuilder
    {
        public D010SwitchStates(BossModule module) : base(module)
        {
            TrivialPhase();
        }
    }

    [ModuleInfo(PrimaryActorOID = (uint)OID.Blue)]
    public class D010Switch : BossModule
    {
        public D010Switch(WorldState ws, Actor primary) : base(ws, primary, new ArenaBoundsSquare(primary.Position, 20))
        {
            ActivateComponent<SwitchHint>();
        }
    }
}
