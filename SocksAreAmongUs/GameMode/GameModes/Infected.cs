using System;
using System.Linq;
using HarmonyLib;
using UnhollowerBaseLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SocksAreAmongUs.GameMode.GameModes
{
    public class Infected : BaseGameMode<Infected.Component>
    {
        public override string Id => "infected";
        internal static bool Enabled => GameModeManager.CurrentGameMode is Infected;

        public class Component : MonoBehaviour
        {
            public Component(IntPtr ptr) : base(ptr)
            {
            }

            private static readonly Color _impostorRed = Palette.ImpostorRed;

            public void Update()
            {
                Palette.ImpostorRed = Enabled || InfectedRoyale.Enabled ? new Color(0.49f, 0.7f, 0.26f) : _impostorRed;
            }
        }

        // [HarmonyPatch(typeof(ReportButtonManager), nameof(ReportButtonManager.SetActive))]
        // public static class ReportButtonPatch
        // {
        //     public static bool Prefix(ReportButtonManager __instance)
        //     {
        //         if (!Enabled)
        //             return true;
        //
        //         __instance.renderer.enabled = false;
        //         return false;
        //     }
        // }

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.SetKillTimer))]
        public static class SetKillTimerPatch
        {
            public static void Prefix([HarmonyArgument(0)] ref float timer)
            {
                if (!Enabled)
                    return;

                if (timer == 10)
                {
                    timer = PlayerControl.GameOptions.KillCooldown;
                }
            }
        }

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.MurderPlayer))]
        public static class MurderPlayerPatch
        {
            public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target)
            {
                if (!Enabled)
                    return true;

                if (__instance.AmOwner)
                {
                    SoundManager.Instance.PlaySound(PlayerControl.LocalPlayer.KillSfx, false, 0.8f);
                }

                RpcSetImpostor.Handle(target, true);
                __instance.SetKillTimer(PlayerControl.GameOptions.KillCooldown);
                target.SetKillTimer(PlayerControl.GameOptions.KillCooldown);
                __instance.NetTransform.SnapTo(target.transform.position);

                var killAnimation = __instance.KillAnimations.First();
                var deadBody = Object.Instantiate(killAnimation.bodyPrefab);
                var vector = target.transform.position + killAnimation.BodyOffset;
                vector.z = vector.y / 1000f;
                deadBody.transform.position = vector;
                deadBody.ParentId = 0;
                // deadBody.ParentId = target.PlayerId;
                // target.SetPlayerMaterialColors(deadBody.GetComponent<Renderer>());

                return false;
            }
        }

        [HarmonyPatch(typeof(TranslationController), nameof(TranslationController.GetString), typeof(StringNames), typeof(Il2CppReferenceArray<Il2CppSystem.Object>))]
        public static class GetStringPatch
        {
            public static bool Prefix([HarmonyArgument(0)] StringNames stringId, [HarmonyArgument(1)] Il2CppReferenceArray<Il2CppSystem.Object> parts, ref string __result)
            {
                if (Enabled && stringId == StringNames.Impostor)
                {
                    __result = "Infected";

                    return false;
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.CheckEndCriteria))]
        public static class CheckEndCriteriaPatch
        {
            public static bool Prefix(ShipStatus __instance)
            {
                if (!Enabled)
                    return true;

                var allPlayers = GameData.Instance.AllPlayers.ToArray();

                if (allPlayers.All(x => x.IsImpostor || x.IsDead || x.Disconnected))
                {
                    __instance.enabled = false;
                    ShipStatus.RpcEndGame(GameOverReason.ImpostorByKill, false);
                    return false;
                }

                return true;
            }
        }
    }
}
