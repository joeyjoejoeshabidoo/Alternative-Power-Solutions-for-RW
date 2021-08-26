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
    public class PlaceWorker_OffshoreWindTurbine : PlaceWorker
    {
        public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
        {
            foreach (IntVec3 c in GenAdj.CellsOccupiedBy(loc, rot, checkingDef.Size))
            {
                if (map.terrainGrid.TerrainAt(c) != DefDatabase<TerrainDef>.GetNamed("WaterOceanDeep"))
                {
                    return new AcceptanceReport("APS.MustBeBuiltOnDeepOceanWater".Translate());
                }
            }
            return true;
        }

        public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing thing = null)
        {
            GenDraw.DrawFieldEdges(CalculateWindCells(center, rot, def.size).ToList());
        }

		[TweakValue("0ASP")] public static int test1 = 9;
		[TweakValue("0ASP")] public static int test2 = 5;
		[TweakValue("0ASP")] public static int test3 = 5;
		[TweakValue("0ASP")] public static int test4 = 2;
		[TweakValue("0ASP")] public static int test5 = 1;
		[TweakValue("0ASP", -10, 10)] public static int test6 = -1;
		public static IEnumerable<IntVec3> CalculateWindCells(IntVec3 center, Rot4 rot, IntVec2 size)
		{
			CellRect rectA = default(CellRect);
			CellRect rectB = default(CellRect);
			int num = 0;
			int num2;
			int num3;
			if (rot == Rot4.North || rot == Rot4.East)
			{
				num2 = test1;
				num3 = test2;
			}
			else
			{
				num2 = test2;
				num3 = test1;
				num = -1;
			}
			if (rot.IsHorizontal)
			{
				rectA.minX = center.x + test4 + num;
				rectA.maxX = center.x + test4 + num2 + num;
				rectB.minX = center.x - test5 - num3 + num;
				rectB.maxX = center.x - test5 + num;
				rectB.minZ = (rectA.minZ = center.z - test3 - test6);
				rectB.maxZ = (rectA.maxZ = center.z + test3);
			}
			else
			{
				rectA.minZ = center.z + test4 + num;
				rectA.maxZ = center.z + test4 + num2 + num;
				rectB.minZ = center.z - test5 - num3 + num;
				rectB.maxZ = center.z - test5 + num;
				rectB.minX = (rectA.minX = center.x - test3 - test6);
				rectB.maxX = (rectA.maxX = center.x + test3);
			}
			for (int z2 = rectA.minZ; z2 <= rectA.maxZ; z2++)
			{
				for (int x = rectA.minX; x <= rectA.maxX; x++)
				{
					yield return new IntVec3(x, 0, z2);
				}
			}
			for (int z2 = rectB.minZ; z2 <= rectB.maxZ; z2++)
			{
				for (int x = rectB.minX; x <= rectB.maxX; x++)
				{
					yield return new IntVec3(x, 0, z2);
				}
			}
		}
	}
}
