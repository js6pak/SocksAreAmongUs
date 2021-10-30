using HarmonyLib;
using UnityEngine;

namespace SocksAreAmongUs.GameMode.Roles
{
    public class SpectatorRole : CustomRole
    {
        public override string Name => "Spectator";
        public override Color? Color => Palette.DisabledGrey;
        public override bool ShouldColorName(PlayerControl player) => false;
        public override RoleSide Side => RoleSide.Solo;

        public override void OnSet(GameData.PlayerInfo playerInfo)
        {
            var player = playerInfo.Object;

            if (playerInfo.Disconnected)
                return;

            if (player.CurrentPet)
            {
                Object.Destroy(player.CurrentPet.gameObject);
            }

            player.Die(DeathReason.Disconnect);

            if (player.AmOwner)
            {
                DestroyableSingleton<HudManager>.Instance.ShadowQuad.gameObject.SetActive(false);

                foreach (var playerTask in player.myTasks)
                {
                    playerTask.OnRemove();
                    Object.Destroy(playerTask.gameObject);
                }

                player.myTasks.Clear();
            }

            player.Data.Tasks = null;
        }

        [HarmonyPatch(typeof(IntroCutscene.Nested_0), nameof(IntroCutscene.Nested_0.MoveNext))]
        public static class IntroCutscenePatch
        {
            public static void Prefix(IntroCutscene.Nested_0 __instance)
            {
                foreach (var playerControl in PlayerControl.AllPlayerControls)
                {
                    if (CustomRoles.Players.TryGetValue(playerControl.PlayerId, out var customRole) && customRole is SpectatorRole)
                    {
                        __instance.yourTeam.Remove(playerControl);
                    }
                }
            }
        }
    }
}
