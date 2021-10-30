using System;
using System.Linq;
using Hazel;
using Reactor;
using Reactor.Extensions;
using Reactor.Networking;
using Reactor.Unstrip;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SocksAreAmongUs.GameMode.Roles
{
    public class DoctorRole : CustomRole
    {
        public override string Name => "Doctor";
        public override Color? Color => Palette.LightBlue;
        public override RoleSide Side => RoleSide.Crewmate;
        public override bool ShouldColorName(PlayerControl player) => player.AmOwner;

        public override void OnSet(GameData.PlayerInfo playerInfo)
        {
            base.OnSet(playerInfo);

            if (!playerInfo.Object.AmOwner)
                return;

            var hudManager = HudManager.Instance;
            var gameObject = Object.Instantiate(hudManager.KillButton.gameObject, hudManager.transform.parent);
            Object.DestroyImmediate(gameObject.GetComponent<KillButtonManager>());
            _buttonManager = gameObject.AddComponent<ButtonManager>();

            CustomButton.Buttons.Add(new CustomButton(_buttonManager, () =>
            {
                var data = PlayerControl.LocalPlayer.Data;

                return CustomRoles.Doctor.Test(data) && !data.IsDead;
            }));
        }

        private static Sprite _bodyAsset;
        private static Sprite _backgroundAsset;

        public override void LoadAssets(AssetBundle assetBundle)
        {
            _bodyAsset = assetBundle.LoadAsset<Sprite>("Assets/AssetBundle/Revive/Body.png").DontUnload();
            _backgroundAsset = assetBundle.LoadAsset<Sprite>("Assets/AssetBundle/Revive/Background.png").DontUnload();
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
                if (timer <= 0 && IsActive && _body && !PlayerControl.LocalPlayer.Data.IsDead)
                {
                    Rpc<RpcReviveBody>.Instance.Send(_body.ParentId);
                    timer = MaxTimer;
                    IsActive = false;
                }

                return false;
            }

            private DeadBody _body;

            public override void FixedUpdate()
            {
                var playerControl = PlayerControl.LocalPlayer;
                var data = playerControl.Data;

                if (data.IsImpostor)
                    return;

                DeadBody newBody = null;

                if (timer > 0 && playerControl.CanMove && !data.IsDead)
                {
                    timer = Mathf.Clamp(timer - Time.fixedDeltaTime, 0, MaxTimer);
                    UpdateCoolDown();
                }
                else
                {
                    var position = PlayerControl.LocalPlayer.GetTruePosition();
                    var max = float.MaxValue;

                    foreach (var collider2D in Physics2D.OverlapCircleAll(position, 1f, Constants.NotShipMask))
                    {
                        if (collider2D.CompareTag("DeadBody"))
                        {
                            var distance = Vector3.Distance(position, collider2D.transform.position);
                            if (distance > max)
                                continue;

                            var component = collider2D.GetComponent<DeadBody>();
                            if (component)
                            {
                                max = distance;
                                newBody = component;
                            }
                        }
                    }
                }

                if (_body != newBody)
                {
                    if (_body)
                    {
                        _body.GetComponent<Renderer>().SetOutline(null);
                    }

                    IsActive = newBody;

                    if (newBody)
                    {
                        var component = newBody.GetComponent<Renderer>();
                        component.SetOutline(UnityEngine.Color.yellow);
                        PlayerControl.SetPlayerMaterialColors(component.material.GetColor(_bodyColor), renderer);
                    }

                    _body = newBody;
                }
            }

            public override float Scale => 1f;
            public override float MaxTimer => PlayerControl.GameOptions.KillCooldown;
        }

        private static ButtonManager _buttonManager;
        private static readonly int _bodyColor = Shader.PropertyToID("_BodyColor");
    }

    [RegisterCustomRpc((uint) CustomRpcCalls.ReviveBody)]
    public class RpcReviveBody : PlayerCustomRpc<SocksAreAmongUsPlugin, byte>
    {
        public RpcReviveBody(SocksAreAmongUsPlugin plugin, uint id) : base(plugin, id)
        {
        }

        public override RpcLocalHandling LocalHandling => RpcLocalHandling.Before;

        public override void Write(MessageWriter writer, byte playerId)
        {
            writer.Write(playerId);
        }

        public override byte Read(MessageReader reader)
        {
            return reader.ReadByte();
        }

        public override void Handle(PlayerControl innerNetObject, byte playerId)
        {
            var deadBody = Object.FindObjectsOfType<DeadBody>().Single(x => x.ParentId == playerId);
            var playerInfo = GameData.Instance.GetPlayerById(playerId);

            Rpc<RpcSetDead>.Instance.Send(playerInfo.Object, data: false);
            playerInfo.Object.NetTransform.SnapTo(deadBody.transform.position);
            Object.Destroy(deadBody.gameObject);
        }
    }
}
