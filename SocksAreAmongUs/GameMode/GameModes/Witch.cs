using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using Reactor;
using Reactor.Extensions;
using Reactor.Networking;
using Reactor.Unstrip;
using SocksAreAmongUs.GameMode.Roles;
using UnityEngine;

namespace SocksAreAmongUs.GameMode.GameModes
{
    public class Witch : BaseGameMode
    {
        public override string Id => "witch";
        internal static bool Enabled => GameModeManager.CurrentGameMode is Witch;

        public static HashSet<byte> Poisoned { get; } = new HashSet<byte>();

        private static Sprite _asset;

        public override void LoadAssets(AssetBundle assetBundle)
        {
            _asset = assetBundle.LoadAsset<Sprite>("Assets/AssetBundle/Poison.png").DontUnload();
        }

        private static Sprite _originalButtonAsset;

        public override void OnGameStart()
        {
            if (!Enabled)
                return;

            Poisoned.Clear();

            if (PlayerControl.LocalPlayer.Data.IsImpostor)
            {
                var killButton = HudManager.Instance.KillButton;
                _originalButtonAsset = killButton.renderer.sprite;
                killButton.renderer.sprite = _asset;
                CooldownHelpers.SetCooldownNormalizedUvs(killButton.renderer);
            }
        }

        public override void Cleanup()
        {
            var killButton = HudManager.Instance.KillButton;
            killButton.renderer.sprite = _originalButtonAsset;
        }

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
        public static class RedNamePatch
        {
            public static void Postfix(PlayerControl __instance)
            {
                if (!Enabled || !PlayerControl.LocalPlayer.Data.IsImpostor)
                    return;

                if (Poisoned.Contains(__instance.PlayerId))
                {
                    __instance.myRend.SetOutline(Palette.PlayerColors[11]);
                }
            }
        }

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcMurderPlayer))]
        [HarmonyPriority(Priority.High)]
        public static class RpcMurderPlayerPatch
        {
            public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target)
            {
                if (!Enabled || !__instance.Data.IsImpostor)
                    return true;

                if (CustomRoles.Players.TryGetValue(PlayerControl.LocalPlayer.PlayerId, out var customRole) && customRole is SheriffRole)
                    return true;

                if (Poisoned.Contains(target.PlayerId))
                    return false;

                if (__instance.AmOwner)
                {
                    PlayerControl.LocalPlayer.SetKillTimer(PlayerControl.GameOptions.KillCooldown);
                }

                Poisoned.Add(target.PlayerId);

                Coroutines.Start(Coroutine(target, __instance.AmOwner));

                return false;
            }

            public static IEnumerator Coroutine(PlayerControl target, bool amOwner)
            {
                yield return new WaitForSeconds(10);

                if (amOwner && !target.Data.IsDead && !target.Data.Disconnected && AmongUsClient.Instance.IsGameStarted)
                {
                    Rpc<RpcSetDead>.Instance.Send(target, data: true);
                }

                Poisoned.Remove(target.PlayerId);
            }
        }
    }
}
