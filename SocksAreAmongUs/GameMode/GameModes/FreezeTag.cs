using System.Collections.Generic;
using System.Linq;
using CodeIsNotAmongUs;
using CodeIsNotAmongUs.Patches.RemovePlayerLimit;
using HarmonyLib;
using Hazel;
using Reactor;
using Reactor.Networking;
using UnityEngine;

namespace SocksAreAmongUs.GameMode.GameModes
{
    public class FreezeTag : BaseGameMode
    {
        public override string Id => "freeze_tag";
        internal static bool Enabled => GameModeManager.CurrentGameMode is FreezeTag;

        public static Dictionary<int, bool> Frozen { get; } = new Dictionary<int, bool>();
        public static bool IsActive { get; set; }

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CanMove), MethodType.Getter)]
        public static class CanMovePatch
        {
            public static bool Prefix(ref bool __result)
            {
                if (IsActive)
                {
                    return __result = false;
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.ReportDeadBody))]
        public static class PlayerControl_ReportDeadBodyPatch
        {
            public static bool Prefix()
            {
                return !IsActive;
            }
        }

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CmdReportDeadBody))]
        public static class CmdReportDeadBodyPatch
        {
            public static bool Prefix()
            {
                return !IsActive;
            }
        }

        [HarmonyPatch(typeof(Minigame), nameof(Minigame.Begin))]
        public static class MinigamePatch
        {
            public static bool Prefix()
            {
                return !IsActive;
            }
        }

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FindClosestTarget))]
        public static class FindClosestTargetPatch
        {
            public static bool Prefix()
            {
                return !Enabled;
            }
        }

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FindClosestTarget))]
        public static class KillButtonPatch
        {
            public static bool Prefix()
            {
                return !Enabled;
            }
        }

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
        public static class RedNamePatch
        {
            public static void Postfix(PlayerControl __instance)
            {
                if (!Enabled)
                    return;

                if (Frozen.TryGetValue(__instance.PlayerId, out var x) && x)
                {
                    __instance.nameText.color = Color.cyan;
                }
                else
                {
                    __instance.nameText.color = __instance.Data.IsImpostor ? Palette.ImpostorRed : Color.white;
                }
            }
        }

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
        public static class FixedUpdatePatch
        {
            public static void Postfix(PlayerControl __instance)
            {
                if (!Enabled)
                    return;

                if (__instance.AmOwner && __instance.Data.IsImpostor && DestroyableSingleton<HudManager>.InstanceExists)
                {
                    DestroyableSingleton<HudManager>.Instance.KillButton.gameObject.SetActive(false);
                }

                if (!__instance.AmOwner || !AmongUsClient.Instance.IsGameStarted || RemovePlayerLimit.IsInCutscene || __instance.Data.IsImpostor)
                    return;

                var truePosition = __instance.GetTruePosition();

                var flag = false;

                foreach (var playerInfo in GameData.Instance.AllPlayers)
                {
                    if (playerInfo.Object && !playerInfo.Object.AmOwner && !playerInfo.IsDead && !playerInfo.Disconnected)
                    {
                        if ((playerInfo.Object.GetTruePosition() - truePosition).magnitude < 0.6)
                        {
                            flag = playerInfo.IsImpostor;

                            if (flag)
                            {
                                break;
                            }
                        }
                    }
                }

                if (IsActive != flag)
                {
                    Rpc<RpcSetFrozen>.Instance.Send(flag);
                }

                IsActive = flag;

                if (IsActive)
                {
                    if (Minigame.Instance)
                    {
                        Minigame.Instance.Close();
                    }

                    PlayerControl.LocalPlayer.MyPhysics.body.velocity = Vector2.zero;
                    PlayerControl.LocalPlayer.MyPhysics.DirtyBits |= 3U;
                }
            }
        }

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.SetInfected))]
        public static class SetInfectedPatch
        {
            public static void Postfix()
            {
                Frozen.Clear();
            }
        }

        [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.CheckEndCriteria))]
        public static class CheckEndCriteriaPatch
        {
            public static void Prefix(ShipStatus __instance)
            {
                if (!Enabled)
                    return;

                if (Frozen.Values.Count(x => x) >= GameData.Instance.AllPlayers.ToArray().Count(x => !x.IsImpostor && !x.Disconnected))
                {
                    __instance.enabled = false;
                    ShipStatus.RpcEndGame(GameOverReason.ImpostorByKill, false);
                }
            }
        }

        [RegisterCustomRpc((uint) CustomRpcCalls.SetFrozen)]
        public class RpcSetFrozen : PlayerCustomRpc<SocksAreAmongUsPlugin, bool>
        {
            public RpcSetFrozen(SocksAreAmongUsPlugin plugin, uint id) : base(plugin, id)
            {
            }

            public override RpcLocalHandling LocalHandling => RpcLocalHandling.Before;

            public override void Write(MessageWriter writer, bool data)
            {
                writer.Write(data);
            }

            public override bool Read(MessageReader reader)
            {
                return reader.ReadBoolean();
            }

            public override void Handle(PlayerControl target, bool value)
            {
                Frozen[target.PlayerId] = value;
            }
        }
    }
}
