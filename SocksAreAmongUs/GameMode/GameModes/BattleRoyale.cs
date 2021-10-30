using System.Linq;
using HarmonyLib;
using UnhollowerBaseLib;
using UnityEngine;

namespace SocksAreAmongUs.GameMode.GameModes
{
    public class BattleRoyale : BaseGameMode
    {
        public override string Id => "battle_royale";
        internal static bool Enabled => GameModeManager.CurrentGameMode is BattleRoyale;

        /// <summary>
        /// Modify game end criteria so last man standing wins
        /// </summary>
        [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.CheckEndCriteria))]
        public static class CheckEndCriteriaPatch
        {
            public static bool Prefix(ShipStatus __instance)
            {
                if (!Enabled)
                    return true;

                var allPlayers = GameData.Instance.AllPlayers.ToArray();
                var dead = allPlayers.Count(x => x.IsDead || x.Disconnected);

                if (dead + 1 >= allPlayers.Count)
                {
                    var alive = allPlayers.SingleOrDefault(x => !x.IsDead && !x.Disconnected);

                    __instance.enabled = false;
                    ShipStatus.RpcEndGame(alive == null ? GameOverReason.ImpostorDisconnect : GameOverReason.ImpostorByKill, false);
                }

                return false;
            }
        }

        /// <summary>
        /// Disable game end by tasks
        /// </summary>
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

        /// <summary>
        /// Select all players as impostors
        /// </summary>
        [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.SelectInfected))]
        public static class SelectInfectedPatch
        {
            public static bool Prefix()
            {
                if (!Enabled)
                    return true;

                var list = GameData.Instance.AllPlayers.ToArray()
                    .Where(pcd => pcd?.PlayerName != null && !pcd.IsDead && !pcd.Disconnected)
                    .ToList();

                PlayerControl.LocalPlayer.RpcSetInfected(new Il2CppReferenceArray<GameData.PlayerInfo>(list.ToArray()));
                return false;
            }
        }

        [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameEnd))]
        public static class OnGameEndPatch
        {
            public static void Prefix()
            {
                if (!Enabled)
                    return;

                var allPlayers = GameData.Instance.AllPlayers.ToArray();
                var dead = allPlayers.Count(x => x.IsDead || x.Disconnected);

                if (dead + 1 >= allPlayers.Count)
                {
                    var alive = allPlayers.SingleOrDefault(x => !x.IsDead && !x.Disconnected);
                    foreach (var player in allPlayers)
                    {
                        RpcSetImpostor.Send(player.Object, alive != null && player.Object == alive.Object);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(IntroCutscene.Nested_0), nameof(IntroCutscene.Nested_0.MoveNext))]
        public static class CutscenePatch
        {
            public static void Prefix(IntroCutscene.Nested_0 __instance)
            {
                if (!Enabled)
                    return;

                var original = __instance.yourTeam.ToArray();

                __instance.yourTeam.Clear();
                __instance.yourTeam.Add(original.First());
            }
        }

        /// <summary>
        /// Make every other player name white
        /// </summary>
        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
        public static class RedNamePatch
        {
            public static void Postfix(PlayerControl __instance)
            {
                if (!Enabled || __instance.PlayerId == PlayerControl.LocalPlayer.PlayerId)
                    return;

                __instance.nameText.color = Color.white;
            }
        }

        /// <summary>
        /// Make every other player meeting name white
        /// </summary>
        [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Update))]
        public static class CreateButtonPatch
        {
            public static void Postfix(MeetingHud __instance)
            {
                if (!Enabled)
                    return;

                foreach (var playerVoteArea in __instance.playerStates)
                {
                    var playerInfo = GameData.Instance.GetPlayerById((byte) playerVoteArea.TargetPlayerId);
                    var flag = PlayerControl.LocalPlayer.Data.IsImpostor && playerInfo.Object == PlayerControl.LocalPlayer;
                    playerVoteArea.NameText.color = flag ? Palette.ImpostorRed : Color.white;
                }
            }
        }

        [HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.RpcEnterVent))]
        public static class RpcEnterVentPatch
        {
            public static bool Prefix()
            {
                return !Enabled;
            }
        }

        [HarmonyPatch(typeof(Vent), nameof(Vent.Use))]
        public static class VentUsePatch
        {
            public static bool Prefix()
            {
                return !Enabled;
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

        [HarmonyPatch(typeof(TranslationController), nameof(TranslationController.GetString), typeof(StringNames), typeof(Il2CppReferenceArray<Il2CppSystem.Object>))]
        public static class GetStringPatch
        {
            public static bool Prefix([HarmonyArgument(0)] StringNames stringId, [HarmonyArgument(1)] Il2CppReferenceArray<Il2CppSystem.Object> parts, ref string __result)
            {
                if (Enabled && stringId == StringNames.Victory)
                {
                    __result = "Victory Royale";

                    return false;
                }

                return true;
            }
        }

        // TODO
        // [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.CoEndGame))]
        // public static class CoEndGamePatch
        // {
        //     public static void Prefix()
        //     {
        //         if (!Enabled)
        //             return;
        //
        //         HEEEEBPANNA.MFIPHLKOFHG.Clear();
        //         HEEEEBPANNA.MFIPHLKOFHG.Add(new AMCELEOOFNB(GameData.Instance.AllPlayers.ToArray().Single(x => !x.IsDead && !x.Disconnected)));
        //     }
        // }
    }
}
