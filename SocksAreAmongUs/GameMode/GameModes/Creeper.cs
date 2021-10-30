using System.Collections;
using HarmonyLib;
using Hazel;
using Reactor;
using Reactor.Extensions;
using Reactor.Networking;
using Reactor.Unstrip;
using SocksAreAmongUs.GameMode.Roles;
using UnityEngine;
using UnityEngine.Video;

namespace SocksAreAmongUs.GameMode.GameModes
{
    public class Creeper : BaseGameMode
    {
        public override string Id => "creeper";
        internal static bool Enabled => GameModeManager.CurrentGameMode is Creeper;

        private static Sprite _buttonAsset;
        private static AudioClip _fuseAsset;
        private static AudioClip _explosionAudioAsset;
        private static VideoClip _explosionVideoAsset;

        public override void LoadAssets(AssetBundle assetBundle)
        {
            _buttonAsset = assetBundle.LoadAsset<Sprite>("Assets/AssetBundle/Creeper/Button.png").DontUnload();
            _fuseAsset = assetBundle.LoadAsset<AudioClip>("Assets/AssetBundle/Creeper/Fuse.ogg").DontUnload();
            _explosionAudioAsset = assetBundle.LoadAsset<AudioClip>("Assets/AssetBundle/Creeper/Explosion.ogg").DontUnload();
            _explosionVideoAsset = assetBundle.LoadAsset<VideoClip>("Assets/AssetBundle/Creeper/ExplosionClip.webm").DontUnload();
        }

        public static IEnumerator Explode(PlayerControl player)
        {
            var videoPlayer = VideoPlayerHelper.Create(_explosionVideoAsset);
            videoPlayer.Stop();
            videoPlayer.Prepare();
            videoPlayer.Stop();

            var fuse = SoundManager.Instance.PlaySound(_fuseAsset, parent: player.transform);

            yield return new WaitForSeconds(_fuseAsset.length - 3.5f);

            fuse.Stop();

            var position = (Vector3) player.GetTruePosition();

            var transform = videoPlayer.gameObject.transform;
            position.z -= 5;
            transform.position = position;
            transform.rotation = new Quaternion(-1f, 0, 0, 1);

            videoPlayer.Play();
            SoundManager.Instance.PlaySound(_explosionAudioAsset, position);

            if (player.AmOwner)
            {
                yield return new WaitForSeconds(0.5f);

                var truePosition = player.GetTruePosition();

                foreach (var targetInfo in GameData.Instance.AllPlayers)
                {
                    if (targetInfo.IsDead || targetInfo.Disconnected)
                        continue;

                    var target = targetInfo.Object;
                    if (target && !target.AmOwner)
                    {
                        var vector = target.GetTruePosition() - truePosition;
                        if (vector.magnitude <= 3)
                        {
                            Rpc<RpcSetDead>.Instance.Send(target, data: true);
                        }
                    }
                }
            }
        }

        private static Sprite _originalButtonAsset;

        public override void OnGameStart()
        {
            if (!Enabled)
                return;

            if (PlayerControl.LocalPlayer.Data.IsImpostor)
            {
                var killButton = HudManager.Instance.KillButton;
                _originalButtonAsset = killButton.renderer.sprite;
                killButton.renderer.sprite = _buttonAsset;
                CooldownHelpers.SetCooldownNormalizedUvs(killButton.renderer);
            }
        }

        public override void Cleanup()
        {
            var killButton = HudManager.Instance.KillButton;
            killButton.renderer.sprite = _originalButtonAsset;
        }

        [HarmonyPatch(typeof(KillButtonManager), nameof(KillButtonManager.PerformKill))]
        public static class PerformPatch
        {
            public static bool Prefix()
            {
                if (!Enabled || !PlayerControl.LocalPlayer.Data.IsImpostor)
                    return true;

                if (CustomRoles.Players.TryGetValue(PlayerControl.LocalPlayer.PlayerId, out var customRole) && customRole is SheriffRole)
                    return true;

                Rpc<RpcExplode>.Instance.Send(null);

                return false;
            }
        }

        [HarmonyPatch(typeof(KillButtonManager), nameof(KillButtonManager.SetTarget))]
        public static class SetTargetPatch
        {
            public static void Prefix([HarmonyArgument(0)] ref PlayerControl target)
            {
                var data = PlayerControl.LocalPlayer.Data;
                if (Enabled && data.IsImpostor && PlayerControl.LocalPlayer.CanMove && !data.IsDead)
                {
                    target = PlayerControl.LocalPlayer;
                }
            }
        }
    }

    [RegisterCustomRpc((uint) CustomRpcCalls.Explode)]
    public class RpcExplode : PlayerCustomRpc<SocksAreAmongUsPlugin, object>
    {
        public RpcExplode(SocksAreAmongUsPlugin plugin, uint id) : base(plugin, id)
        {
        }

        public override RpcLocalHandling LocalHandling => RpcLocalHandling.Before;

        public override void Write(MessageWriter writer, object data)
        {
        }

        public override object Read(MessageReader reader)
        {
            return null;
        }

        public override void Handle(PlayerControl target, object value)
        {
            Coroutines.Start(Creeper.Explode(target));
        }
    }
}
