using HarmonyLib;
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
    [DefOf]
    public static class APS_DefOf
    {
        public static ThingDef APS_EnhancedGeothermalSystem;
    }
    [StaticConstructorOnStartup]
    public static class HarmonyPatches
    {
        static HarmonyPatches()
        {
            var harmony = new Harmony("AlternativePowerSolutions.Mod");
            harmony.PatchAll();
            LongEventHandler.ExecuteWhenFinished(delegate ()
            {
                var methodToCall = AccessTools.Method(typeof(HarmonyPatches), "DesiredPowerOutputPostfix");
                foreach (var type in typeof(CompPowerPlant).AllSubclassesNonAbstract().AddItem(typeof(CompPowerPlant)))
                {
                    try
                    {
                        var methodToPatch = AccessTools.Method(type, "get_DesiredPowerOutput");
                        harmony.Patch(methodToPatch, postfix: new HarmonyMethod(methodToCall));
                    }
                    catch
                    {

                    }
                }
                var nuclearPlantMethod = AccessTools.Method("CompPowerPlantNuclear:get_DesiredPowerOutputAndRadius");
                if (nuclearPlantMethod != null)
                {
                    harmony.Patch(nuclearPlantMethod, postfix: new HarmonyMethod(methodToCall));
                }
            });
        }

        private static Dictionary<CompPowerTrader, CompWaterConsumer> cachedComps = new Dictionary<CompPowerTrader, CompWaterConsumer>();
        public static void DesiredPowerOutputPostfix(CompPowerTrader __instance, ref float __result)
        {
            if (!cachedComps.TryGetValue(__instance, out var comp))
            {
                cachedComps[__instance] = comp = __instance.parent.TryGetComp<CompWaterConsumer>();
            }
            if (comp != null && !comp.HasEnoughWater())
            {
                __result = 0;
            }
        }
    }

    [HarmonyPatch(typeof(CellFinder), "FindNoWipeSpawnLocNear")]
    public class FindNoWipeSpawnLocNear_Patch
    {
        private static void Postfix(ref IntVec3 __result, IntVec3 near, Map map, ThingDef thingToSpawn, Rot4 rot, ref int maxDist, Predicate<IntVec3> extraValidator = null)
        {
            if (__result == near && __result.GetEdifice(map)?.def == APS_DefOf.APS_EnhancedGeothermalSystem)
            {
                __result = FindNoWipeSpawnLocNearFixed(near, map, thingToSpawn, rot, maxDist, extraValidator);
            }
        }

        private static List<Thing> tmpUniqueWipedThings = new List<Thing>();
        public static IntVec3 FindNoWipeSpawnLocNearFixed(IntVec3 near, Map map, ThingDef thingToSpawn, Rot4 rot, int maxDist = 2, Predicate<IntVec3> extraValidator = null)
        {
            int num = GenRadial.NumCellsInRadius(30);
            IntVec3 result = IntVec3.Invalid;
            float num2 = 0f;
            for (int i = 0; i < num; i++)
            {
                IntVec3 intVec = near + GenRadial.RadialPattern[i];
                if (!intVec.InBounds(map))
                {
                    continue;
                }
                CellRect cellRect = GenAdj.OccupiedRect(intVec, rot, new IntVec2(30, 30));
                if (!cellRect.InBounds(map) || (extraValidator != null && !extraValidator(intVec)) || (thingToSpawn.category == ThingCategory.Building && !GenConstruct.CanBuildOnTerrain(thingToSpawn, intVec, map, rot)))
                {
                    continue;
                }
                bool flag = false;
                bool flag2 = false;
                tmpUniqueWipedThings.Clear();
                foreach (IntVec3 item in cellRect)
                {
                    if (item.Impassable(map))
                    {
                        flag2 = true;
                    }
                    List<Thing> thingList = item.GetThingList(map);
                    for (int j = 0; j < thingList.Count; j++)
                    {
                        if (thingList[j] is Pawn)
                        {
                            flag = true;
                        }
                        else if (GenSpawn.SpawningWipes(thingToSpawn, thingList[j].def) && !tmpUniqueWipedThings.Contains(thingList[j]))
                        {
                            tmpUniqueWipedThings.Add(thingList[j]);
                        }
                    }
                }
                if (flag && thingToSpawn.passability == Traversability.Impassable)
                {
                    tmpUniqueWipedThings.Clear();
                    continue;
                }
                if (flag2 && thingToSpawn.category == ThingCategory.Item)
                {
                    tmpUniqueWipedThings.Clear();
                    continue;
                }
                float num3 = 0f;
                for (int k = 0; k < tmpUniqueWipedThings.Count; k++)
                {
                    if (tmpUniqueWipedThings[k].def.category == ThingCategory.Building && !tmpUniqueWipedThings[k].def.CostList.NullOrEmpty() && tmpUniqueWipedThings[k].def.CostStuffCount == 0)
                    {
                        List<ThingDefCountClass> list = tmpUniqueWipedThings[k].CostListAdjusted();
                        for (int l = 0; l < list.Count; l++)
                        {
                            num3 += list[l].thingDef.GetStatValueAbstract(StatDefOf.MarketValue) * (float)list[l].count * (float)tmpUniqueWipedThings[k].stackCount;
                        }
                    }
                    else
                    {
                        num3 += tmpUniqueWipedThings[k].MarketValue * (float)tmpUniqueWipedThings[k].stackCount;
                    }
                    if (tmpUniqueWipedThings[k].def.category == ThingCategory.Building || tmpUniqueWipedThings[k].def.category == ThingCategory.Item)
                    {
                        num3 = Mathf.Max(num3, 0.001f);
                    }
                }
                tmpUniqueWipedThings.Clear();
                if (!result.IsValid || num3 < num2)
                {
                    if (num3 == 0f)
                    {
                        return intVec;
                    }
                    result = intVec;
                    num2 = num3;
                }
            }
            if (!result.IsValid)
            {
                return near;
            }
            return result;
        }
    }

    [HarmonyPatch(typeof(CompCreatesInfestations), "CanCreateInfestationNow", MethodType.Getter)]
    public class CanCreateInfestationNow_Patch
    {
        private static Dictionary<CompCreatesInfestations, CompBreakdownable> cachedComps = new Dictionary<CompCreatesInfestations, CompBreakdownable>();
        private static bool Prefix(CompCreatesInfestations __instance, ref bool __result)
        {
            if (__instance.parent.def == APS_DefOf.APS_EnhancedGeothermalSystem)
            {
                if (!cachedComps.TryGetValue(__instance, out var comp))
                {
                    cachedComps[__instance] = comp = __instance.parent.TryGetComp<CompBreakdownable>();
                }
                if (comp != null && comp.BrokenDown)
                {
                    __result = false;
                    return false;
                }
                if (__instance.CantFireBecauseCreatedInfestationRecently)
                {
                    __result = false;
                    return false;
                }
                if (__instance.CantFireBecauseSomethingElseCreatedInfestationRecently)
                {
                    __result = false;
                    return false;
                }
                __result = true;
                return false;
            }
            return true;
        }
    }
}
