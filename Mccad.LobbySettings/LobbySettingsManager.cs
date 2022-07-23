using System;
using System.IO;
using System.Collections.Generic;
using BepInEx;
using System.Text;
using UnityEngine;
using SNetwork;
using Steamworks;
using System.Linq;
using CellMenu;
using Player;

namespace Mccad.LobbySettings
{
    class LobbySettingsManager : ScriptableObject
    {
        public LobbySettingsManager(IntPtr intPTR) : base(intPTR)
        {
            Log("LobbySettings - Written by mccad00");                                              //Debug logging
            Log("Initializing the unity object");                                                   //Debug logging
            Log(intPTR);
        }

        void Awake()
        {
            Privacy = new CS_Value<int>((eCellSettingID)PRIVACY_SETTING, (Action<eCellSettingID, CS_Value<int>>)RegIntVal, (Func<int, int>)ApplyPrivacy, (int)LobbyRestrictions.Public);

            File_Blacklist = Path.Combine(Paths.ConfigPath, "blacklist.txt");                       //Get the filepath of the blacklist
            if (!File.Exists(File_Blacklist))                                                       //If blacklist doesnt exist:
            {
                File.CreateText(File_Blacklist);                                                    //Make a new, empty blacklist
                Log("Generating blacklist.txt");                                                    //Logging
            }

            string[] lines = File.ReadAllLines(File_Blacklist);                                     //Get all the lines in the blacklist
            foreach (string line in lines)                                                           //For each line in the blacklist:
            {
                var steamID_ulong = Convert.ToUInt64(line);                                         //Convert our line to a ulong
                CSteamID steamID = new CSteamID(steamID_ulong);                                     //Create a new CSteamID

                Log($"Blacklisting {steamID_ulong}");                                               //Logging
                Blacklist.Add(steamID);                                                             //Add this steam id to the blacklist
            }
        }



        /// <summary>
        /// Returns true if the given steamID is banned from our lobby
        /// </summary>
        public bool IsPlayerBanned(SNet_Player player, CSteamID steamID)
        {
            Log(Host); Log(steamID);
            if (!Host) return false;
            if (Blacklist.Contains(steamID))                                                //Is the given player in our blacklist?
            {
                Log($"Player {player.GetName()} is banned. kicking", LogSeverity.Warning);  //Logging
                return true;                                                                //Kick the player from the lobby (true)
            }
            return false;                                                                   //Do not kick the player from the lobby (false)
        }




        /// <summary>
        /// Returns true or false depending on the privacy settings of the lobby.
        /// Public: Anyone can join
        /// Private: Invite only
        /// Locked: Nobody can join
        /// </summary>
        /// <returns></returns>
        public bool TryApplyPrivacySettings(SNet_Player player, CSteamID steamID)
        {
            if (!Host) return true;
            string playername = player.GetName();
            switch (LobbyPrivacy)
            {
                case LobbyRestrictions.Public:
                    break;

                case LobbyRestrictions.Private:
                    if (Whitelist.Contains(steamID)) return true;
                    Log($"Player {playername} failed to join. Player is not whitelisted", LogSeverity.Warning);
                    return false;

                case LobbyRestrictions.Locked:
                    Log($"Player {playername} failed to join. Lobby is locked", LogSeverity.Warning);
                    return false;

            }
            if (SNet.Lobby.Players.Count >= LobbyLimit)
            {
                Log($"Player {playername} failed to join. Lobby is full");
                return false;
            }

            return true;
        }



        /// <summary>
        /// Adds a player to the blacklist and removes them from the lobby
        /// </summary>
        public void BanPlayer(int slot)
        {
            if (!Host) return;

            SNet_Player player = SNet.Slots.PlayerSlots[slot].player;                       //Get the player from the slot
            CSteamID steamID = new CSteamID(player.Profile.player.lookup);                  //Get the steamID from the slot

            Blacklist.Add(steamID);                                                         //Add player to the blacklist

            KickPlayer(slot);                                                               //Boot the player from the lobby
            Log($"{player.NickName} has been banned from the lobby", LogSeverity.Warning);  //Logging

            List<string> blacklist_Content = File.ReadAllLines(File_Blacklist).ToList();    //Get the content of our blacklist file
            blacklist_Content.Add(steamID.ToString());                                      //add the steam ID from the list
            File.WriteAllLines(File_Blacklist, blacklist_Content);                          //Rewrite the blacklist file
        }



        /// <summary>
        /// Removes a player from the blacklist
        /// </summary>
        public void UnbanPlayer(SNet_Player player, CSteamID steamID)
        {
            if (Blacklist.Remove(steamID))
            {
                List<string> blacklist_Content = File.ReadAllLines(File_Blacklist).ToList();    //Get the content of our blacklist file
                blacklist_Content.Remove(steamID.ToString());                                   //Remove the steam ID from the list
                File.WriteAllLines(File_Blacklist, blacklist_Content);                          //Rewrite the blacklist file
                return;
            }
            Log($"Failed to remove player {player.GetName()} from the blacklist. SteamID not found", LogSeverity.Error);
        }



        /// <summary>
        /// Adds a player to the whitelist; Allows them to join a private lobby
        /// </summary>
        public void WhitelistPlayer(CSteamID steamID)
        {
            if (Whitelist.Contains(steamID)) return;

            Whitelist.Add(steamID);
            Log("Invite sent. Player added to whitelist");
        }


        /// <summary>
        /// Boot a player from the lobby
        /// </summary>
        public void KickPlayer(int slot)
        {
            if (!Host) return;

            var player = SNet.Slots.PlayerSlots[slot].player;
            SNet.SessionHub.KickPlayer(player, SNet_PlayerEventReason.Kick_ByVote);                 //Kick the player
            PlayerBackpackManager.DestroyBackpack(player);                                          //Empty the backpack
            Log($"{player.GetName()} has been kicked from the lobby", LogSeverity.Normal);          //Logging
        }



        /// <summary>
        /// Generate a log in the bepinEx console
        /// </summary>
        void Log(object log, LogSeverity severity = LogSeverity.Normal)
        {
            switch (severity)
            {
                case LogSeverity.Normal:
                    BasePlugin.Log.LogMessage($"Mccad.LobbySettings: {log}");
                    break;

                case LogSeverity.Warning:
                    BasePlugin.Log.LogWarning($"Mccad.LobbySettings: {log}");
                    break;

                case LogSeverity.Error:
                    BasePlugin.Log.LogError($"Mccad.LobbySettings: {log}");
                    break;
            }
        }

        public void GenerateSettingsButtons()
        {
            if (PageSettings) return;

            PageSettings = GuiManager.MainMenuLayer.PageSettings;
            LobbySettings_Button = PageSettings.m_guiLayer.AddRectComp(PageSettings.m_subMenuButtonPrefab, GuiAnchor.TopLeft, new Vector2 (70f, PageSettings.m_subMenuItemOffset), PageSettings.m_movingContentHolder.transform).Cast<CM_Item>();
            LobbySettings_Button.SetScaleFactor(0.85f);
            LobbySettings_Button.UpdateColliderOffset();
            PageSettings.m_subMenuItemOffset -= 80f;
            LobbySettings_Button.SetText("Lobby Privacy");
            LobbySettings_Button.name = "LobbySettings_Button";

            
            LobbySettings_ScrollWindow = PageSettings.m_guiLayer.AddRectComp(PageSettings.m_scrollwindowPrefab, GuiAnchor.TopLeft, new Vector2(420f, -200f), PageSettings.m_movingContentHolder.transform).Cast<CM_ScrollWindow>();
            LobbySettings_ScrollWindow.Setup();
            LobbySettings_ScrollWindow.SetSize(new Vector2(1020f, 900f));
            LobbySettings_ScrollWindow.SetVisible(false);
            LobbySettings_ScrollWindow.SetHeader($"Lobby Privacy | V{BasePlugin.VersionNumber}");
            LobbySettings_ScrollWindow.name = "LobbySettings_ScrollWindow";
            PageSettings.m_allSettingsWindows.Add(LobbySettings_ScrollWindow);

            LobbySettings_Button.add_OnBtnPressCallback((Action<int>)((_) => OpenSettingsMenu()));

            LobbySettings_LabelText[0] = PageSettings.m_guiLayer.AddRectComp(PageSettings.m_settingsItemPrefab, GuiAnchor.TopLeft, Vector2.zero, LobbySettings_ScrollWindow.m_contentContainer.transform).Cast<CM_SettingsItem>();
            LobbySettings_LabelText[0].Setup();
            LobbySettings_LabelText[0].transform.localPosition = new Vector3(5f, -5f, 0f);
            LobbySettings_LabelText[0].m_basePage = PageSettings;
            LobbySettings_LabelText[0].m_title.text = "Lobby Privacy";
            LobbySettings_LabelText[0].SetText("Lobby Privacy");
            LobbySettings_LabelText[0].name = "LobbySettings_LabelText_PrivacyEnum";

            LobbySettings_LabelText[1] = PageSettings.m_guiLayer.AddRectComp(PageSettings.m_settingsItemPrefab, GuiAnchor.TopLeft, Vector2.zero, LobbySettings_ScrollWindow.m_contentContainer.transform).Cast<CM_SettingsItem>();
            LobbySettings_LabelText[1].Setup();
            LobbySettings_LabelText[1].transform.localPosition = new Vector3(5f, -150f, 0f);
            LobbySettings_LabelText[1].m_basePage = PageSettings;
            LobbySettings_LabelText[1].m_title.text = "Player Limit";
            LobbySettings_LabelText[1].SetText("Player Limit");
            LobbySettings_LabelText[1].name = "LobbySettings_LabelText_LimitInt";


            for (var i = 0; i < 3; i++)
            {
                LobbySettings_ButtonEnum[i] = PageSettings.m_guiLayer.AddRectComp(PageSettings.m_subMenuButtonPrefab, GuiAnchor.TopLeft, Vector2.zero, LobbySettings_ScrollWindow.m_contentContainer.transform).Cast<CM_Item>();
                LobbySettings_ButtonEnum[i].Setup();
                LobbySettings_ButtonEnum[i].SetScaleFactor(1f);
                LobbySettings_ButtonEnum[i].UpdateColliderOffset();
                LobbySettings_ButtonEnum[i].transform.localPosition = new Vector3(25f + (335f * i), -60f, 0f);
                LobbySettings_ButtonEnum[i].SetText($"{(LobbyRestrictions)i}");
                LobbySettings_ButtonEnum[i].name = $"LobbySettings_ButtonEnum_{(LobbyRestrictions)i}";
                var color = new UnhollowerBaseLib.Il2CppStructArray<Color>(1);
                color[0] = new Vector4(1, 1, 0, 1);
                LobbySettings_ButtonEnum[i].m_textColorOver = color;
                LobbySettings_ButtonEnum[i].ID = i + OFFSET_ENUMBUTTON;

                LobbySettings_ButtonEnum[i].add_OnBtnPressCallback((Action<int>)((@enum) => SetPrivacyEnum(@enum)));
            }

            for (var i = 0; i < 4; i++)
            {
                LobbySettings_ButtonInt[i] = PageSettings.m_guiLayer.AddRectComp(PageSettings.m_subMenuButtonPrefab, GuiAnchor.TopLeft, Vector2.zero, LobbySettings_ScrollWindow.m_contentContainer.transform).Cast<CM_Item>();
                LobbySettings_ButtonInt[i].Setup();
                LobbySettings_ButtonInt[i].SetScaleFactor(0.70f);
                LobbySettings_ButtonInt[i].UpdateColliderOffset();
                LobbySettings_ButtonInt[i].transform.localPosition = new Vector3(25f + (252.5f * i), -205f, 0f);
                LobbySettings_ButtonInt[i].SetText($"{i + 1}");
                LobbySettings_ButtonInt[i].name = $"LobbySettings_ButtonInt_{i + 1}";
                var color = new UnhollowerBaseLib.Il2CppStructArray<Color>(1);
                color[0] = new Vector4(1, 1, 0, 1);
                LobbySettings_ButtonInt[i].m_textColorOver = color;
                LobbySettings_ButtonInt[i].ID = i + OFFSET_INTBUTTON;

                LobbySettings_ButtonInt[i].add_OnBtnPressCallback((Action<int>)((@int) => SetPlayerCount(@int)));
            }

            LobbySettings_ButtonEnum[0].m_textColorOut = colorActive;
            LobbySettings_ButtonEnum[0].OnHoverOut();

            LobbySettings_ButtonInt[3].m_textColorOut = colorActive;
            LobbySettings_ButtonInt[3].OnHoverOut();

        }



        public void OpenSettingsMenu()
        {
            CM_PageSettings.ToggleAudioTestLoop(false);
            PageSettings.ResetAllInputFields();
            PageSettings.ResetAllValueHolders();
            PageSettings.ShowSettingsWindow(LobbySettings_ScrollWindow);
            PageSettings.m_currentSubMenuId = (eSettingsSubMenuId)1000;
        }

        public void SetPrivacyEnum(int privacy)
        {
            privacy -= OFFSET_ENUMBUTTON;

            LobbyPrivacy = (LobbyRestrictions)privacy;

            foreach(var button in LobbySettings_ButtonEnum)
            {
                button.m_textColorOut = colorInactive;
                button.OnHoverOut();
            }
            LobbySettings_ButtonEnum[privacy].OnHoverIn();
            LobbySettings_ButtonEnum[privacy].m_textColorOut = colorActive;
        }
        public void SetPlayerCount(int count)
        {
            count -= OFFSET_INTBUTTON;

            LobbyLimit = count + 1;

            foreach (var button in LobbySettings_ButtonInt)
            {
                button.m_textColorOut = colorInactive;
                button.OnHoverOut();
            }
            LobbySettings_ButtonInt[count].OnHoverIn();
            LobbySettings_ButtonInt[count].m_textColorOut = colorActive;
        }

        public void GenerateLobbyButtons()
        {
            if (PageLoadout)
            {
                for (var i = 0; i < 4; i++)
                {
                    UI_KickButtons[i].gameObject.SetActive(Host);
                    UI_BanButtons[i].gameObject.SetActive(Host);
                }
                return;
            }
            PageLoadout = GuiManager.MainMenuLayer.PageLoadout;
            PlayerLobbyBars = GuiManager.MainMenuLayer.PageLoadout.m_playerLobbyBars;

            for (var i = 3; i >= 0; i--)
            {
                UI_KickButtons[i] = InitializeButton<CM_TimedButton>(
                    PageLoadout.m_readyButtonPrefab, PlayerLobbyBars[i].m_hasPlayerRoot.transform,
                    new Vector3(-63.0f, 342.0f, 0.0f), new Vector3(0.75f, 0.75f, 0.75f),
                    "Kick Player", $"KickButton_{i}", i + OFFSET_KICKBUTTON, (id) => KickPlayer(id - OFFSET_KICKBUTTON)
                );

                UI_BanButtons[i] = InitializeButton<CM_TimedButton>(
                    PageLoadout.m_readyButtonPrefab, PlayerLobbyBars[i].m_hasPlayerRoot.transform,
                    new Vector3(245.0f, 342.0f, 0.0f), new Vector3(0.75f, 0.75f, 0.75f),
                    "Ban Player", $"BanButton_{i}", i + OFFSET_BANBUTTON, (id) => BanPlayer(id - OFFSET_BANBUTTON)
                );
            }
        }

        public void GenerateMapButtons()
        {
            if (PageMap)
            {
                for ( var i = 3; i >= 0; i--)
                {
                    UI_KickButtons[i + 4].gameObject.SetActive(Host);
                    UI_BanButtons[i + 4].gameObject.SetActive(Host);
                }
                return;
            }

            PageMap = GuiManager.MainMenuLayer.PageMap;
            PUI_Inventories = PageMap.m_inventory;

            for (var i = 3; i >= 0; i--)
            {
                UI_KickButtons[i + 4] = PageMap.m_guiLayer.AddRectComp(
                    GuiManager.MainMenuLayer.PageLoadout.m_readyButtonPrefab,
                    GuiAnchor.TopLeft, Vector2.zero, PUI_Inventories[i].transform
                    ).Cast<CM_TimedButton>();
                UI_KickButtons[i + 4].transform.localPosition = new Vector3(0f, 32f, 0f);
                UI_KickButtons[i + 4].transform.localScale = new Vector3(0.75f, 0.75f, 0.75f);
                UI_KickButtons[i + 4].SetText("Kick");
                UI_KickButtons[i + 4].ID = i + 4 + OFFSET_KICKBUTTON;
                UI_KickButtons[i + 4].add_OnBtnPressCallback((Action<int>)((id) => KickPlayer(id - 4 - OFFSET_KICKBUTTON)));
                UI_KickButtons[i + 4].name = $"KickButton_{i + 4}";
                UI_KickButtons[i + 4].gameObject.SetActive(Host);

                UI_BanButtons[i + 4] = PageMap.m_guiLayer.AddRectComp(
                    GuiManager.MainMenuLayer.PageLoadout.m_readyButtonPrefab,
                    GuiAnchor.TopLeft, Vector2.zero, PUI_Inventories[i].transform
                    ).Cast<CM_TimedButton>();
                UI_BanButtons[i + 4].transform.localPosition = new Vector3(0f, -32f, 0f);
                UI_BanButtons[i + 4].transform.localScale = new Vector3(0.75f, 0.75f, 0.75f);
                UI_BanButtons[i + 4].SetText("Ban");
                UI_BanButtons[i + 4].ID = i + 4 + OFFSET_BANBUTTON;
                UI_BanButtons[i + 4].add_OnBtnPressCallback((Action<int>)((id) => BanPlayer(id - 4 - OFFSET_BANBUTTON)));
                UI_BanButtons[i + 4].name = $"BanButton_{i + 4}";
                UI_BanButtons[i + 4].gameObject.SetActive(Host);
            }
        }

        public T InitializeButton<T>(GameObject prefab, Transform parent, Vector3 relPosition, Vector3 relScale, string label, string name, int id, Action<int> callback) where T : CM_Item
        {
            var button = PageLoadout.m_guiLayer.AddRectComp(prefab, GuiAnchor.TopLeft, Vector2.zero, parent).Cast<T>(); Log(1);
            button.transform.localPosition = relPosition;
            button.transform.localScale = relScale;
            button.SetText(label);
            button.ID = id;
            button.add_OnBtnPressCallback(callback);
            button.name = $"UI_{name}";
            button.gameObject.SetActive(Host);

            return button;
        }

        private const int OFFSET_KICKBUTTON = 86000000;
        private const int OFFSET_BANBUTTON = 86000008;
        private const int OFFSET_ENUMBUTTON = 87000000;
        private const int OFFSET_INTBUTTON = 87000003;



        public CM_PageSettings PageSettings;
        public CM_Item LobbySettings_Button;
        public CM_ScrollWindow LobbySettings_ScrollWindow;
        public CM_Item[] LobbySettings_ButtonEnum = new CM_Item[3];
        public CM_Item[] LobbySettings_ButtonInt = new CM_Item[4];
        public CM_SettingsItem[] LobbySettings_LabelText = new CM_SettingsItem[2];

        public UnhollowerBaseLib.Il2CppStructArray<Color> colorActive = new Color[] { Color.green };
        public UnhollowerBaseLib.Il2CppStructArray<Color> colorInactive = new Color[] { Color.gray };

        public CS_Value<int> Privacy;
        public static int ApplyPrivacy(int value)
        {
            Current.LobbyPrivacy = (LobbyRestrictions)value;
            return value;
        }

        public static void RegIntVal(eCellSettingID ID, CS_Value<int> val)
        {
            if (CellSettingsManager.Current.m_intValueLookup.ContainsKey(ID))
            {
                CellSettingsManager.Current.m_intValueLookup[ID] = val;
                return;
            }
            CellSettingsManager.Current.m_intValueLookup.Add(ID, val);
        }


        //public CS_Value<int>

        private const int LOBBYLIMIT_SETTING = 400;
        private const int PRIVACY_SETTING = 401;

        public CM_PageLoadout PageLoadout;
        public CM_PlayerLobbyBar[] PlayerLobbyBars;
        public CM_TimedButton[] UI_KickButtons = new CM_TimedButton[8];
        public CM_TimedButton[] UI_BanButtons = new CM_TimedButton[8];

        public CM_PageMap PageMap;
        public PUI_Inventory[] PUI_Inventories;



        public static bool Host => SNet.IsMaster;
        public static LobbySettingsManager Current { get {
                if (_instance == null)
                {
                    _instance = ScriptableObject.CreateInstance<LobbySettingsManager>();
                    _instance.name = "Current";
                    GameObject.DontDestroyOnLoad(_instance);
                }
                return _instance;
            }}

        private static LobbySettingsManager _instance;

        public SNet_Lobby_STEAM SteamLobby => SNet.Lobby.Cast<SNet_Lobby_STEAM>();
        public string File_Blacklist { get; set; }
        public List<CSteamID> Blacklist { get; set; } = new List<CSteamID>();
        public List<CSteamID> Whitelist { get; set; } = new List<CSteamID>();
        public int LobbyLimit { get; set; } = 4;
        public LobbyRestrictions LobbyPrivacy { get; set; } = 0;
        public enum LobbyRestrictions
        {
            Public = 0,
            Private = 1,
            Locked = 2
        }
        public enum LogSeverity
        {
            Normal = 0,
            Warning = 1,
            Error = 2
        }
    }
}

