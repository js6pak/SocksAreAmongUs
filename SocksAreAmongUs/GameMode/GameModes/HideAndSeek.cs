using HarmonyLib;

namespace SocksAreAmongUs.GameMode.GameModes
{
    public class HideAndSeek : BaseGameMode
    {
        public override string Id => "hide_and_seek";
        internal static bool Enabled => GameModeManager.CurrentGameMode is HideAndSeek;

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcSetInfected))]
        public static class RpcSetInfectedPatch
        {
            public static void Postfix()
            {
                if (!Enabled)
                    return;

                foreach (var playerControl in PlayerControl.AllPlayerControls)
                {
                    var isImpostor = playerControl.Data.IsImpostor;
                    // playerControl.RpcSetName(isImpostor ? "Seeker" : "Hider");
                    playerControl.RpcSetColor(isImpostor ? (byte) 0 : (byte) 1);
                    playerControl.RpcSetSkin(0);
                    playerControl.RpcSetHat(0);
                    playerControl.RpcSetPet(0);
                }
            }
        }

        // [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.SetInfected))]
        // public static class RedNamePatch
        // {
        //     public static void Postfix(PlayerControl __instance, [HarmonyArgument(0)] Il2CppStructArray<byte> infected)
        //     {
        //         foreach (var playerId in infected)
        //         {
        //             var player = GameData.Instance.GetPlayerById(playerId);
        //             player.Object.nameText.color = player.IsImpostor ? Palette.ImpostorRed : Color.white;
        //         }
        //     }
        // }
    }
}
