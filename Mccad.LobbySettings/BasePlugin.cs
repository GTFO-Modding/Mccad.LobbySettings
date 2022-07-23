using System;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnhollowerRuntimeLib;
using UnityEngine;

namespace Mccad.LobbySettings
{
    [BepInPlugin("com.Mccad.LobbySettings", "Mccad.LobbySettings", VersionNumber)]
    public class BasePlugin : BepInEx.IL2CPP.BasePlugin
    {
        public override void Load()
        {
            var id = "com.Mccad.LobbySettings";
            var harmony = new Harmony(id);
            Log = base.Log;

            ClassInjector.RegisterTypeInIl2Cpp<LobbySettingsManager>();
            
            Patch_SNet_Lobby_STEAM.Inject(harmony);
            Patch_SteamMatchmaking.Inject(harmony);
            Patch_CM_PageLoadout.Inject(harmony);
            Patch_CM_PageMap.Inject(harmony);
            Patch_CM_PageSettings.Inject(harmony);
        }

        public static new ManualLogSource Log { get; set; }
        public const string VersionNumber = "1.0.0";
    }
}
