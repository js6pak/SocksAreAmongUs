using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Hazel;
using InnerNet;
using Reactor;
using Reactor.Extensions;
using Reactor.Networking;
using Reactor.Unstrip;
using UnityEngine;

namespace SocksAreAmongUs.GameMode.GameModes
{
    public class LiveBodyDragging : BaseGameMode<LiveBodyDragging.Component>
    {
        public override string Id => "live_body_dragging";
        internal static bool Enabled => GameModeManager.CurrentGameMode is LiveBodyDragging;

        private static Sprite _bodyAsset;
        private static Sprite _backgroundAsset;

        public override void LoadAssets(AssetBundle assetBundle)
        {
            _bodyAsset = assetBundle.LoadAsset<Sprite>("Assets/AssetBundle/LiveBodyDragging/Body.png").DontUnload();
            _backgroundAsset = assetBundle.LoadAsset<Sprite>("Assets/AssetBundle/LiveBodyDragging/Background.png").DontUnload();
        }

        private static Dictionary<PlayerControl, PlayerControl> Bodies { get; } = new Dictionary<PlayerControl, PlayerControl>(Il2CppEqualityComparer<PlayerControl>.Instance);

        public class Component : MonoBehaviour
        {
            public Component(IntPtr ptr) : base(ptr)
            {
            }

            private void LateUpdate()
            {
                foreach (var (controller, controlled) in Bodies)
                {
                    controlled.myRend.SetOutline(Color.green);

                    var position = controller.GetTruePosition();

                    var body = controlled.MyPhysics.body;

                    body.inertia = HatManager.Instance.AllPets[0].body.inertia;

                    var bodyPosition = controlled.GetTruePosition();
                    var velocity = body.velocity;
                    var a = position - bodyPosition; // + new Vector2(0.1f, 0.1f);
                    var num = 0f;
                    if (PlayerControl.LocalPlayer.CanMove)
                    {
                        num = 0.2f;
                    }

                    if (a.sqrMagnitude > num)
                    {
                        if (a.sqrMagnitude > GameOptionsData.KillDistances[PlayerControl.GameOptions.KillDistance])
                        {
                            controlled.transform.position = position;
                            return;
                        }

                        a *= 5f * PlayerControl.GameOptions.PlayerSpeedMod;
                        velocity = velocity * 0.8f + a * 0.2f;
                    }
                    else
                    {
                        velocity *= 0.7f;
                    }

                    body.velocity = velocity;
                }
            }
        }

        [RegisterInIl2Cpp]
        public class ButtonManager : CustomButtonBehaviour
        {
            public ButtonManager(IntPtr ptr) : base(ptr)
            {
            }

            protected override void SetSprite()
            {
                renderer = GetComponent<SpriteRenderer>();
                var material = renderer.material;
                renderer.Destroy();

                renderer = AddLayer(_bodyAsset, new Material(PlayerSelectMenu.PlayerShaderAsset));
                AddLayer(_backgroundAsset, material);
            }

            public override bool OnClick()
            {
                if (IsActive && !Bodies.ContainsKey(PlayerControl.LocalPlayer))
                {
                    foreach (var pair in Bodies)
                    {
                        if (pair.Value.Equals(_target))
                        {
                            Rpc<RpcDragLiveBody>.Instance.Send(pair.Key, pair.Value, false);
                        }
                    }

                    Rpc<RpcDragLiveBody>.Instance.Send(_target);
                }
                else if (_target)
                {
                    Rpc<RpcDragLiveBody>.Instance.Send(null);
                }

                return false;
            }

            private PlayerControl _target;

            public override void FixedUpdate()
            {
                if (Bodies.ContainsKey(PlayerControl.LocalPlayer))
                    return;

                var target = HudManager.Instance.KillButton.CurrentTarget;

                if (!PlayerControl.Equals(target, _target))
                {
                    IsActive = target && !isCoolingDown;

                    if (target)
                    {
                        PlayerControl.SetPlayerMaterialColors(target.Data.ColorId, renderer);
                    }

                    _target = target;
                }
            }

            public void Drop()
            {
                _target.myRend.SetOutline(null);
                _target = null;
            }

            public override float Scale => 1f;
            public override float MaxTimer => 0f;
        }

        [HarmonyPatch(typeof(HudManager), nameof(HudManager.Start))]
        public static class ButtonPatch
        {
            public static void Postfix()
            {
                CustomButton.Buttons.Add(new CustomButton(Extensions.CreateButton<ButtonManager>(), () =>
                {
                    var data = PlayerControl.LocalPlayer.Data;

                    return Enabled && data.IsImpostor && !data.IsDead;
                }));
            }
        }

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CanMove), MethodType.Getter)]
        public static class CanMovePatch
        {
            public static bool Prefix(PlayerControl __instance, ref bool __result)
            {
                if (Bodies.Values.Contains(__instance, Il2CppEqualityComparer<PlayerControl>.Instance))
                {
                    return __result = false;
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(CustomNetworkTransform), nameof(CustomNetworkTransform.FixedUpdate))]
        public static class CustomNetworkTransform_FixedUpdatePatch
        {
            public static bool Prefix(CustomNetworkTransform __instance)
            {
                var playerControl = __instance.GetComponent<PlayerControl>();
                if (playerControl && Bodies.Values.Contains(playerControl, Il2CppEqualityComparer<PlayerControl>.Instance))
                {
                    return false;
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(CustomNetworkTransform), nameof(CustomNetworkTransform.Deserialize))]
        public static class CustomNetworkTransform_DeserializePatch
        {
            public static bool Prefix(CustomNetworkTransform __instance)
            {
                var playerControl = __instance.GetComponent<PlayerControl>();
                if (playerControl && Bodies.Values.Contains(playerControl, Il2CppEqualityComparer<PlayerControl>.Instance))
                {
                    return false;
                }

                return true;
            }
        }

        [RegisterCustomRpc((uint) CustomRpcCalls.DragLiveBody)]
        public class RpcDragLiveBody : PlayerCustomRpc<SocksAreAmongUsPlugin, PlayerControl>
        {
            public RpcDragLiveBody(SocksAreAmongUsPlugin plugin, uint id) : base(plugin, id)
            {
            }

            public override RpcLocalHandling LocalHandling => RpcLocalHandling.After;

            public override void Write(MessageWriter writer, PlayerControl data)
            {
                MessageExtensions.WriteNetObject(writer, data);
            }

            public override PlayerControl Read(MessageReader reader)
            {
                return MessageExtensions.ReadNetObject<PlayerControl>(reader);
            }

            public override void Handle(PlayerControl innerNetObject, PlayerControl data)
            {
                if (data == null)
                {
                    if (Bodies.TryGetValue(innerNetObject, out var previous))
                    {
                        previous.myRend.SetOutline(null);

                        var body = previous.MyPhysics.body;
                        body.inertia = 0;
                        body.velocity = Vector2.zero;

                        var netTransform = previous.NetTransform;
                        netTransform.targetSyncPosition = netTransform.prevPosSent = previous.transform.position;
                        netTransform.targetSyncVelocity = netTransform.prevVelSent = Vector2.zero;
                    }

                    Bodies.Remove(innerNetObject);
                }
                else
                {
                    Bodies[innerNetObject] = data;
                }
            }
        }
    }
}
