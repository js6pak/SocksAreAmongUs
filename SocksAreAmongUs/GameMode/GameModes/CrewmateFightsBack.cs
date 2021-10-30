using HarmonyLib;
using UnityEngine;

namespace SocksAreAmongUs.GameMode.GameModes
{
    public class CrewmateFightsBack : BaseGameMode
    {
        public override string Id => "crewmate_fights_back";
        internal static bool Enabled => GameModeManager.CurrentGameMode is CrewmateFightsBack;

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
        public static class FixedUpdatePatch
        {
            public static void Postfix(PlayerControl __instance)
            {
                if (!Enabled || !__instance.AmOwner || !__instance.CanMove || __instance.Data.IsDead || !AmongUsClient.Instance.IsGameStarted)
                    return;

                __instance.SetKillTimer(Mathf.Max(0f, __instance.killTimer - Time.fixedDeltaTime));
                var target = __instance.FindClosestTarget();

                if (target != null && target.inVent)
                {
                    target = null;
                }

                DestroyableSingleton<HudManager>.Instance.KillButton.SetTarget(target);
                DestroyableSingleton<HudManager>.Instance.KillButton.gameObject.SetActive(true);
            }
        }

        [HarmonyPatch(typeof(HudManager), nameof(HudManager.SetHudActive))]
        public static class SetHudActivePatch
        {
            public static void Postfix(HudManager __instance, [HarmonyArgument(0)] bool isActive)
            {
                var data = PlayerControl.LocalPlayer.Data;

                if (!Enabled || data.IsDead)
                    return;

                __instance.KillButton.gameObject.SetActive(isActive);
            }
        }

        [HarmonyPatch(typeof(KeyboardJoystick), nameof(KeyboardJoystick.Update))]
        public static class KeyboardJoystickPatch
        {
            public static void Postfix()
            {
                if (!Enabled)
                    return;

                if (PlayerControl.LocalPlayer != null && PlayerControl.LocalPlayer.Data != null && !PlayerControl.LocalPlayer.Data.IsImpostor && Input.GetKeyDown(KeyCode.Q))
                {
                    DestroyableSingleton<HudManager>.Instance.KillButton.PerformKill();
                }
            }
        }

        [HarmonyPatch(typeof(KillButtonManager), nameof(KillButtonManager.PerformKill))]
        public static class IsImpostorPatch
        {
            public static void Prefix(ref bool __state)
            {
                if (!Enabled || PlayerControl.LocalPlayer == null)
                    return;

                var data = PlayerControl.LocalPlayer.Data;
                __state = data.IsImpostor;
                data.IsImpostor = true;
            }

            public static void Postfix(bool __state)
            {
                if (!Enabled || PlayerControl.LocalPlayer == null)
                    return;

                PlayerControl.LocalPlayer.Data.IsImpostor = __state;
            }
        }

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.MurderPlayer))]
        public static class MurderPlayerPatch
        {
            public static void Prefix(PlayerControl __instance, ref bool __state)
            {
                // if (!Enabled)
                //     return;

                var data = __instance.Data;
                __state = data.IsImpostor;
                data.IsImpostor = true;
            }

            public static void Postfix(PlayerControl __instance, bool __state)
            {
                // if (!Enabled)
                //     return;

                __instance.Data.IsImpostor = __state;
            }
        }
    }
}
