using System;
using System.Collections.Generic;
using System.Linq;
using CodeIsNotAmongUs;
using HarmonyLib;
using Hazel;
using Reactor;
using Reactor.Extensions;
using Reactor.Networking;
using Reactor.Unstrip;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SocksAreAmongUs.GameMode.GameModes
{
    public class BodyDragging : BaseGameMode<BodyDragging.Component>
    {
        public override string Id => "body_dragging";
        internal static bool Enabled => GameModeManager.CurrentGameMode is BodyDragging || Terminator.Enabled;

        private static Sprite _bodyAsset;
        private static Sprite _backgroundAsset;

        public override void LoadAssets(AssetBundle assetBundle)
        {
            _bodyAsset = assetBundle.LoadAsset<Sprite>("Assets/AssetBundle/BodyDragging/Body.png").DontUnload();
            _backgroundAsset = assetBundle.LoadAsset<Sprite>("Assets/AssetBundle/BodyDragging/Background.png").DontUnload();
        }

        private static Dictionary<byte, DeadBody> Bodies { get; } = new Dictionary<byte, DeadBody>();

        public class Component : MonoBehaviour
        {
            public Component(IntPtr ptr) : base(ptr)
            {
            }

            private void FixedUpdate()
            {
                Bodies.Where(x => !x.Value).Select(x => x.Key).ToArray().Do(b => Bodies.Remove(b));

                foreach (var pair in Bodies)
                {
                    var playerInfo = GameData.Instance.GetPlayerById(pair.Key);
                    var deadBody = pair.Value;

                    if (!deadBody)
                    {
                        PluginSingleton<CodeIsNotAmongUsPlugin>.Instance.Log.LogWarning("deadBody was null!");
                        continue;
                    }

                    var position = playerInfo.Object.GetTruePosition();

                    var collider = deadBody.myCollider;
                    var body = deadBody.GetComponent<Rigidbody2D>();
                    if (body == null)
                    {
                        var pet = HatManager.Instance.AllPets[0];
                        body = deadBody.gameObject.AddComponent<Rigidbody2D>();

                        body.inertia = pet.body.inertia;
                        body.gravityScale = pet.body.gravityScale;
                        body.freezeRotation = true;
                        collider.isTrigger = pet.Collider.isTrigger;
                    }

                    var bodyPosition = (Vector2) deadBody.transform.position + deadBody.myCollider.offset * 0.7f;
                    var velocity = body.velocity;
                    var a = position - bodyPosition;
                    var num = 0f;
                    if (PlayerControl.LocalPlayer.CanMove)
                    {
                        num = 0.2f;
                    }

                    if (a.sqrMagnitude > num)
                    {
                        if (a.sqrMagnitude > 2f)
                        {
                            deadBody.transform.position = position;
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
                if (IsActive && !Bodies.ContainsKey(PlayerControl.LocalPlayer.PlayerId))
                {
                    foreach (var pair in Bodies)
                    {
                        if (pair.Value.ParentId == _body.ParentId)
                        {
                            Rpc<RpcDropBody>.Instance.Send(GameData.Instance.GetPlayerById(pair.Key).Object, pair.Value.transform.position);
                        }
                    }

                    Rpc<RpcPickBody>.Instance.Send(_body.ParentId);
                    _body.GetComponent<Renderer>().SetOutline(Color.green);
                }
                else if (_body)
                {
                    Rpc<RpcDropBody>.Instance.Send(_body.transform.position);
                }

                return false;
            }

            private DeadBody _body;

            public override void FixedUpdate()
            {
                if (Bodies.ContainsKey(PlayerControl.LocalPlayer.PlayerId))
                    return;

                var newBody = PlayerControl.LocalPlayer.FindClosestBody(1);

                if (!DeadBody.Equals(newBody, _body))
                {
                    if (_body)
                    {
                        _body.GetComponent<Renderer>().SetOutline(null);
                    }

                    IsActive = newBody;

                    if (newBody)
                    {
                        var component = newBody.GetComponent<Renderer>();
                        component.SetOutline(Color.yellow);
                        Extensions.SetPlayerMaterialColors(component.material, renderer.material);
                    }

                    _body = newBody;
                }
            }

            public void Drop()
            {
                _body.GetComponent<Renderer>().SetOutline(null);
                _body = null;
            }

            public override float Scale => 1f;
            public override float MaxTimer => 0f;
        }

        private static ButtonManager _buttonManager;
        private static readonly int _bodyColor = Shader.PropertyToID("_BodyColor");

        [HarmonyPatch(typeof(HudManager), nameof(HudManager.Start))]
        public static class ButtonPatch
        {
            public static void Postfix(HudManager __instance)
            {
                var gameObject = Object.Instantiate(__instance.KillButton.gameObject, __instance.transform.parent);
                Object.DestroyImmediate(gameObject.GetComponent<KillButtonManager>());
                _buttonManager = gameObject.AddComponent<ButtonManager>();

                CustomButton.Buttons.Add(new CustomButton(_buttonManager, () =>
                {
                    var data = PlayerControl.LocalPlayer.Data;

                    return Enabled && data.IsImpostor && !data.IsDead;
                }));
            }
        }

        [RegisterCustomRpc((uint) CustomRpcCalls.PickBody)]
        public class RpcPickBody : PlayerCustomRpc<SocksAreAmongUsPlugin, byte>
        {
            public RpcPickBody(SocksAreAmongUsPlugin plugin, uint id) : base(plugin, id)
            {
            }

            public override RpcLocalHandling LocalHandling => RpcLocalHandling.Before;

            public override void Write(MessageWriter writer, byte data)
            {
                writer.Write(data);
            }

            public override byte Read(MessageReader reader)
            {
                return reader.ReadByte();
            }

            public override void Handle(PlayerControl player, byte bodyId)
            {
                Bodies[player.PlayerId] = Object.FindObjectsOfType<DeadBody>().FirstOrDefault(x => x.ParentId == bodyId);
            }
        }

        [RegisterCustomRpc((uint) CustomRpcCalls.DropBody)]
        public class RpcDropBody : PlayerCustomRpc<SocksAreAmongUsPlugin, Vector2>
        {
            public RpcDropBody(SocksAreAmongUsPlugin plugin, uint id) : base(plugin, id)
            {
            }

            public override RpcLocalHandling LocalHandling => RpcLocalHandling.Before;

            public override void Write(MessageWriter writer, Vector2 data)
            {
                writer.Write(data);
            }

            public override Vector2 Read(MessageReader reader)
            {
                return reader.ReadVector2();
            }

            public override void Handle(PlayerControl player, Vector2 final)
            {
                var deadBody = Bodies[player.PlayerId];
                Bodies.Remove(player.PlayerId);

                if (player.AmOwner)
                {
                    _buttonManager.Drop();
                }

                deadBody.transform.position = final;

                var body = deadBody.GetComponent<Rigidbody2D>();
                if (body != null)
                {
                    body.velocity = Vector2.zero;
                }
            }
        }
    }
}
