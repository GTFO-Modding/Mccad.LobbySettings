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
    class Patch_SteamMatchmaking
    {
        public static void Inject(Harmony harmony)
        {
            var gameType = typeof(SteamMatchmaking);
            var patchType = typeof(Patch_SteamMatchmaking);

            harmony.Patch(gameType.GetMethod("InviteUserToLobby"), new HarmonyMethod(patchType.GetMethod("InviteUserToLobby")));
        }

        public static void InviteUserToLobby(CSteamID steamIDInvitee)
        {
            LobbySettingsManager.Current.WhitelistPlayer(steamIDInvitee);
        }
    }
}