using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using Steamworks;
using CellMenu;

namespace Mccad.LobbySettings
{
    class Patch_CM_PageLoadout
    {
        public static void Inject(Harmony harmony)
        {
            var gameType = typeof(CM_PageLoadout);
            var patchType = typeof(Patch_CM_PageLoadout);

            harmony.Patch(gameType.GetMethod("SetPageActive"), null, new HarmonyMethod(patchType.GetMethod("SetPageActive")));
        }

        public static void SetPageActive(bool active)
        {
            if (active) LobbySettingsManager.Current.GenerateLobbyButtons();
        }
    }
}