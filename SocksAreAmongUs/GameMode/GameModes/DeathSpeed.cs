using HarmonyLib;

namespace SocksAreAmongUs.GameMode.GameModes
{
    public class DeathSpeed : BaseGameMode
    {
        public override string Id => "death_speed";
        internal static bool Enabled => GameModeManager.CurrentGameMode is DeathSpeed;

        public static int Deaths { get; set; }

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.SetInfected))]
        public static class SetInfectedPatch
        {
            public static void Postfix()
            {
                if (!Enabled)
                    return;

                Deaths = 0;
                PlayerControl.GameOptions.PlayerSpeedMod = 1f + Deaths / 2f;
            }
        }

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.MurderPlayer))]
        public static class MurderPlayerPatch
        {
            public static void Postfix([HarmonyArgument(0)] PlayerControl target)
            {
                if (!Enabled)
                    return;

                if (target && target.Data.IsDead)
                {
                    Deaths++;
                    PlayerControl.GameOptions.PlayerSpeedMod = 1f + Deaths / 2f;
                }
            }
        }
    }
}
