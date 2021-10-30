using HarmonyLib;
using SocksAreAmongUs.GameMode.Roles;

namespace SocksAreAmongUs.GameMode
{
    public static class EndGamePatches
    {
        [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameEnd))]
        public static class OnGameEndPatch
        {
            public static bool Prefix(AmongUsClient __instance, [HarmonyArgument(0)] GameOverReason gameOverReason, [HarmonyArgument(1)] bool showAd)
            {
                __instance.DisconnectHandlers.Clear();
                if (Minigame.Instance)
                {
                    Minigame.Instance.Close();
                    Minigame.Instance.Close();
                }

                TempData.EndReason = gameOverReason;
                TempData.showAd = showAd;
                var flag = TempData.DidHumansWin(gameOverReason);
                TempData.winners = new Il2CppSystem.Collections.Generic.List<WinningPlayerData>();

                foreach (var playerInfo in GameData.Instance.AllPlayers)
                {
                    if (gameOverReason == GameOverReason.HumansDisconnect || gameOverReason == GameOverReason.ImpostorDisconnect || gameOverReason == JesterRole.Reason || gameOverReason == TrollRole.Reason || flag != playerInfo.IsImpostor)
                    {
                        var winningPlayerData = new WinningPlayerData(playerInfo);

                        if (CustomRoles.Players.TryGetValue(playerInfo.PlayerId, out var customRole))
                        {
                            if (customRole is JesterRole)
                            {
                                if (gameOverReason == JesterRole.Reason)
                                {
                                    winningPlayerData.IsDead = true;
                                }
                                else
                                {
                                    continue;
                                }
                            }
                            else if (customRole is TrollRole)
                            {
                                if (gameOverReason == TrollRole.Reason)
                                {
                                    winningPlayerData.IsDead = true;
                                }
                                else
                                {
                                    continue;
                                }
                            }
                        }
                        else if (gameOverReason == TrollRole.Reason || gameOverReason == JesterRole.Reason)
                        {
                            continue;
                        }

                        TempData.winners.Add(winningPlayerData);
                    }
                }

                __instance.StartCoroutine(__instance.CoEndGame());

                return false;
            }
        }
    }
}
