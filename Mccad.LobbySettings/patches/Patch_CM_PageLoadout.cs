using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using Steamworks;
using CellMenu;

namespace Mccad.LobbySettings
{
    class Patch_CM_PageMap
    {
        public static void Inject(Harmony harmony)
        {
            var gameType = typeof(CM_PageMap);
            var patchType = typeof(Patch_CM_PageMap);

            harmony.Patch(gameType.GetMethod("SetPageActive"), null, new HarmonyMethod(patchType.GetMethod("SetPageActive")));
        }

        public static void SetPageActive(bool active)
        {
            if (active) LobbySettingsManager.Current.GenerateMapButtons();
        }
    }
}