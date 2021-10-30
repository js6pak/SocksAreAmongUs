using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Reactor.Extensions;
using UnityEngine;

namespace SocksAreAmongUs.GameMode.Roles
{
    public class JesterRole : CustomRole
    {
        public override string Name => "Jester";
        public override Color? Color => new Color32(240, 15, 225, byte.MaxValue);
        public override RoleSide Side => RoleSide.Solo;
        public override string Description => $"Trick the crewmates into thinking\nthat you are the [{Palette.ImpostorRed.ToHtmlStringRGBA()}]Impostor[]";

        public override bool ShouldColorName(PlayerControl player) => player.AmOwner;

        public override void OnSet(GameData.PlayerInfo playerInfo)
        {
            var player = playerInfo.Object;

            if (player.AmOwner)
            {
                foreach (var playerTask in player.myTasks)
                {
                    playerTask.OnRemove();
                    Object.Destroy(playerTask.gameObject);
                }
            }

            player.myTasks.Clear();
            player.Data.Tasks = null;
        }

        public const GameOverReason Reason = (GameOverReason) 7;

        private static readonly int _color = Shader.PropertyToID("_Color");

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Exiled))]
        public static class ExiledPatch
        {
            public static void Postfix(PlayerControl __instance)
            {
                if (AmongUsClient.Instance.AmHost && CustomRoles.Players.TryGetValue(__instance.PlayerId, out var customRole) && customRole is JesterRole)
                {
                    ShipStatus.RpcEndGame(Reason, false);
                }
            }
        }

        [HarmonyPatch(typeof(EndGameManager), nameof(EndGameManager.SetEverythingUp))]
        public static class SetEverythingUpPatch
        {
            public static void Postfix(EndGameManager __instance)
            {
                if (TempData.EndReason == Reason)
                {
                    __instance.BackgroundBar.material.SetColor(_color, CustomRoles.Jester.Color!.Value);
                }
            }
        }

        [HarmonyPatch]
        public static class GameOverReasonPatch
        {
            public static IEnumerable<MethodBase> TargetMethods()
            {
                return typeof(StatsManager).GetMethods(AccessTools.all).Where(x => x.ReturnType == typeof(void) && x.GetParameters().Length == 1 && x.GetParameters().ElementAt(0).ParameterType == typeof(GameOverReason));
            }

            public static void Prefix([HarmonyArgument(0)] GameOverReason reason, ref bool __runOriginal)
            {
                if (reason != Reason)
                {
                    __runOriginal = false;
                }
            }
        }
    }
}
