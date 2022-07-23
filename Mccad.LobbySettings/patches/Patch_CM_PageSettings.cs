using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using Steamworks;
using CellMenu;

namespace Mccad.LobbySettings
{
    class Patch_CM_PageSettings
    {
        public static void Inject(Harmony harmony)
        {
            var gameType = typeof(CM_PageSettings);
            var patchType = typeof(Patch_CM_PageSettings);

            harmony.Patch(gameType.GetMethod("SetPageActive"), null, new HarmonyMethod(patchType.GetMethod("SetPageActive")));
        }

        public static void SetPageActive(bool active)
        {
            if (active) LobbySettingsManager.Current.GenerateSettingsButtons();
        }
    }
}