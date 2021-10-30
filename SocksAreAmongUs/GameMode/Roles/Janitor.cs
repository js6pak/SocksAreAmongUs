using System;
using System.Linq;
using HarmonyLib;
using Hazel;
using Reactor;
using Reactor.Extensions;
using Reactor.Networking;
using Reactor.Unstrip;
using UnityEngine;

namespace SocksAreAmongUs.GameMode.Roles
{
    public class JanitorRole : CustomRole
    {
        public override string Name => "Janitor";
        public override Color? Color => null;
        public override string Description => null;
        public override RoleSide Side => RoleSide.Impostor;
        public override bool ShouldColorName(PlayerControl player) => PlayerControl.LocalPlayer.Data.IsImpostor;

        private static Sprite _backgroundAsset;
        private static Sprite _bodyAsset;

        public override void LoadAssets(AssetBundle assetBundle)
        {
            _backgroundAsset = assetBundle.LoadAsset<Sprite>("Assets/AssetBundle/Janitor/Background.png").DontUnload();
            _bodyAsset = assetBundle.LoadAsset<Sprite>("Assets/AssetBundle/Janitor/Body.png").DontUnload();
        }

        [HarmonyPatch(typeof(HudManager), nameof(HudManager.Start))]
        public static class ButtonsPatch
        {
            public static void Postfix()
            {
                static bool IsActive()
                {
                    var data = PlayerControl.LocalPlayer.Data;

                    return CustomRoles.Janitor.Test(data) && data.IsImpostor && !data.IsDead;
                }

                CustomButton.Buttons.Add(new CustomButton(Extensions.CreateButton<ButtonManager>(), IsActive));
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
                if (base.OnClick() && _body)
                {
                    Rpc<DespawnBodyRpc>.Instance.Send(_body.ParentId);
                    IsActive = false;
                    timer = MaxTimer;
                }

                return false;
            }

            private DeadBody _body;
            private bool? _lastIsCoolingDown;
            public override float MaxTimer => 20;

            public override void FixedUpdate()
            {
                UpdateTimer();

                var newBody = PlayerControl.LocalPlayer.FindClosestBody(1);

                if (!DeadBody.Equals(newBody, _body) || _lastIsCoolingDown != isCoolingDown)
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
                        Extensions.SetPlayerMaterialColors(component.material, renderer.material);
                    }

                    _body = newBody;
                }

                _lastIsCoolingDown = isCoolingDown;
            }

            [RegisterCustomRpc((uint) CustomRpcCalls.DespawnBody)]
            public class DespawnBodyRpc : PlayerCustomRpc<SocksAreAmongUsPlugin, byte>
            {
                public DespawnBodyRpc(SocksAreAmongUsPlugin plugin, uint id) : base(plugin, id)
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

                public override void Handle(PlayerControl innerNetObject, byte data)
                {
                    FindObjectsOfType<DeadBody>().Where(x => x.ParentId == data).Do(x => x.gameObject.Destroy());
                }
            }
        }
    }
}
