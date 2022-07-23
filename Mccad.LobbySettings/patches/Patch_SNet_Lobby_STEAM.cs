using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using BepInEx.Logging;
using UnityEngine;
using SNetwork;
using Steamworks;

namespace Mccad.LobbySettings
{
    class Patch_SNet_Lobby_STEAM
    {
        public static void Inject(Harmony harmony)
        {
            var gameType = typeof(SNet_Lobby_STEAM);
            var patchType = typeof(Patch_SNet_Lobby_STEAM);

            harmony.Patch(gameType.GetMethod(nameof(SNet_Lobby_STEAM.PlayerJoined), new Type[] { typeof(SNet_Player), typeof(CSteamID) }), new HarmonyMethod(patchType.GetMethod("PlayerJoined")));
        }

        public static bool PlayerJoined(SNet_Player player, CSteamID steamID)
        {
            if (LobbySettingsManager.Current.IsPlayerBanned(player, steamID)) return false;     //If the given steamID is banned, prevent them from joining the lobby
            return LobbySettingsManager.Current.TryApplyPrivacySettings(player, steamID);       //Only join if the effective lobby privacy settings allow it
        }
    }
}