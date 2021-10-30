using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using SocksAreAmongUs.GameMode;
using UnhollowerBaseLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SocksAreAmongUs.Patches
{
    public abstract class CustomOption
    {
        public abstract string Base { get; }
        public StringNames Name { get; }
        public Action<OptionBehaviour> OnValueChanged { get; }

        public OptionBehaviour Behaviour { get; set; }

        protected CustomOption(StringNames name, Action<OptionBehaviour> onValueChanged)
        {
            Name = name;
            OnValueChanged = onValueChanged;
        }
    }

    public class CustomKeyValueOption : CustomOption
    {
        public override string Base => "MapName";
        public Func<Dictionary<int, string>> GetValues { get; }
        public Func<int> DefaultSelected { get; }

        public CustomKeyValueOption(StringNames name, Action<OptionBehaviour> onValueChanged, Func<Dictionary<int, string>> getValues, Func<int> defaultSelected) : base(name, onValueChanged)
        {
            GetValues = getValues;
            DefaultSelected = defaultSelected;
        }
    }

    public class CustomNumberOption : CustomOption
    {
        public override string Base => "NumLongTasks";
        public Func<float> DefaultValue { get; }

        public CustomNumberOption(StringNames name, Action<OptionBehaviour> onValueChanged, Func<float> defaultValue) : base(name, onValueChanged)
        {
            DefaultValue = defaultValue;
        }
    }

    public static class CustomOptions
    {
        public static List<CustomOption> Options { get; } = new List<CustomOption>
        {
            new CustomKeyValueOption(CustomStringNames.GameMode, option =>
                {
                    var keyValueOption = option.Cast<KeyValueOption>();
                    GameModeManager.CurrentGameMode = keyValueOption.Selected == 0 ? null : GameModeManager.GameModes.ElementAt(keyValueOption.Selected - 1);
                },
                () =>
                {
                    var values = new Dictionary<int, string>
                    {
                        [0] = "None"
                    };

                    foreach (var gameMode in GameModeManager.GameModes)
                    {
                        values[values.Count] = gameMode.Name;
                    }

                    return values;
                },
                () => GameModeManager.CurrentGameMode == null ? 0 : GameModeManager.GameModes.ToList().IndexOf(GameModeManager.CurrentGameMode) + 1),

            new CustomNumberOption(CustomStringNames.MaxPlayers, option =>
            {
                PlayerControl.GameOptions.MaxPlayers = Mathf.Clamp(option.Cast<NumberOption>().GetInt(), 0, CustomGameSettings.MaxPlayers);
                GameStartManager.Instance.LastPlayerCount = -1;
                PlayerControl.LocalPlayer.RpcSyncSettings(PlayerControl.GameOptions);
            }, () => PlayerControl.GameOptions.MaxPlayers)
        };

        private static CustomOption _current;

        [HarmonyPatch(typeof(GameSettingMenu), nameof(GameSettingMenu.OnEnable))]
        public static class GameSettingMenuPatch
        {
            public static void Prefix(GameSettingMenu __instance)
            {
                foreach (var option in Options)
                {
                    if (option.Behaviour != null)
                        continue;

                    var mapName = __instance.AllItems.Single(x => x.name == option.Base);

                    _current = option;
                    var gameModeObject = Object.Instantiate(mapName.gameObject, mapName.parent);
                    _current = null;
                    option.Behaviour = gameModeObject.GetComponent<OptionBehaviour>();
                    option.Behaviour.Title = option.Name;

                    __instance.AllItems = __instance.AllItems.AddItem(gameModeObject.transform).ToArray();

                    var menu = __instance.GetComponentInChildren<GameOptionsMenu>();
                    menu.Children = menu.Children.AddItem(option.Behaviour).ToArray();
                }
            }
        }

        [HarmonyPatch(typeof(KeyValueOption), nameof(KeyValueOption.OnEnable))]
        public static class KeyValueOptionPatch
        {
            public static bool Prefix(KeyValueOption __instance)
            {
                var current = Options.SingleOrDefault(x => x.Name == __instance.Title);
                if (current is CustomKeyValueOption keyValueOption)
                {
                    __instance.Selected = keyValueOption.DefaultSelected();
                    return false;
                }

                if (_current is CustomKeyValueOption option)
                {
                    __instance.Title = option.Name;

                    var gameModes = GameModeManager.GameModes;
                    __instance.Values = new Il2CppSystem.Collections.Generic.List<Il2CppSystem.Collections.Generic.KeyValuePair<string, int>>(gameModes.Count);

                    foreach (var pair in option.GetValues().OrderBy(x => x.Key))
                    {
                        __instance.Values.Add(new Il2CppSystem.Collections.Generic.KeyValuePair<string, int>
                        {
                            value = pair.Key,
                            key = pair.Value
                        });
                    }

                    __instance.Selected = option.DefaultSelected();
                    __instance.TitleText.text = DestroyableSingleton<TranslationController>.Instance.GetString(__instance.Title, new Il2CppReferenceArray<Il2CppSystem.Object>(0));
                    __instance.ValueText.text = __instance.Values[Mathf.Clamp(__instance.Selected, 0, __instance.Values.Count - 1)].Key;

                    return false;
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(NumberOption), nameof(NumberOption.OnEnable))]
        public static class NumberOptionPatch
        {
            public static bool Prefix(NumberOption __instance)
            {
                var current = Options.SingleOrDefault(x => x.Name == __instance.Title);
                if (current is CustomNumberOption numberOption)
                {
                    __instance.Value = numberOption.DefaultValue();
                    return false;
                }

                if (_current is CustomNumberOption option)
                {
                    __instance.Title = option.Name;
                    __instance.Value = option.DefaultValue();
                    __instance.TitleText.text = DestroyableSingleton<TranslationController>.Instance.GetString(__instance.Title, new Il2CppReferenceArray<Il2CppSystem.Object>(0));
                    __instance.ValueText.text = string.Format(__instance.FormatString, __instance.Value);
                    __instance.ValidRange.max = CustomGameSettings.MaxPlayers;

                    return false;
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(GameOptionsMenu), nameof(GameOptionsMenu.Start))]
        public static class GameOptionsMenuPatch
        {
            public static void Postfix(GameOptionsMenu __instance)
            {
                foreach (var option in Options)
                {
                    option.Behaviour.OnValueChanged = option.OnValueChanged;
                }
            }
        }
    }
}
