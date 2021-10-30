using System.Linq;
using HarmonyLib;
using UnhollowerBaseLib;
using UnityEngine;

namespace SocksAreAmongUs.GameMode.GameModes
{
    public class InfectedRoyale : BaseGameMode
    {
        public override string Id => "infected_royale";
        internal static bool Enabled => GameModeManager.CurrentGameMode is InfectedRoyale;

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

                target.Data.IsImpostor = true;
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
                // target.SetPlayerMaterialColors(deadBody.GetComponent<Renderer>());

                return false;
            }
        }

        // [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
        // public static class PlayerNameUpdatePatch
        // {
        //     public static void Postfix(PlayerControl __instance)
        //     {
        //         if (__instance && __instance.Data.IsImpostor)
        //         {
        //             __instance.nameText.color = Palette.ImpostorRed;
        //         }
        //     }
        // }

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
                var infected = allPlayers.Count(x => x.IsDead || x.Disconnected || x.IsImpostor);

                if (infected + 1 >= allPlayers.Count)
                {
                    var alive = allPlayers.SingleOrDefault(x => !x.IsDead && !x.Disconnected && !x.IsImpostor);

                    __instance.enabled = false;
                    ShipStatus.RpcEndGame(alive == null ? GameOverReason.HumansDisconnect : GameOverReason.HumansByVote, false);
                }

                return false;
            }
        }

        [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.CheckTaskCompletion))]
        public static class CheckTaskCompletionPatch
        {
            public static bool Prefix(ref bool __result)
            {
                if (!Enabled)
                    return true;

                return __result = false;
            }
        }

        [HarmonyPatch(typeof(PingTracker), nameof(PingTracker.Update))]
        public static class NumberPatch
        {
            public static void Postfix(PingTracker __instance)
            {
                if (!Enabled)
                    return;

                __instance.text.text += $"\n{GameData.Instance.AllPlayers.ToArray().Count(x => !x.IsDead && !x.Disconnected && !x.IsImpostor)}/{GameData.Instance.AllPlayers.Count}";
            }
        }

        [HarmonyPatch(typeof(EndGameManager), nameof(EndGameManager.SetEverythingUp))]
        public static class SetEverythingUpPatch
        {
            public static bool IsRunning { get; private set; }

            public static void Prefix()
            {
                IsRunning = true;
            }

            public static void Postfix()
            {
                IsRunning = false;
            }
        }

        [HarmonyPatch(typeof(TempData), nameof(TempData.DidHumansWin))]
        public static class DidHumansWinPatch
        {
            public static bool Prefix(ref bool __result)
            {
                if (Enabled && SetEverythingUpPatch.IsRunning)
                {
                    __result = false;
                    return false;
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CmdReportDeadBody))]
        public static class CmdReportDeadBodyPatch
        {
            public static bool Prefix()
            {
                return !Enabled;
            }
        }

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.ReportClosest))]
        public static class ReportClosestPatch
        {
            public static bool Prefix()
            {
                return !Enabled;
            }
        }

        [HarmonyPatch(typeof(DeadBody), nameof(DeadBody.OnClick))]
        public static class DeadBodyPatch
        {
            public static bool Prefix()
            {
                return !Enabled;
            }
        }

        [HarmonyPatch(typeof(HudManager), nameof(HudManager.OpenMeetingRoom))]
        public static class OpenMeetingRoomPatch
        {
            public static bool Prefix()
            {
                return !Enabled;
            }
        }

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.ReportDeadBody))]
        public static class ReportDeadBodyPatch
        {
            public static bool Prefix()
            {
                return !Enabled;
            }
        }
    }
}
