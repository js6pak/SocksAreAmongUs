using HarmonyLib;
using Reactor.Extensions;
using UnityEngine;

namespace SocksAreAmongUs.GameMode.Roles
{
    public class SheriffRole : CustomRole
    {
        public override string Name => "Sheriff";
        public override Color? Color => Palette.Orange;
        public override string Description => $"Shoot the [{Palette.ImpostorRed.ToHtmlStringRGBA()}]Impostor[],\nor miss and kill yourself";
        public override RoleSide Side => RoleSide.Crewmate;

        public override bool ShouldColorName(PlayerControl player) => player.AmOwner;

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
        public static class FixedUpdatePatch
        {
            public static void Postfix(PlayerControl __instance)
            {
                if (!__instance.AmOwner || !__instance.CanMove || __instance.Data.IsDead || !AmongUsClient.Instance.IsGameStarted || !CustomRoles.Sheriff.Test(__instance.Data))
                    return;

                __instance.SetKillTimer(Mathf.Max(0f, __instance.killTimer - Time.fixedDeltaTime));
                var target = __instance.FindClosestTarget();

                if (target != null && target.inVent)
                {
                    target = null;
                }

                DestroyableSingleton<HudManager>.Instance.KillButton.SetTarget(target);
            }
        }

        [HarmonyPatch(typeof(HudManager), nameof(HudManager.SetHudActive))]
        public static class SetHudActivePatch
        {
            public static void Postfix(HudManager __instance, [HarmonyArgument(0)] bool isActive)
            {
                var data = PlayerControl.LocalPlayer.Data;

                if (data.IsDead || !CustomRoles.Sheriff.Test(data))
                    return;

                __instance.KillButton.gameObject.SetActive(isActive);
            }
        }

        [HarmonyPatch(typeof(KeyboardJoystick), nameof(KeyboardJoystick.Update))]
        public static class KeyboardJoystickPatch
        {
            public static void Postfix()
            {
                if (PlayerControl.LocalPlayer != null && PlayerControl.LocalPlayer.Data != null && !PlayerControl.LocalPlayer.Data.IsImpostor && CustomRoles.Sheriff.Test(PlayerControl.LocalPlayer.Data) && Input.GetKeyDown(KeyCode.Q))
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
                if (PlayerControl.LocalPlayer == null)
                    return;

                var data = PlayerControl.LocalPlayer.Data;

                if (CustomRoles.Sheriff.Test(data))
                {
                    __state = data.IsImpostor;
                    data.IsImpostor = true;
                }
            }

            public static void Postfix(bool __state)
            {
                if (PlayerControl.LocalPlayer == null)
                    return;

                var data = PlayerControl.LocalPlayer.Data;

                if (CustomRoles.Sheriff.Test(data))
                {
                    data.IsImpostor = __state;
                }
            }
        }

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.MurderPlayer))]
        public static class MurderPlayerPatch
        {
            public static void Prefix(PlayerControl __instance, ref bool __state)
            {
                if (CustomRoles.Sheriff.Test(__instance.Data))
                {
                    var data = __instance.Data;
                    __state = data.IsImpostor;
                    data.IsImpostor = true;
                }
            }

            public static void Postfix(PlayerControl __instance, bool __state, [HarmonyArgument(0)] PlayerControl target)
            {
                if (__instance && CustomRoles.Sheriff.Test(__instance.Data))
                {
                    if (__instance.AmOwner && target && !target.AmOwner && !target.Data.IsImpostor)
                    {
                        __instance.RpcMurderPlayer(__instance);
                    }

                    __instance.Data.IsImpostor = __state;
                }
            }
        }
    }
}
