using CodeIsNotAmongUs.Patches.RemovePlayerLimit;
using HarmonyLib;
using UnityEngine;

namespace SocksAreAmongUs.GameMode.Roles
{
    public class MayorRole : CustomRole
    {
        public override string Name => "Mayor";
        public override Color? Color => UnityEngine.Color.yellow;
        public override string Description => "Your vote counts twice";
        public override RoleSide Side => RoleSide.Crewmate;

        public override bool ShouldColorName(PlayerControl player) => player.AmOwner;

        [HarmonyPatch(typeof(MeetingPatches.CheckForEndVotingPatch), nameof(MeetingPatches.CheckForEndVotingPatch.GetVotePower))]
        public static class GetVotePowerPatch
        {
            public static void Postfix(PlayerVoteArea playerVoteArea, ref byte __result)
            {
                if (playerVoteArea.TargetPlayerId != -1)
                {
                    if (CustomRoles.Players.TryGetValue((byte) playerVoteArea.TargetPlayerId, out var customRole) && customRole is MayorRole)
                    {
                        __result *= 2;
                    }
                }
            }
        }
    }
}
