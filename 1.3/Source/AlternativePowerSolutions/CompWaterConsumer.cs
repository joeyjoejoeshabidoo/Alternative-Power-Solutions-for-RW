using DubsBadHygiene;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace AlternativePowerSolutions
{
    public class CompProperties_WaterConsumer : CompProperties
    {
        public float waterPerTick;
        public CompProperties_WaterConsumer()
        {
            this.compClass = typeof(CompWaterConsumer);
        }
    }
    public class CompWaterConsumer : ThingComp
    {
        private CompPipe compPipe;

        private CompPowerTrader compPower;
        public CompProperties_WaterConsumer Props => base.props as CompProperties_WaterConsumer;
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            compPipe = this.parent.GetComp<CompPipe>();
            compPower = this.parent.GetComp<CompPowerTrader>();
        }
        public override void CompTick()
        {
            base.CompTick();
            if (compPower.PowerOutput > 0)
            {
                compPipe.pipeNet.PullWater(Props.waterPerTick, out _);
            }
        }

        public override string CompInspectStringExtra()
        {
            StringBuilder sb = new StringBuilder(base.CompInspectStringExtra());
            sb.AppendLine("APS.WaterConsumptionPerSecond".Translate(Props.waterPerTick * 60));
            return sb.ToString().TrimEndNewlines();
        }

        public bool HasEnoughWater()
        {
            return compPipe.pipeNet.WaterStorage >= Props.waterPerTick;
        }
    }
}
