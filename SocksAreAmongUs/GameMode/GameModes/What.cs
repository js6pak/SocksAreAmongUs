using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace SocksAreAmongUs.GameMode.GameModes
{
    public class What : BaseGameMode
    {
        public override string Id => "what";
        internal static bool Enabled => GameModeManager.CurrentGameMode is What;

        [HarmonyPatch(typeof(IntroCutscene.Nested_0), nameof(IntroCutscene.Nested_0.MoveNext))]
        public static class CutscenePatch
        {
            public static void Prefix(IntroCutscene.Nested_0 __instance)
            {
                if (!Enabled || !PlayerControl.LocalPlayer.Data.IsImpostor)
                    return;

                var original = __instance.yourTeam.ToArray();

                __instance.yourTeam.Clear();
                __instance.yourTeam.Add(original.First());
            }
        }

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
    }
}
