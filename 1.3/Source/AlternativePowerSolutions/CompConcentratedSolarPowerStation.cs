using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace AlternativePowerSolutions
{
	[StaticConstructorOnStartup]
	public class CompConcentratedSolarPowerStation : CompPowerPlant
	{

		private static readonly Material PowerPlantSolarBarFilledMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.5f, 0.475f, 0.1f));

		private static readonly Material PowerPlantSolarBarUnfilledMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.15f, 0.15f, 0.15f));

		public SimpleCurve SolarGeneration = new SimpleCurve
		{
			new CurvePoint(1f, 1f),
			new CurvePoint(0.9f, 0.99f),
			new CurvePoint(0.8f, 0.96f),
			new CurvePoint(0.7f, 0.93f),
			new CurvePoint(0.6f, 0.90f),
			new CurvePoint(0.5f, 0.86f),
			new CurvePoint(0.4f, 0.83f),
			new CurvePoint(0.3f, 0.80f),
			new CurvePoint(0.2f, 0.77f),
			new CurvePoint(0.1f, 0.74f),
			new CurvePoint(0f, 0.71f),
		};

		private float curOutput;
		protected override float DesiredPowerOutput
        {
            get
            {
				if (parent.Map.skyManager.CurSkyGlow > 0)
                {
					var tempOutput = Mathf.Lerp(0, 25000f, SolarGeneration.Evaluate(parent.Map.skyManager.CurSkyGlow)) * RoofedPowerOutputFactor;
					if (tempOutput > curOutput)
                    {
						curOutput += 5;
                    }
					else
                    {
						curOutput = tempOutput;
                    }
                }
				else
                {
					if (curOutput > 0)
                    {
						curOutput--;
                    }
                }
				return curOutput;
			}
		}

		private float RoofedPowerOutputFactor
		{
			get
			{
				int num = 0;
				int num2 = 0;
				foreach (IntVec3 item in parent.OccupiedRect())
				{
					num++;
					if (parent.Map.roofGrid.Roofed(item))
					{
						num2++;
					}
				}
				return (float)(num - num2) / (float)num;
			}
		}

		public static readonly Vector2 BarSize = new Vector2(3, 0.14f);
		public override void PostDraw()
		{
			base.PostDraw();
			GenDraw.FillableBarRequest r = default(GenDraw.FillableBarRequest);
			r.center = parent.DrawPos + Vector3.up * 0.1f;
			r.size = BarSize;
			r.fillPercent = base.PowerOutput / 25000f;
			r.filledMat = PowerPlantSolarBarFilledMat;
			r.unfilledMat = PowerPlantSolarBarUnfilledMat;
			r.margin = 0.15f;
			Rot4 rotation = parent.Rotation;
			rotation.Rotate(RotationDirection.Clockwise);
			r.rotation = rotation;
			GenDraw.DrawFillableBar(r);
		}

        public override void PostExposeData()
        {
            base.PostExposeData();
			Scribe_Values.Look(ref curOutput, "curOutput");
        }
    }
}