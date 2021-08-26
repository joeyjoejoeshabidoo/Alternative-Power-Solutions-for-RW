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
	public class CompPowerPlantWind : CompPowerPlant
	{
		public int updateWeatherEveryXTicks = 250;

		private int ticksSinceWeatherUpdate;

		private float cachedPowerOutput;

		private List<IntVec3> windPathCells = new List<IntVec3>();

		private List<Thing> windPathBlockedByThings = new List<Thing>();

		private List<IntVec3> windPathBlockedCells = new List<IntVec3>();

		private float spinPosition;

		private Sustainer sustainer;

		[TweakValue("APSGraphics", 0f, 1f)]
		private static float SpinRateFactor = 0.035f;

		[TweakValue("APSGraphics", 0f, 80f)]
		private static float BladeHeight = 11.5f;

		[TweakValue("APSGraphics", 0f, 80f)]
		private static float BladeWidth = 6.6f;
		private static Vector2 BarSize;

		private static readonly Material WindTurbineBarFilledMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.5f, 0.475f, 0.1f));

		private static readonly Material WindTurbineBarUnfilledMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.15f, 0.15f, 0.15f));

		private static readonly Material WindTurbineBladesMat = MaterialPool.MatFrom("Things/Building/Power/OffshoreWindTurbine/OffshoreWindTurbineBlade");
		protected override float DesiredPowerOutput => cachedPowerOutput;
		private float PowerPercent => base.PowerOutput / ((0f - base.Props.basePowerConsumption) * 1.5f);

		public override void PostSpawnSetup(bool respawningAfterLoad)
		{
			base.PostSpawnSetup(respawningAfterLoad);
			BarSize = new Vector2((float)parent.def.size.z - 0.95f, 0.14f);
			RecalculateBlockages();
			spinPosition = Rand.Range(0f, 15f);
		}

		public override void PostDeSpawn(Map map)
		{
			base.PostDeSpawn(map);
			if (sustainer != null && !sustainer.Ended)
			{
				sustainer.End();
			}
		}

		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Values.Look(ref ticksSinceWeatherUpdate, "updateCounter", 0);
			Scribe_Values.Look(ref cachedPowerOutput, "cachedPowerOutput", 0f);
		}

		public override void CompTick()
		{
			base.CompTick();
			if (!base.PowerOn)
			{
				cachedPowerOutput = 0f;
				return;
			}
			ticksSinceWeatherUpdate++;
			if (ticksSinceWeatherUpdate >= updateWeatherEveryXTicks)
			{
				float num = Mathf.Min(parent.Map.windManager.WindSpeed, 1.5f);
				ticksSinceWeatherUpdate = 0;
				cachedPowerOutput = 0f - base.Props.basePowerConsumption * num;
				RecalculateBlockages();
				if (windPathBlockedCells.Count > 0)
				{
					float num2 = 0f;
					for (int i = 0; i < windPathBlockedCells.Count; i++)
					{
						num2 += cachedPowerOutput * 0.2f;
					}
					cachedPowerOutput -= num2;
					if (cachedPowerOutput < 0f)
					{
						cachedPowerOutput = 0f;
					}
				}
			}
			if (cachedPowerOutput > 0.01f)
			{
				spinPosition += PowerPercent * SpinRateFactor;
			}
			if (sustainer == null || sustainer.Ended)
			{
				sustainer = SoundDefOf.WindTurbine_Ambience.TrySpawnSustainer(SoundInfo.InMap(parent));
			}
			sustainer.Maintain();
			sustainer.externalParams["PowerOutput"] = PowerPercent;
		}

		public override void PostDraw()
		{
			base.PostDraw();
			GenDraw.FillableBarRequest fillableBarRequest = default(GenDraw.FillableBarRequest);
			fillableBarRequest.center = parent.DrawPos + Vector3.up * 0.1f;
			fillableBarRequest.size = BarSize;
			fillableBarRequest.fillPercent = PowerPercent;
			fillableBarRequest.filledMat = WindTurbineBarFilledMat;
			fillableBarRequest.unfilledMat = WindTurbineBarUnfilledMat;
			fillableBarRequest.margin = 0.15f;
			GenDraw.FillableBarRequest r = fillableBarRequest;
			Rot4 rotation = parent.Rotation;
			rotation.Rotate(RotationDirection.Clockwise);
			r.rotation = rotation;
			GenDraw.DrawFillableBar(r);


			Vector3 pos = parent.TrueCenter();

			pos += parent.Rotation.FacingCell.ToVector3() * GetVerticalBladeOffset();
			pos += parent.Rotation.RighthandCell.ToVector3() * GetHorizontalBladeOffset();
			pos.y += 3f / 74f;
			float num = BladeHeight * Mathf.Sin(spinPosition);
			if (num < 0f)
			{
				num *= -1f;
			}
			bool num2 = spinPosition % (float)Math.PI * 2f < (float)Math.PI;
			Vector2 vector = new Vector2(num, BladeWidth);
			Vector3 s = new Vector3(vector.x, 1f, vector.y);
			Matrix4x4 matrix = default(Matrix4x4);
			matrix.SetTRS(pos, parent.Rotation.AsQuat, s);
			Graphics.DrawMesh(num2 ? MeshPool.plane10 : MeshPool.plane10Flip, matrix, WindTurbineBladesMat, 0);
			pos.y += YOffset;
			matrix.SetTRS(pos, parent.Rotation.AsQuat, s);
			Graphics.DrawMesh(num2 ? MeshPool.plane10Flip : MeshPool.plane10, matrix, WindTurbineBladesMat, 0);
		}

		[TweakValue("APSGraphics", -10f, 10f)] private static float YOffset = -0.02f;

		[TweakValue("APSGraphics", -10f, 10f)] private static float HorizontalBladeOffsetEast = -0.02f;
		[TweakValue("APSGraphics", -10f, 10f)] private static float HorizontalBladeOffsetWest = 1.7f;
		[TweakValue("APSGraphics", -10f, 10f)] private static float HorizontalBladeOffsetNorth = -0.02f;
		[TweakValue("APSGraphics", -10f, 10f)] private static float HorizontalBladeOffsetSouth = 0.01f;

		[TweakValue("APSGraphics", -10f, 10f)] private static float VerticalBladeOffsetEast = 0.7f;
		[TweakValue("APSGraphics", -10f, 10f)] private static float VerticalBladeOffsetWest = 2.3f;
		[TweakValue("APSGraphics", -10f, 10f)] private static float VerticalBladeOffsetNorth = 4.5f;
		[TweakValue("APSGraphics", -10f, 10f)] private static float VerticalBladeOffsetSouth = -0.25f;
		public float GetVerticalBladeOffset()
        {
			switch (this.parent.Rotation.AsInt)
            {
				case 0: return VerticalBladeOffsetNorth;
				case 1: return VerticalBladeOffsetEast;
				case 2: return VerticalBladeOffsetSouth;
				case 3: return VerticalBladeOffsetWest;
			}
			return -1f;
		}
		public float GetHorizontalBladeOffset()
		{
			switch (this.parent.Rotation.AsInt)
			{
				case 0: return HorizontalBladeOffsetNorth;
				case 1: return HorizontalBladeOffsetEast;
				case 2: return HorizontalBladeOffsetSouth;
				case 3: return HorizontalBladeOffsetWest;
			}
			return -1f;
		}
		public override string CompInspectStringExtra()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append(base.CompInspectStringExtra());
			if (windPathBlockedCells.Count > 0)
			{
				stringBuilder.AppendLine();
				Thing thing = null;
				if (windPathBlockedByThings != null)
				{
					thing = windPathBlockedByThings[0];
				}
				if (thing != null)
				{
					stringBuilder.Append("WindTurbine_WindPathIsBlockedBy".Translate() + " " + thing.Label);
				}
				else
				{
					stringBuilder.Append("WindTurbine_WindPathIsBlockedByRoof".Translate());
				}
			}
			return stringBuilder.ToString();
		}

		private void RecalculateBlockages()
		{
			if (windPathCells.Count == 0)
			{
				IEnumerable<IntVec3> collection = PlaceWorker_OffshoreWindTurbine.CalculateWindCells(parent.Position, parent.Rotation, parent.def.size);
				windPathCells.AddRange(collection);
			}
			windPathBlockedCells.Clear();
			windPathBlockedByThings.Clear();
			for (int i = 0; i < windPathCells.Count; i++)
			{
				IntVec3 intVec = windPathCells[i];
				if (parent.Map.roofGrid.Roofed(intVec))
				{
					windPathBlockedByThings.Add(null);
					windPathBlockedCells.Add(intVec);
					continue;
				}
				List<Thing> list = parent.Map.thingGrid.ThingsListAt(intVec);
				for (int j = 0; j < list.Count; j++)
				{
					Thing thing = list[j];
					if (thing.def.blockWind)
					{
						windPathBlockedByThings.Add(thing);
						windPathBlockedCells.Add(intVec);
						break;
					}
				}
			}
		}
	}
}
