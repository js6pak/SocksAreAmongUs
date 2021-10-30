using System;
using System.Linq;
using HarmonyLib;
using Reactor.Extensions;
using Reactor.Unstrip;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SocksAreAmongUs.GameMode.GameModes
{
    public class Teleport : BaseGameMode<Teleport.Component>
    {
        public override string Id => "teleport";
        internal static bool Enabled => GameModeManager.CurrentGameMode is Teleport || Terminator.Enabled;

        private static float _timer;
        private const float MaxTimer = 20;
        private static TextRenderer _text;

        public class Component : MonoBehaviour
        {
            public Component(IntPtr ptr) : base(ptr)
            {
            }

            private void FixedUpdate()
            {
                if (!Enabled || PlayerControl.LocalPlayer == null || !PlayerControl.LocalPlayer.CanMove)
                    return;

                _timer = Mathf.Max(0, _timer - Time.fixedDeltaTime);
            }
        }

        private static AudioClip _asset;

        public override void LoadAssets(AssetBundle assetBundle)
        {
            _asset = assetBundle.LoadAsset<AudioClip>("Assets/AssetBundle/Teleport.ogg").DontUnload();
        }

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.SetInfected))]
        public static class SetInfectedPatch
        {
            public static void Postfix()
            {
                if (!Enabled)
                    return;

                _timer = MaxTimer;
            }
        }

        private static void UpdateText()
        {
            _text.Text = _timer > 0 ? $"You can teleport in: {(int) _timer}s" : "You can teleport now!";
            _text.Color = _timer > 0 ? Color.red : Color.green;
        }

        [HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.ShowNormalMap))]
        public static class OpenPatch
        {
            public static void Postfix(MapBehaviour __instance)
            {
                if (!Enabled || !PlayerControl.LocalPlayer.Data.IsImpostor || !__instance.taskOverlay.gameObject.active)
                    return;

                if (_text)
                {
                    Object.Destroy(_text.gameObject);
                }

                var pingTracker = Object.FindObjectOfType<PingTracker>();
                var gameObject = Object.Instantiate(pingTracker.gameObject, pingTracker.transform.parent);
                Object.DestroyImmediate(gameObject.GetComponent<PingTracker>());

                var aspectPosition = gameObject.GetComponent<AspectPosition>();

                var vector3 = aspectPosition.DistanceFromEdge;
                vector3.x = 0.2f;
                vector3.y = 0.3f;
                aspectPosition.DistanceFromEdge = vector3;

                aspectPosition.Alignment = AspectPosition.EdgeAlignments.LeftBottom;

                aspectPosition.AdjustPosition();

                _text = gameObject.GetComponent<TextRenderer>();
                UpdateText();
            }
        }

        [HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.Close))]
        public static class ClosePatch
        {
            public static void Postfix()
            {
                if (_text)
                {
                    _text.gameObject.SetActive(false);
                    Object.Destroy(_text.gameObject);
                }
            }
        }

        public static bool TeleportToMouse(MapBehaviour mapBehaviour)
        {
            Input.ResetInputAxes();

            var camera = Camera.main;
            if (camera == null)
                return false;

            var point = mapBehaviour.HerePoint.transform.parent.InverseTransformPoint(camera.ScreenToWorldPoint(Input.mousePosition));
            point.x /= Mathf.Sign(ShipStatus.Instance.transform.localScale.x);
            point *= ShipStatus.Instance.MapScale;

            if (!ShipStatus.Instance.AllRooms.Any(room => room.roomArea && room.roomArea.OverlapPoint(point)))
                return false;

            var transform = PlayerControl.LocalPlayer.transform;

            SoundManager.Instance.PlaySound(_asset, false, 10);
            PlayerControl.LocalPlayer.NetTransform.RpcSnapTo(new Vector3(point.x, point.y, transform.position.z));

            return true;
        }

        [HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.FixedUpdate))]
        public static class MapPatch
        {
            public static void Postfix(MapBehaviour __instance)
            {
                if (!Enabled || !PlayerControl.LocalPlayer.Data.IsImpostor || _text == null)
                    return;

                UpdateText();

                if (Input.GetMouseButtonDown(0))
                {
                    if (_timer > 0 || !__instance.taskOverlay.gameObject.active)
                        return;

                    if (TeleportToMouse(__instance))
                    {
                        _timer = MaxTimer;
                    }
                }
            }
        }
    }
}
