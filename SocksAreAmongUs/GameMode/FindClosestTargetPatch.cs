using System.Linq;
using HarmonyLib;
using SocksAreAmongUs.GameMode.GameModes;

namespace SocksAreAmongUs.GameMode
{
    /// <summary>
    /// Allow to kill other impostors
    /// </summary>
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FindClosestTarget))]
    public static class FindClosestTargetPatch
    {
        public static void Prefix(out bool[] __state)
        {
            __state = null;
            if (!BattleRoyale.Enabled && !What.Enabled && !CrewmateFightsBack.Enabled && !CustomRoles.Sheriff.Test(PlayerControl.LocalPlayer.Data))
                return;

            __state = GameData.Instance.AllPlayers.ToArray().Select(x => x.IsImpostor).ToArray();
            foreach (var player in GameData.Instance.AllPlayers)
            {
                if (player.Object == PlayerControl.LocalPlayer)
                {
                    if (CrewmateFightsBack.Enabled)
                    {
                        player.IsImpostor = true;
                    }

                    continue;
                }

                player.IsImpostor = false;
            }
        }

        public static void Postfix(bool[] __state)
        {
            if (!BattleRoyale.Enabled && !What.Enabled && !CrewmateFightsBack.Enabled && !CustomRoles.Sheriff.Test(PlayerControl.LocalPlayer.Data))
                return;

            var allPlayers = GameData.Instance.AllPlayers.ToArray();

            for (var i = 0; i < __state.Length; i++)
            {
                var isImpostor = __state[i];

                if (i >= 0 && i < allPlayers.Length)
                {
                    allPlayers[i].IsImpostor = isImpostor;
                }
            }
        }
    }
}
