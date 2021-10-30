using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace SocksAreAmongUs.Patches
{
    public static class CustomGameSettings
    {
        public const int MaxPlayers = 120;

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FindClosestTarget))]
        public static class InfiniteKillDistancePatch
        {
            public static bool Prefix(PlayerControl __instance, ref PlayerControl __result)
            {
                if (PlayerControl.GameOptions.KillDistance != 3)
                    return true;

                var num = float.MaxValue;
                var truePosition = __instance.GetTruePosition();
                var allPlayers = GameData.Instance.AllPlayers;
                foreach (var playerInfo in allPlayers)
                {
                    if (playerInfo.Object && playerInfo.Object.PlayerId != __instance.PlayerId && !playerInfo.IsDead && !playerInfo.IsImpostor)
                    {
                        var @object = playerInfo.Object;
                        if (@object)
                        {
                            var vector = @object.GetTruePosition() - truePosition;
                            var magnitude = vector.magnitude;
                            if (magnitude <= num && !PhysicsHelpers.AnyNonTriggersBetween(truePosition, vector.normalized, magnitude, Constants.ShipAndObjectsMask))
                            {
                                __result = @object;
                                num = magnitude;
                            }
                        }
                    }
                }

                return false;
            }
        }

        [HarmonyPatch(typeof(GameOptionsMenu), nameof(GameOptionsMenu.Start))]
        public static class GameOptionsMenuPatch
        {
            public static void Postfix(GameOptionsMenu __instance)
            {
                foreach (var option in __instance.Children)
                {
                    var stringOption = option.TryCast<StringOption>();
                    if (stringOption)
                    {
                        if (stringOption.Title == StringNames.GameKillDistance)
                        {
                            stringOption.Values = stringOption.Values.AddItem((StringNames) CustomStringNames.Infinite).ToArray();
                        }
                    }

                    var numberOption = option.TryCast<NumberOption>();
                    if (numberOption)
                    {
                        if (numberOption.Title == StringNames.GameNumImpostors)
                        {
                            numberOption.ValidRange.max = MaxPlayers;
                        }

                        if (numberOption.Title == StringNames.GameKillCooldown)
                        {
                            numberOption.ValidRange.min = 0;
                        }
                    }
                }
            }
        }

        [HarmonyPatch]
        public static class QuickNumberOptionPatch
        {
            public static System.Collections.Generic.IEnumerable<MethodBase> TargetMethods()
            {
                yield return AccessTools.Method(typeof(NumberOption), nameof(NumberOption.Increase));
                yield return AccessTools.Method(typeof(NumberOption), nameof(NumberOption.Decrease));
            }

            public static void Prefix(NumberOption __instance, out float __state)
            {
                __state = __instance.Increment;

                if (Input.GetKey(KeyCode.LeftShift))
                {
                    __instance.Increment *= 10;
                }
            }

            public static void Postfix(NumberOption __instance, float __state)
            {
                __instance.Increment = __state;
            }
        }

        public static class QuickKeyValueOptionPatch
        {
            private const int Increment = 2;

            [HarmonyPatch(typeof(KeyValueOption), nameof(KeyValueOption.Increase))]
            [HarmonyPrefix]
            public static bool Increase(KeyValueOption __instance)
            {
                if (Input.GetKey(KeyCode.LeftShift))
                {
                    __instance.Selected = Mathf.Clamp(__instance.Selected + Increment, 0, __instance.Values.Count - Increment);
                    __instance.OnValueChanged.Invoke(__instance);

                    return false;
                }

                return true;
            }

            [HarmonyPatch(typeof(KeyValueOption), nameof(KeyValueOption.Decrease))]
            [HarmonyPrefix]
            public static bool Decrease(KeyValueOption __instance)
            {
                if (Input.GetKey(KeyCode.LeftShift))
                {
                    __instance.Selected = Mathf.Clamp(__instance.Selected - Increment, 0, __instance.Values.Count - Increment);
                    __instance.OnValueChanged.Invoke(__instance);

                    return false;
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(KillButtonManager), nameof(KillButtonManager.SetCoolDown))]
        public static class InvisibleKillButtonPatch
        {
            private static readonly int _percent = Shader.PropertyToID("_Percent");

            public static bool Prefix(KillButtonManager __instance, [HarmonyArgument(1)] float maxTimer)
            {
                if (maxTimer == 0)
                {
                    if (__instance.renderer)
                    {
                        __instance.renderer.material.SetFloat(_percent, 0f);
                    }

                    __instance.isCoolingDown = false;
                    __instance.TimerText.gameObject.SetActive(false);

                    return false;
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.SetKillTimer))]
        [HarmonyPriority(Priority.First)]
        public static class PlayerControlSetKillTimerPatch
        {
            public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] float time)
            {
                __instance.killTimer = time;

                if (__instance.AmOwner)
                {
                    HudManager.Instance.KillButton.SetCoolDown(time, PlayerControl.GameOptions.KillCooldown);
                }

                return false;
            }
        }
    }
}
