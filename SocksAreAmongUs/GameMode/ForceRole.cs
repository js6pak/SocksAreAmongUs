using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using InnerNet;
using Reactor;
using Reactor.Extensions;
using UnhollowerBaseLib.Attributes;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SocksAreAmongUs.GameMode
{
    [RegisterInIl2Cpp]
    public class ForceRole : MonoBehaviour
    {
        public ForceRole(IntPtr ptr) : base(ptr)
        {
        }

        public enum Role
        {
            Random,
            Crewmate,
            Impostor
        }

        public static Dictionary<string, Role> Force { get; } = new Dictionary<string, Role>();

        [HideFromIl2Cpp]
        public ForceRoleMenu Menu { get; } = new ForceRoleMenu();

        private void Start()
        {
            SceneManager.add_sceneLoaded((Action<Scene, LoadSceneMode>) ((_, _) =>
            {
                Menu.Hide();
            }));
        }

        private void FixedUpdate()
        {
            if (AmongUsClient.Instance && AmongUsClient.Instance.AmHost)
            {
                if (Input.GetKeyDown(KeyCode.F2))
                {
                    Menu.Toggle();

                    var gameStartManager = GameStartManager.Instance;
                    if (gameStartManager && gameStartManager.StartButton)
                    {
                        gameStartManager.StartButton.gameObject.SetActive(!Menu.IsShown);
                    }
                }
            }
            else if (Menu.IsShown)
            {
                Menu.Hide();
            }
        }

        [HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.ResetStartState))]
        private static class ResetStartStatePatch
        {
            public static bool Prefix()
            {
                return !PluginSingleton<SocksAreAmongUsPlugin>.Instance.ForceRole.Menu.IsShown;
            }
        }

        [HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.Update))]
        private static class UpdatePatch
        {
            public static bool Prefix(GameStartManager __instance)
            {
                if (!GameData.Instance || !__instance.MakePublicButton)
                {
                    return false;
                }

                __instance.MakePublicButton.sprite = (AmongUsClient.Instance.IsGamePublic ? __instance.PublicGameImage : __instance.PrivateGameImage);
                if (GameData.Instance.PlayerCount != __instance.LastPlayerCount)
                {
                    __instance.LastPlayerCount = GameData.Instance.PlayerCount;
                    var arg = "[FF0000FF]";
                    if (__instance.LastPlayerCount > __instance.MinPlayers)
                    {
                        arg = "[00FF00FF]";
                    }

                    if (__instance.LastPlayerCount == __instance.MinPlayers)
                    {
                        arg = "[FFFF00FF]";
                    }

                    __instance.PlayerCounter.text = $"{arg}{__instance.LastPlayerCount}/{PlayerControl.GameOptions.MaxPlayers}";
                    __instance.StartButton.color = ((__instance.LastPlayerCount >= __instance.MinPlayers) ? Palette.EnabledColor : Palette.DisabledClear);
                    if (DestroyableSingleton<DiscordManager>.InstanceExists)
                    {
                        if (AmongUsClient.Instance.AmHost && AmongUsClient.Instance.GameMode == global::GameModes.OnlineGame)
                        {
                            DestroyableSingleton<DiscordManager>.Instance.SetInLobbyHost(__instance.LastPlayerCount, AmongUsClient.Instance.GameId);
                        }
                        else
                        {
                            DestroyableSingleton<DiscordManager>.Instance.SetInLobbyClient(__instance.LastPlayerCount, AmongUsClient.Instance.GameId);
                        }
                    }
                }

                if (AmongUsClient.Instance.AmHost)
                {
                    if (__instance.startState == GameStartManager.StartingStates.Countdown)
                    {
                        var num = Mathf.CeilToInt(__instance.countDownTimer);
                        __instance.countDownTimer -= Time.deltaTime;
                        var num2 = Mathf.CeilToInt(__instance.countDownTimer);
                        __instance.GameStartText.text = DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.GameStarting, new Il2CppSystem.Object[]
                        {
                            (Il2CppSystem.String) num2.ToString()
                        });
                        if (num != num2)
                        {
                            PlayerControl.LocalPlayer.RpcSetStartCounter(num2);
                        }

                        if (num2 <= 0)
                        {
                            __instance.FinallyBegin();
                            return false;
                        }
                    }
                    else
                    {
                        __instance.GameStartText.text = string.Empty;
                    }
                }

                return false;
            }
        }

        public class ForceRoleMenu : PlayerSelectMenu
        {
            public override Vector2 Size { get; } = new Vector2(295, 300);
            public override Vector2 ButtonSize { get; } = new Vector2(250, 40);
            public override int Columns => 1;

            public override void OnClick(PlayerControl playerControl)
            {
            }

            protected override void UpdateButton(PlayerControl player, Button button, Text text, Image image)
            {
                base.UpdateButton(player, button, text, image);

                if (CustomRoles.Players.TryGetValue(player.PlayerId, out var customRole) && customRole.Color.HasValue)
                {
                    text.color = customRole.Color.Value;
                }

                if (Force.TryGetValue(player.Data.PlayerName, out var role))
                {
                    var dropdown = button.GetComponentInChildren<Dropdown>();

                    if (dropdown)
                    {
                        dropdown.value = (int) role;
                    }
                }
            }

            protected override Button AddButton(GameObject content, PlayerControl player)
            {
                var button = base.AddButton(content, player);
                button.interactable = true;
                button.enabled = false;

                var dropdown = DefaultControls.CreateDropdown(GUIExtensions.StandardResources).GetComponent<Dropdown>();
                dropdown.template.GetComponent<ScrollRect>().scrollSensitivity = 32;

                var rectTransform = dropdown.gameObject.GetComponent<RectTransform>();
                rectTransform.SetParent(button.transform);

                rectTransform.anchorMax = rectTransform.anchorMin = rectTransform.pivot = new Vector2(1f, 0.5f);
                rectTransform.SetSize(120, 25);
                rectTransform.anchoredPosition = new Vector2(-5, 0);

                dropdown.captionText.fontSize = 0;
                dropdown.captionText.rectTransform.offsetMin = new Vector2(10f, 0f);
                dropdown.captionText.rectTransform.offsetMax = new Vector2(25f, 0f);

                dropdown.options.Clear();

                foreach (var s in Enum.GetNames(typeof(Role)))
                {
                    dropdown.options.Add(new Dropdown.OptionData(s));
                }

                foreach (var customRole in CustomRoles.Roles)
                {
                    dropdown.options.Add(new Dropdown.OptionData(customRole.Name));
                }

                dropdown.onValueChanged.AddListener((Action<int>) (i =>
                {
                    var roleType = (Role) i;
                    Force[player.Data.PlayerName] = roleType;

                    if (AmongUsClient.Instance.GameState == InnerNetClient.GameStates.Started || TutorialManager.InstanceExists)
                    {
                        if (i <= 3)
                        {
                            CustomRoles.Players[player.PlayerId] = null;
                            RpcSetImpostor.Send(player, roleType == Role.Impostor);
                        }
                        else
                        {
                            var newRole = CustomRoles.Roles.Single(x => x.Force == roleType);
                            RpcSetImpostor.Send(player, newRole.Side == RoleSide.Impostor);
                            CustomRoles.Players[player.PlayerId] = newRole;
                        }

                        CustomRoles.SetCustomRolePatch.Postfix();
                    }
                }));

                if (Force.TryGetValue(player.Data.PlayerName, out var role))
                {
                    dropdown.value = (int) role;
                }

                // if (AmongUsClient.Instance.GameState != InnerNetClient.GameStates.Joined && !TutorialManager.InstanceExists)
                // {
                //     dropdown.interactable = false;
                // }

                return button;
            }
        }
    }
}
