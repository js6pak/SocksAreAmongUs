using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace SocksAreAmongUs.GameMode.Roles
{
    public class TrollRole : CustomRole
    {
        public override string Name => "Troll";
        public override Color? Color => new Color32(29, 152, 83, 255);
        public override string Description => "If you get killed, you win.";
        public override RoleSide Side => RoleSide.Solo;

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

        public const GameOverReason Reason = (GameOverReason) 8;

        private static readonly int _color = Shader.PropertyToID("_Color");

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.MurderPlayer))]
        public static class MurderPlayerPatch
        {
            public static void Postfix([HarmonyArgument(0)] PlayerControl target)
            {
                if (AmongUsClient.Instance.AmHost && CustomRoles.Players.TryGetValue(target.PlayerId, out var customRole) && customRole is TrollRole)
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
                    __instance.BackgroundBar.material.SetColor(_color, CustomRoles.Troll.Color!.Value);
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
