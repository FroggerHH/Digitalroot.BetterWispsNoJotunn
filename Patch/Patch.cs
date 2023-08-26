using System;
using System.Linq;
using Extensions;
using HarmonyLib;
using UnityEngine;
using static BetterWispsNoJotunn.Plugin;
using System.Reflection;
using JetBrains.Annotations;

namespace BetterWispsNoJotunn
{
    [UsedImplicitly]
    public class Patch
    {
        [UsedImplicitly, HarmonyPatch(typeof(Demister))]
        public static class PatchDemisterOnEnable
        {
            [HarmonyPostfix, HarmonyPriority(Priority.Normal),
             HarmonyPatch(typeof(Demister), nameof(Demister.OnEnable))]
            public static void Postfix([NotNull] ref Demister __instance)
            {
                try
                {
                    if (!Game.instance) return;
                    if (!ObjectDB.instance) return;
                    if (!ZNetScene.instance) return;
                    if (!Player.m_localPlayer) return;

                    var itemData = Player.m_localPlayer.GetInventory().GetEquippedItems()
                        .FirstOrDefault(i => i.m_dropPrefab.name == "Demister");

                    if (!__instance.isActiveAndEnabled || itemData == null) return;
                    __instance.m_forceField.endRange = baseRange + (increasedRangePerLevel * (itemData.m_quality - 1));

                    Debug($"Updated {itemData.m_dropPrefab.name} range to {__instance.m_forceField.endRange}.");
                }
                catch (Exception ex)
                {
                    DebugError(ex, false);
                }
            }
        }
    }
}